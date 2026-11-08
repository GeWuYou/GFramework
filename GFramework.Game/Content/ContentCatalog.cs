// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Config;
using GFramework.Game.Abstractions.Content;

namespace GFramework.Game.Content;

/// <summary>
/// A fully validated, structurally immutable session catalog. Create another session after restart to
/// change content. DTOs are game-owned and must be treated as read-only by consumers.
/// </summary>
public sealed class ContentCatalog
{
    private readonly IReadOnlyDictionary<string, IConfigTable> _tables;
    private readonly IReadOnlySet<ContentReference> _references;
    private readonly IReadOnlyDictionary<ContentReference, IReadOnlyList<ContentSource>> _sources;
    private readonly IReadOnlyDictionary<(ContentReference Reference, string Field), ContentSource> _fieldSources;

    /// <summary>Publishes a prepared catalog only after all operations and validations succeed.</summary>
    internal ContentCatalog(ContentLoadPlan plan, IReadOnlyDictionary<string, IConfigTable> tables,
        IReadOnlySet<ContentReference> references,
        IReadOnlyDictionary<ContentReference, IReadOnlyList<ContentSource>> sources,
        IReadOnlyDictionary<(ContentReference Reference, string Field), ContentSource> fieldSources)
    {
        Snapshot = plan.Snapshot;
        Packages = plan.Packages;
        _tables = tables;
        _references = references;
        _sources = sources;
        _fieldSources = fieldSources;
    }

    /// <summary>Gets the exact content set and effective load sequence.</summary>
    public ContentSnapshot Snapshot
    {
        get;
    }
    /// <summary>Gets package declarations for diagnostics and explicit save compatibility decisions.</summary>
    public IReadOnlyList<ContentPack> Packages
    {
        get;
    }
    /// <summary>Gets registered table names in stable order.</summary>
    public IReadOnlyCollection<string> TableNames => [.. _tables.Keys.Order(StringComparer.Ordinal)];

    /// <summary>Checks whether a game or save reference resolves in this session.</summary>
    /// <param name="reference">A stable table and row identity.</param>
    /// <returns>Whether the referenced row exists.</returns>
    public bool Contains(ContentReference reference)
    {
        ArgumentNullException.ThrowIfNull(reference);
        return _references.Contains(reference);
    }

    /// <summary>Gets a typed read-only table; content does not select CLR types.</summary>
    /// <typeparam name="T">Game-registered DTO type.</typeparam>
    /// <param name="tableName">Game-registered table name.</param>
    /// <returns>The complete table for this session.</returns>
    public IConfigTable<ContentId, T> GetTable<T>(string tableName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        if (!_tables.TryGetValue(tableName, out var table))
            throw new KeyNotFoundException($"Content table '{tableName}' is not registered.");
        return table as IConfigTable<ContentId, T>
            ?? throw new InvalidOperationException($"Content table '{tableName}' has a different registered DTO type.");
    }

    /// <summary>Gets every add, replacement, patch or removal contribution to a record.</summary>
    /// <param name="reference">Record identity, including removed records.</param>
    /// <returns>Contributions in effective application order.</returns>
    public IReadOnlyList<ContentSource> GetSources(ContentReference reference)
    {
        ArgumentNullException.ThrowIfNull(reference);
        return _sources.TryGetValue(reference, out var sources) ? sources : [];
    }

    /// <summary>Finds the last operation supplying a field; array elements inherit the array's source.</summary>
    /// <param name="reference">Record identity.</param>
    /// <param name="fieldPath">Dot-separated field path.</param>
    /// <returns>The effective source, or null if no field is known.</returns>
    public ContentSource? GetFieldSource(ContentReference reference, string fieldPath)
    {
        ArgumentNullException.ThrowIfNull(reference);
        ArgumentNullException.ThrowIfNull(fieldPath);
        var current = fieldPath;
        while (true)
        {
            if (_fieldSources.TryGetValue((reference, current), out var source))
                return source;
            var separator = Math.Max(current.LastIndexOf('.'), current.LastIndexOf('['));
            if (separator < 0)
                return null;
            current = current[..separator];
        }
    }
}
