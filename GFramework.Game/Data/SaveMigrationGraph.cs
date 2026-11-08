// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Data;

/// <summary>
/// Immutable directed schema graph shared by game and mod payloads. Only monotonic routes toward the
/// target are legal. A unique shortest route is selected; equal shortest routes require an explicit version path.
/// </summary>
public sealed class SaveMigrationGraph
{
    private readonly IReadOnlyDictionary<(int From, int To), SaveMigrationStep> _steps;

    /// <summary>Captures all declared edges; later caller collection changes cannot alter this policy.</summary>
    /// <param name="steps">Explicitly implemented edges, or null for an empty graph.</param>
    public SaveMigrationGraph(IEnumerable<SaveMigrationStep>? steps = null)
    {
        var indexed = new Dictionary<(int From, int To), SaveMigrationStep>();
        foreach (var step in steps ?? [])
        {
            ArgumentNullException.ThrowIfNull(step, nameof(steps));
            if (!indexed.TryAdd((step.FromVersion, step.ToVersion), step))
                throw new ArgumentException("A migration edge can only be registered once.", nameof(steps));
        }
        _steps = indexed;
    }

    /// <summary>Plans a deterministic monotonic conversion without running any callbacks.</summary>
    /// <param name="fromVersion">Source version.</param>
    /// <param name="toVersion">Target version.</param>
    /// <param name="versionPath">Optional complete path including source and target.</param>
    /// <returns>Immutable selected conversion edges.</returns>
    public IReadOnlyList<SaveMigrationStep> Plan(int fromVersion, int toVersion, IReadOnlyList<int>? versionPath = null)
    {
        if (fromVersion < 1 || toVersion < 1) throw new ArgumentOutOfRangeException(nameof(fromVersion));
        if (versionPath is not null) return ResolveExplicitPath(fromVersion, toVersion, versionPath);
        if (fromVersion == toVersion) return Array.Empty<SaveMigrationStep>();
        return ResolveExplicitPath(fromVersion, toVersion, FindShortestPath(fromVersion, toVersion));
    }

    /// <summary>
    /// Visits each schema node once and counts shortest routes up to two. This avoids enumerating exponentially
    /// many paths in a branching schema DAG, including the important missing-target case.
    /// </summary>
    private IReadOnlyList<int> FindShortestPath(int fromVersion, int toVersion)
    {
        var outgoing = _steps.Values.GroupBy(static edge => edge.FromVersion)
            .ToDictionary(static group => group.Key, static group => group.OrderBy(static edge => edge.ToVersion).ToArray());
        var distances = new Dictionary<int, int> { [fromVersion] = 0 };
        var routeCounts = new Dictionary<int, int> { [fromVersion] = 1 };
        var predecessor = new Dictionary<int, int>();
        var pending = new Queue<int>();
        pending.Enqueue(fromVersion);
        while (pending.TryDequeue(out var version))
        {
            if (version == toVersion) break;
            if (!outgoing.TryGetValue(version, out var edges)) continue;
            foreach (var edge in edges)
            {
                if (!MovesToward(version, edge.ToVersion, toVersion)) continue;
                var distance = distances[version] + 1;
                if (!distances.TryGetValue(edge.ToVersion, out var known))
                {
                    distances.Add(edge.ToVersion, distance);
                    routeCounts.Add(edge.ToVersion, routeCounts[version]);
                    predecessor.Add(edge.ToVersion, version);
                    pending.Enqueue(edge.ToVersion);
                }
                else if (known == distance)
                    routeCounts[edge.ToVersion] = Math.Min(2, routeCounts[edge.ToVersion] + routeCounts[version]);
            }
        }
        if (!routeCounts.TryGetValue(toVersion, out var count))
            throw new InvalidOperationException($"No migration path exists from {fromVersion} to {toVersion}.");
        if (count != 1) throw new InvalidOperationException("Migration path is ambiguous; supply an explicit version path.");
        var result = new List<int> { toVersion };
        while (result[^1] != fromVersion) result.Add(predecessor[result[^1]]);
        result.Reverse();
        return result.AsReadOnly();
    }

    /// <summary>Executes only the planned edges on private block copies and verifies every declared output version.</summary>
    internal static async Task<SaveDataBlock> ConvertAsync(SaveDataBlock source,
        IReadOnlyList<SaveMigrationStep> path, CancellationToken cancellationToken)
    {
        var current = source.Clone();
        foreach (var step in path)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (current.Version != step.FromVersion) throw new InvalidOperationException("Migration source version changed.");
            var converted = await step.ConvertAsync(current.Clone(), cancellationToken).ConfigureAwait(false);
            if (converted is null || converted.Data is null || converted.Version != step.ToVersion)
                throw new InvalidOperationException("A migration returned null data or the wrong declared version.");
            current = converted.Clone();
        }
        return current;
    }

    /// <summary>Validates every explicit edge instead of treating a caller-selected route as trusted configuration.</summary>
    private IReadOnlyList<SaveMigrationStep> ResolveExplicitPath(int fromVersion, int toVersion, IReadOnlyList<int> versions)
    {
        if (versions.Count == 0 || versions[0] != fromVersion || versions[^1] != toVersion)
            throw new ArgumentException("An explicit path must include the source and target versions.", nameof(versions));
        var result = new List<SaveMigrationStep>();
        for (var index = 1; index < versions.Count; index++)
        {
            if (!MovesToward(versions[index - 1], versions[index], toVersion) ||
                !_steps.TryGetValue((versions[index - 1], versions[index]), out var step))
                throw new ArgumentException("An explicit path contains an undeclared or non-monotonic edge.", nameof(versions));
            result.Add(step);
        }
        return result.AsReadOnly();
    }

    /// <summary>Restricts conversion to progress toward the target without overshooting or reversing direction.</summary>
    private static bool MovesToward(int from, int to, int target) =>
        target > from ? to > from && to <= target : to < from && to >= target;
}
