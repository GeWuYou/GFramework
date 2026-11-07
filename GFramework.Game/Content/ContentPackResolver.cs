// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Content;

namespace GFramework.Game.Content;

/// <summary>
/// Resolves one deterministic startup content set. Dependencies always precede their users;
/// player preferences determine the remaining order, and discovery never enables optional content.
/// </summary>
public static class ContentPackResolver
{
    /// <summary>Validates activation and compatibility before constructing an effective load plan.</summary>
    /// <param name="packages">Discovered installed packages.</param>
    /// <param name="profile">Player choices to apply for the next session.</param>
    /// <param name="host">Game-owned compatibility contract.</param>
    /// <returns>A complete ordered plan; no package is silently omitted to make it succeed.</returns>
    public static ContentLoadPlan Resolve(IEnumerable<ContentPack> packages, ContentPackProfile profile,
        ContentHostCompatibility host)
    {
        ArgumentNullException.ThrowIfNull(packages);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(host);
        ArgumentException.ThrowIfNullOrWhiteSpace(host.GameId, nameof(host));
        _ = ContentVersionRules.Parse(host.GameVersion);
        _ = ContentVersionRules.Parse(host.ContentApiVersion);
        if (!ContentId.IsCanonicalSegment(host.BasePackId))
            throw new ArgumentException("Base package ID must be canonical.", nameof(host));
        ArgumentNullException.ThrowIfNull(profile.EnabledPackages, nameof(profile));
        ArgumentNullException.ThrowIfNull(profile.LoadOrder, nameof(profile));
        var installed = IndexPackages(packages);
        var enabled = new HashSet<string>(profile.EnabledPackages, StringComparer.Ordinal) { host.BasePackId };
        foreach (var id in enabled)
            if (!installed.ContainsKey(id))
                throw Failure("MissingPackage", id, $"Enabled package '{id}' is not installed.");
        var selected = installed.Where(pair => enabled.Contains(pair.Key))
            .ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal);
        foreach (var pack in selected.Values)
            ValidateSelectedPackage(pack, selected, host);
        return new ContentLoadPlan(Sort(selected, profile.LoadOrder, host.BasePackId));
    }

    private static Dictionary<string, ContentPack> IndexPackages(IEnumerable<ContentPack> packages)
    {
        var installed = new Dictionary<string, ContentPack>(StringComparer.Ordinal);
        foreach (var pack in packages)
        {
            ArgumentNullException.ThrowIfNull(pack, nameof(packages));
            try { ContentPackDiscovery.ValidateManifest(pack.Manifest); }
            catch (ArgumentException exception)
            {
                throw new ContentLoadException(new ContentLoadDiagnostic
                {
                    Code = "InvalidManifest", PackId = pack.Manifest.Id, PackVersion = pack.Manifest.Version,
                    Detail = exception.Message
                }, exception);
            }
            if (!installed.TryAdd(pack.Manifest.Id, pack))
                throw Failure("DuplicatePackage", pack.Manifest.Id,
                    $"Package '{pack.Manifest.Id}' has multiple installed copies; select one installation explicitly.");
        }
        return installed;
    }

    private static void ValidateSelectedPackage(ContentPack pack, IReadOnlyDictionary<string, ContentPack> selected,
        ContentHostCompatibility host)
    {
        var manifest = pack.Manifest;
        if (!string.Equals(manifest.GameId, host.GameId, StringComparison.Ordinal) ||
            !ContentVersionRules.Contains(host.GameVersion, manifest.GameVersions) ||
            !ContentVersionRules.Contains(host.ContentApiVersion, manifest.ContentApiVersions))
            throw Failure("HostIncompatible", manifest.Id, $"Package '{manifest.Id}' does not support this game/content API.");
        if (string.Equals(manifest.Id, host.BasePackId, StringComparison.Ordinal) && manifest.Dependencies.Count != 0)
            throw Failure("InvalidBasePackage", manifest.Id, "The mandatory base package cannot depend on optional packages.");
        foreach (var dependency in manifest.Dependencies)
        {
            if (!selected.TryGetValue(dependency.Id, out var target))
            {
                if (dependency.Optional) continue;
                throw Failure("MissingDependency", manifest.Id,
                    $"Package '{manifest.Id}' requires enabled package '{dependency.Id}'.", dependency.Id);
            }
            if (!ContentVersionRules.Contains(target.Manifest.Version, dependency.Versions))
                throw Failure("DependencyVersionMismatch", manifest.Id,
                    $"Package '{manifest.Id}' does not accept installed '{dependency.Id}' version '{target.Manifest.Version}'.", dependency.Id);
        }
        foreach (var incompatible in manifest.IncompatiblePackages)
            if (selected.ContainsKey(incompatible))
                throw Failure("IncompatiblePackages", manifest.Id,
                    $"Packages '{manifest.Id}' and '{incompatible}' cannot be enabled together.", incompatible);
    }

    private static IReadOnlyList<ContentPack> Sort(IReadOnlyDictionary<string, ContentPack> selected,
        IReadOnlyList<string> preferences, string baseId)
    {
        var priorities = preferences.Select((id, index) => (id, index))
            .GroupBy(static item => item.id, StringComparer.Ordinal)
            .ToDictionary(static group => group.Key, static group => group.First().index, StringComparer.Ordinal);
        var remaining = selected.Keys.ToHashSet(StringComparer.Ordinal);
        var result = new List<ContentPack>(selected.Count);
        var loaded = new HashSet<string>(StringComparer.Ordinal);
        while (remaining.Count != 0)
        {
            var candidate = remaining
                .Where(id => (string.Equals(id, baseId, StringComparison.Ordinal) || loaded.Contains(baseId)) &&
                    selected[id].Manifest.Dependencies.All(dependency =>
                        !selected.ContainsKey(dependency.Id) || loaded.Contains(dependency.Id)))
                .OrderBy(id => string.Equals(id, baseId, StringComparison.Ordinal) ? int.MinValue :
                    priorities.TryGetValue(id, out var priority) ? priority : int.MaxValue)
                .ThenBy(static id => id, StringComparer.Ordinal).FirstOrDefault();
            if (candidate is null)
                throw Failure("DependencyCycle", remaining.OrderBy(static id => id, StringComparer.Ordinal).First(),
                    "Enabled package dependencies form a cycle: " + string.Join(", ", remaining.OrderBy(static id => id, StringComparer.Ordinal)));
            remaining.Remove(candidate);
            loaded.Add(candidate);
            result.Add(selected[candidate]);
        }
        return result;
    }

    private static ContentLoadException Failure(string code, string packId, string detail, string? relatedPackId = null)
        => new(new ContentLoadDiagnostic { Code = code, PackId = packId, RelatedPackId = relatedPackId, Detail = detail });
}
