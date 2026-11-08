// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Text;
using GFramework.Game.Abstractions.Content;
using GFramework.Game.Abstractions.Storage;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GFramework.Game.Data;

/// <summary>
/// Encodes the stable save envelope as UTF-8 JSON. TypeNameHandling is disabled and metadata properties
/// are not interpreted; no file-controlled CLR type activation is used. Unknown payload fields survive conversion.
/// </summary>
public sealed class JsonSaveDocumentCodec : ISaveDocumentCodec
{
    /// <inheritdoc />
    public SaveDocument Decode(StorageSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        using var reader = new JsonTextReader(new StringReader(snapshot.ReadText()))
        {
            MaxDepth = 64, DateParseHandling = DateParseHandling.None
        };
        var root = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
        if (reader.Read()) throw new InvalidOperationException("A save must contain exactly one JSON document.");
        ValidateEnvelopeShape(root);
        var document = root.ToObject<SaveDocument>(CreateSerializer())
            ?? throw new InvalidOperationException("A save envelope cannot be null.");
        ValidateDocument(document);
        return document;
    }

    /// <inheritdoc />
    public StorageSnapshot Encode(SaveDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        ValidateDocument(document);
        var root = JObject.FromObject(document, CreateSerializer());
        return new StorageSnapshot(Encoding.UTF8.GetBytes(root.ToString(Formatting.None)));
    }

    /// <inheritdoc />
    public JObject SerializeData<T>(T data) where T : class
    {
        ArgumentNullException.ThrowIfNull(data);
        return JObject.FromObject(data, CreateSerializer());
    }

    /// <inheritdoc />
    public T DeserializeData<T>(JObject data) where T : class
    {
        ArgumentNullException.ThrowIfNull(data);
        return data.ToObject<T>(CreateSerializer())
            ?? throw new InvalidOperationException("The game payload cannot deserialize to null.");
    }

    /// <summary>Creates operation-local serializer state, avoiding mutable shared converter settings.</summary>
    private static Newtonsoft.Json.JsonSerializer CreateSerializer() => Newtonsoft.Json.JsonSerializer.Create(new JsonSerializerSettings
    {
        TypeNameHandling = TypeNameHandling.None,
        MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
        DateParseHandling = DateParseHandling.None,
        MaxDepth = 64
    });

    /// <summary>Requires explicit metadata instead of interpreting missing fields through CLR property defaults.</summary>
    private static void ValidateEnvelopeShape(JObject root)
    {
        if (root["FormatVersion"]?.Type != JTokenType.Integer || root["GameId"]?.Type != JTokenType.String ||
            root["Game"] is not JObject game || root["Mods"] is not JObject mods ||
            !root.ContainsKey("Content"))
            throw new InvalidOperationException("A save requires explicit FormatVersion, GameId, Game, Content and Mods metadata.");
        ValidateBlockShape(game);
        foreach (var property in mods.Properties())
        {
            if (property.Value is not JObject block) throw new InvalidOperationException("A mod block must be an object.");
            ValidateBlockShape(block);
        }
        if (root["Content"]?.Type == JTokenType.Null) return;
        if (root["Content"] is not JObject content || content["Packs"] is not JArray packs)
            throw new InvalidOperationException("Content metadata requires an explicit package array.");
        foreach (var pack in packs)
            if (pack is not JObject stamp || stamp["Id"]?.Type != JTokenType.String ||
                stamp["Version"]?.Type != JTokenType.String || stamp["Fingerprint"]?.Type != JTokenType.String)
                throw new InvalidOperationException("Content stamps require explicit ID, version and fingerprint.");
    }

    /// <summary>Requires schema/data declarations for every known or opaque owner.</summary>
    private static void ValidateBlockShape(JObject block)
    {
        if (block["Version"]?.Type != JTokenType.Integer || block["Data"] is not JObject)
            throw new InvalidOperationException("Save blocks require explicit Version and Data properties.");
    }

    /// <summary>Checks outer metadata and all opaque blocks before callbacks or DTO activation.</summary>
    private static void ValidateDocument(SaveDocument document)
    {
        if (document.FormatVersion != 1 || string.IsNullOrWhiteSpace(document.GameId))
            throw new InvalidOperationException("Unknown save envelope format or missing game identity.");
        ValidateBlock(document.Game);
        if (document.Mods is null) throw new InvalidOperationException("A save requires a mod data map.");
        foreach (var pair in document.Mods)
        {
            if (!ContentId.IsCanonicalSegment(pair.Key)) throw new InvalidOperationException("Invalid mod data namespace.");
            ValidateBlock(pair.Value);
        }
        if (document.Content is not null)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var pack in document.Content.Packs)
            {
                if (pack is null || !ContentId.IsCanonicalSegment(pack.Id) || !ids.Add(pack.Id) ||
                    string.IsNullOrWhiteSpace(pack.Version) || string.IsNullOrWhiteSpace(pack.Fingerprint))
                    throw new InvalidOperationException("Invalid or duplicate save content metadata.");
            }
        }
    }

    /// <summary>Rejects malformed blocks independently of whether their owner is currently installed.</summary>
    private static void ValidateBlock(SaveDataBlock? block)
    {
        if (block is null || block.Version < 1 || block.Data is null)
            throw new InvalidOperationException("Save blocks require a positive schema version and object payload.");
    }
}
