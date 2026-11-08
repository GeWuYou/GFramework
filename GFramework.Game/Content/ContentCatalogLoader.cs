// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using GFramework.Game.Abstractions.Config;
using GFramework.Game.Abstractions.Content;
using GFramework.Game.Modding;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GFramework.Game.Content;

/// <summary>
/// Builds an isolated content session from game-owned table definitions. Registration precedes loading;
/// the returned catalog is published only when every package and cross-table reference validates.
/// The loader is configured and used by one startup owner, not mutated concurrently.
/// </summary>
public sealed partial class ContentCatalogLoader
{
    private readonly Dictionary<string, IContentTableDefinition> _definitions = new(StringComparer.Ordinal);
    private bool _started;

    /// <summary>Registers the DTO and optional schema the game accepts for an extensible table.</summary>
    /// <typeparam name="T">Game-owned DTO type; content files never supply CLR type names.</typeparam>
    /// <param name="tableName">Stable table name.</param>
    /// <param name="schemaPath">Host-owned schema file path, or null for explicit game-side validation.</param>
    /// <param name="keySelector">Extracts the persistent string ID from a typed DTO.</param>
    /// <returns>This loader for composition.</returns>
    public ContentCatalogLoader RegisterTable<T>(string tableName, string? schemaPath, Func<T, string> keySelector)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        ArgumentNullException.ThrowIfNull(keySelector);
        if (_started)
            throw new InvalidOperationException("Content table registration is closed after loading starts.");
        if (!_definitions.TryAdd(tableName, new ContentTableDefinition<T>(tableName, schemaPath, keySelector)))
            throw new InvalidOperationException($"Content table '{tableName}' is already registered.");
        return this;
    }

    /// <summary>Applies a complete resolved plan and validates final data before returning it.</summary>
    /// <param name="plan">Dependency-resolved package sequence.</param>
    /// <param name="cancellationToken">Cancels preparation; no catalog is published on cancellation.</param>
    /// <returns>A fully validated content session.</returns>
    public Task<ContentCatalog> LoadAsync(ContentLoadPlan plan, CancellationToken cancellationToken = default)
        => LoadAsync(plan, admission: null, cancellationToken);

    /// <summary>Loads a plan whose code was approved and loaded before native resource preparation.</summary>
    /// <param name="plan">The exact plan used to prepare the mod host.</param>
    /// <param name="admission">Live mod-host admission; required whenever the plan contains code.</param>
    /// <param name="cancellationToken">Cancels data preparation.</param>
    /// <returns>A validated catalog. Start the mod host before publishing gameplay.</returns>
    public async Task<ContentCatalog> LoadAsync(ContentLoadPlan plan, CodeModAdmission? admission,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (plan.Packages.Any(static pack => pack.Manifest.Code is not null))
        {
            if (admission is null)
                throw new InvalidOperationException("Code packages require explicit GameModHost admission.");
            admission.Validate(plan);
        }
        _started = true;
        var rows = new Dictionary<ContentReference, JObject>();
        var sources = new Dictionary<ContentReference, List<ContentSource>>();
        var fields = new Dictionary<(ContentReference Reference, string Field), ContentSource>();
        foreach (var package in plan.Packages)
        {
            foreach (var file in package.Manifest.ContentFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ApplyFileAsync(package, file, rows, sources, fields, cancellationToken).ConfigureAwait(false);
            }
        }
        cancellationToken.ThrowIfCancellationRequested();
        var tables = new Dictionary<string, IConfigTable>(StringComparer.Ordinal);
        foreach (var definition in _definitions.Values)
        {
            var table = await definition.BuildAsync(rows, fields, sources, cancellationToken).ConfigureAwait(false);
            tables.Add(definition.Name, table);
        }
        // Discovery and preparation are distinct IO phases. Refuse publication when an installer or
        // editor changed any input in between; a save must never record a fingerprint for other data.
        foreach (var package in plan.Packages)
        {
            try
            {
                var fingerprint = await ContentPackDiscovery.ComputeFingerprintAsync(package.RootPath, cancellationToken)
                    .ConfigureAwait(false);
                if (!string.Equals(fingerprint, package.Fingerprint, StringComparison.Ordinal))
                    throw new IOException("Package files changed after discovery; restart discovery before loading content.");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                throw new ContentLoadException(new ContentLoadDiagnostic
                {
                    Code = "PackageChanged",
                    PackId = package.Manifest.Id,
                    PackVersion = package.Manifest.Version,
                    Detail = exception.Message,
                }, exception);
            }
        }
        return new ContentCatalog(plan, new ReadOnlyDictionary<string, IConfigTable>(tables), rows.Keys.ToHashSet(),
            new ReadOnlyDictionary<ContentReference, IReadOnlyList<ContentSource>>(sources.ToDictionary(
                static pair => pair.Key, static pair => (IReadOnlyList<ContentSource>)pair.Value.AsReadOnly())),
            new ReadOnlyDictionary<(ContentReference Reference, string Field), ContentSource>(fields));
    }

    private async Task ApplyFileAsync(ContentPack pack, string file, Dictionary<ContentReference, JObject> rows,
        Dictionary<ContentReference, List<ContentSource>> sources,
        Dictionary<(ContentReference Reference, string Field), ContentSource> fields, CancellationToken cancellationToken)
    {
        JArray operations;
        try
        {
            operations = ContentDocumentParser.Parse(await pack.ReadTextAsync(file, cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or
            ArgumentException or InvalidOperationException or YamlDotNet.Core.YamlException)
        {
            throw Error("InvalidContentFile", new ContentSource(pack.Manifest.Id, pack.Manifest.Version, file, 0),
reference: null, field: null, exception.Message, exception);
        }
        for (var index = 0; index < operations.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var source = new ContentSource(pack.Manifest.Id, pack.Manifest.Version, file, index);
            try
            {
                ApplyOperation(operations[index], source, rows, sources, fields);
            }
            catch (ContentLoadException) { throw; }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
            {
                throw Error("InvalidOperation", source, reference: null, field: null, exception.Message, exception);
            }
        }
    }

    private void ApplyOperation(JToken operation, ContentSource source, Dictionary<ContentReference, JObject> rows,
        Dictionary<ContentReference, List<ContentSource>> sources,
        Dictionary<(ContentReference Reference, string Field), ContentSource> fields)
    {
        if (operation is not JObject mapping || mapping.Properties().Any(static property =>
                property.Name is not "op" and not "table" and not "id" and not "value"))
            throw new InvalidOperationException("Operations must be objects with op, table, id and optional value.");
        var op = RequiredString(mapping, "op");
        var tableName = RequiredString(mapping, "table");
        var id = new ContentId(RequiredString(mapping, "id"));
        var reference = new ContentReference(tableName, id);
        if (!_definitions.ContainsKey(tableName))
            throw Error("UnknownTable", source, reference, field: null, $"Table '{tableName}' is not registered by the game.");
        rows.TryGetValue(reference, out var existing);
        if (string.Equals(op, "add", StringComparison.Ordinal) &&
            (!string.Equals(id.PackId, source.PackId, StringComparison.Ordinal) || existing is not null))
            throw Error("DuplicateOrForeignContent", source, reference, field: null,
                "Add requires a new ID owned by the contributing package; use replace or patch for existing content.");
        if (op is "replace" or "patch" or "remove" && existing is null)
            throw Error("MissingContentTarget", source, reference, field: null, $"Cannot {op} missing content '{id}'.");
        if (string.Equals(op, "remove", StringComparison.Ordinal))
        {
            if (mapping.ContainsKey("value"))
                throw Error("InvalidOperation", source, reference, field: null, "Remove cannot contain a value.");
            rows.Remove(reference);
            RemoveFieldSources(fields, reference, string.Empty);
        }
        else
        {
            if (op is not "add" and not "replace" and not "patch")
                throw Error("UnknownOperation", source, reference, field: null, $"Unknown content operation '{op}'.");
            if (mapping["value"] is not JObject value)
                throw Error("InvalidOperation", source, reference, field: null, "Add, replace and patch need an object value.");
            if (string.Equals(op, "patch", StringComparison.Ordinal))
            {
                var merged = (JObject)existing!.DeepClone();
                MergePatch(merged, value, reference, string.Empty, source, fields);
                rows[reference] = merged;
            }
            else
            {
                RemoveFieldSources(fields, reference, string.Empty);
                rows[reference] = (JObject)value.DeepClone();
                RecordFields(rows[reference], reference, string.Empty, source, fields);
            }
        }
        if (!sources.TryGetValue(reference, out var history))
        {
            history = [];
            sources.Add(reference, history);
        }
        history.Add(source);
    }

    private static string RequiredString(JObject mapping, string property)
        => mapping[property] is JValue { Type: JTokenType.String } value && value.Value<string>() is { Length: > 0 } text
            ? text : throw new InvalidOperationException($"Operation '{property}' must be a nonempty string.");

    private static void MergePatch(JObject target, JObject patch, ContentReference reference, string prefix,
        ContentSource source, Dictionary<(ContentReference Reference, string Field), ContentSource> fields)
    {
        foreach (var property in patch.Properties())
        {
            var path = prefix.Length == 0 ? property.Name : prefix + "." + property.Name;
            if (property.Value.Type == JTokenType.Null)
            {
                target.Remove(property.Name);
                RemoveFieldSources(fields, reference, path);
                // Keep deletion provenance so missing-required-field errors identify the deleting patch.
                fields[(reference, path)] = source;
            }
            else if (property.Value is JObject nested)
            {
                if (target[property.Name] is not JObject existing)
                {
                    existing = [];
                    target[property.Name] = existing;
                    RemoveFieldSources(fields, reference, path);
                    fields[(reference, path)] = source;
                }
                MergePatch(existing, nested, reference, path, source, fields);
            }
            else
            {
                target[property.Name] = property.Value.DeepClone();
                RemoveFieldSources(fields, reference, path);
                RecordFields(property.Value, reference, path, source, fields);
            }
        }
    }

    private static void RecordFields(JToken token, ContentReference reference, string path, ContentSource source,
        Dictionary<(ContentReference Reference, string Field), ContentSource> fields)
    {
        fields[(reference, path)] = source;
        if (token is not JObject mapping)
            return;
        foreach (var property in mapping.Properties())
            RecordFields(property.Value, reference, path.Length == 0 ? property.Name : path + "." + property.Name, source, fields);
    }

    private static void RemoveFieldSources(Dictionary<(ContentReference Reference, string Field), ContentSource> fields,
        ContentReference reference, string prefix)
    {
        foreach (var key in fields.Keys.Where(key => key.Reference == reference &&
                     (prefix.Length == 0 || string.Equals(key.Field, prefix, StringComparison.Ordinal) ||
                      key.Field.StartsWith(prefix + ".", StringComparison.Ordinal))).ToArray())
            fields.Remove(key);
    }

    private static ContentLoadException Error(string code, ContentSource source, ContentReference? reference,
        string? field, string detail, Exception? inner = null)
        => new(new ContentLoadDiagnostic
        {
            Code = code,
            PackId = source.PackId,
            PackVersion = source.PackVersion,
            FilePath = source.FilePath,
            TableName = reference?.TableName,
            ContentId = reference?.Id.ToString(),
            FieldPath = field,
            Detail = detail,
        }, inner);
}
