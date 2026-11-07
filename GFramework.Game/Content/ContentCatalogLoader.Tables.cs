// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Threading;
using System.Threading.Tasks;
using GFramework.Game.Abstractions.Config;
using GFramework.Game.Abstractions.Content;
using GFramework.Game.Config;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GFramework.Game.Content;

public sealed partial class ContentCatalogLoader
{
    private interface IContentTableDefinition
    {
        string Name { get; }
        Task<IConfigTable> BuildAsync(IReadOnlyDictionary<ContentReference, JObject> rows,
            IReadOnlyDictionary<(ContentReference Reference, string Field), ContentSource> fields,
            IReadOnlyDictionary<ContentReference, List<ContentSource>> sources, CancellationToken cancellationToken);
    }

    private sealed class ContentTableDefinition<T>(string name, string? schemaPath, Func<T, string> keySelector)
        : IContentTableDefinition
    {
        public string Name => name;

        public async Task<IConfigTable> BuildAsync(IReadOnlyDictionary<ContentReference, JObject> rows,
            IReadOnlyDictionary<(ContentReference Reference, string Field), ContentSource> fields,
            IReadOnlyDictionary<ContentReference, List<ContentSource>> sources, CancellationToken cancellationToken)
        {
            var schema = schemaPath is null ? null :
                await YamlConfigSchemaValidator.LoadAsync(name, schemaPath, cancellationToken).ConfigureAwait(false);
            var values = new List<T>();
            var deserializer = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
            foreach (var row in rows.Where(pair => string.Equals(pair.Key.TableName, name, StringComparison.Ordinal))
                         .OrderBy(static pair => pair.Key.Id.ToString(), StringComparer.Ordinal))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var reference = row.Key;
                var source = sources[reference][^1];
                try
                {
                    var document = row.Value.ToString(Formatting.None);
                    if (schema is not null)
                        ValidateReferences(reference, schema, document, source, rows, fields, sources);
                    var value = deserializer.Deserialize<T>(document);
                    if (value is null || new ContentId(keySelector(value)) != reference.Id)
                        throw Error("ContentIdChanged", FindFieldSource(reference, "id", fields, source), reference,
                            "id", "The DTO's ID must equal the operation target; IDs cannot be renamed by a patch.");
                    values.Add(value);
                }
                catch (ContentLoadException) { throw; }
                catch (ConfigLoadException exception)
                {
                    var field = exception.Diagnostic.DisplayPath;
                    throw Error("SchemaValidationFailed", FindFieldSource(reference, field, fields, source),
                        reference, field, exception.Message, exception);
                }
                catch (Exception exception) when (exception is not OutOfMemoryException)
                {
                    throw Error("ContentBindingFailed", source, reference, null, exception.Message, exception);
                }
            }
            return new InMemoryConfigTable<ContentId, T>(values, value => new ContentId(keySelector(value)));
        }

        private static void ValidateReferences(ContentReference owner, YamlConfigSchema schema, string document,
            ContentSource source, IReadOnlyDictionary<ContentReference, JObject> rows,
            IReadOnlyDictionary<(ContentReference Reference, string Field), ContentSource> fields,
            IReadOnlyDictionary<ContentReference, List<ContentSource>> sources)
        {
            // The existing validator owns schema semantics. Content IDs replace only the old loader's
            // key-conversion phase; checking the complete merged row set permits forward references.
            var usages = YamlConfigSchemaValidator.ValidateAndCollectReferences(owner.TableName, schema,
                source.FilePath, document);
            foreach (var usage in usages)
            {
                ContentId id;
                try { id = new ContentId(usage.RawValue); }
                catch (ArgumentException exception)
                {
                    throw Error("InvalidContentReference", FindFieldSource(owner, usage.DisplayPath, fields, source),
                        owner, usage.DisplayPath, "Content references must use namespaced string IDs.", exception);
                }
                var target = new ContentReference(usage.ReferencedTableName, id);
                if (!rows.ContainsKey(target))
                {
                    var origin = FindFieldSource(owner, usage.DisplayPath, fields, source);
                    var removal = sources.TryGetValue(target, out var history) ? history[^1] : null;
                    throw new ContentLoadException(new ContentLoadDiagnostic
                    {
                        Code = "MissingContentReference", PackId = origin.PackId, PackVersion = origin.PackVersion,
                        FilePath = origin.FilePath, TableName = owner.TableName, ContentId = owner.Id.ToString(),
                        FieldPath = usage.DisplayPath, RelatedPackId = removal?.PackId ?? id.PackId,
                        Detail = $"Content '{owner.Id}' references missing '{target.TableName}/{target.Id}'." +
                            (removal is null ? string.Empty : $" Last target operation came from '{removal.PackId}' in '{removal.FilePath}'.")
                    });
                }
            }
        }
    }

    private static ContentSource FindFieldSource(ContentReference reference, string? path,
        IReadOnlyDictionary<(ContentReference Reference, string Field), ContentSource> fields, ContentSource fallback)
    {
        var current = path ?? string.Empty;
        while (true)
        {
            if (fields.TryGetValue((reference, current), out var source)) return source;
            var separator = Math.Max(current.LastIndexOf('.'), current.LastIndexOf('['));
            if (separator < 0) return fallback;
            current = current[..separator];
        }
    }
}
