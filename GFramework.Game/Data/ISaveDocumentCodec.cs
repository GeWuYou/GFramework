// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Storage;
using Newtonsoft.Json.Linq;

namespace GFramework.Game.Data;

/// <summary>Separates document storage encoding and plain DTO conversion from repository orchestration.</summary>
public interface ISaveDocumentCodec
{
    /// <summary>Decodes and validates an envelope without deserializing its game payload.</summary>
    /// <param name="snapshot">Complete encoded document.</param>
    /// <returns>A privately owned mutable document.</returns>
    SaveDocument Decode(StorageSnapshot snapshot);

    /// <summary>Encodes a complete document for one atomic storage replacement.</summary>
    /// <param name="document">Document to encode.</param>
    /// <returns>Immutable encoded bytes.</returns>
    StorageSnapshot Encode(SaveDocument document);

    /// <summary>Converts a game DTO into plain data without CLR type metadata.</summary>
    /// <typeparam name="T">DTO type.</typeparam>
    /// <param name="data">DTO to encode.</param>
    /// <returns>A newly owned JSON object.</returns>
    JObject SerializeData<T>(T data) where T : class;

    /// <summary>Constructs the current DTO only after document transformations have succeeded.</summary>
    /// <typeparam name="T">Current DTO type.</typeparam>
    /// <param name="data">Current-schema plain data.</param>
    /// <returns>A newly constructed DTO.</returns>
    T DeserializeData<T>(JObject data) where T : class;
}
