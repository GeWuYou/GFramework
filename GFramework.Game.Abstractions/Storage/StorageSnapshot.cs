// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace GFramework.Game.Abstractions.Storage;

/// <summary>
/// Owns an immutable copy of a serialized file, including fields unknown to a game DTO.
/// Used for recovery approval fingerprints and lossless backups rather than reserializing old data.
/// </summary>
public sealed class StorageSnapshot
{
    private readonly byte[] _bytes;

    /// <summary>Copies bytes so later caller mutation cannot change an approved snapshot.</summary>
    /// <param name="bytes">Complete original file bytes.</param>
    public StorageSnapshot(byte[] bytes)
    {
        if (bytes is null)
            throw new ArgumentNullException(nameof(bytes));
        _bytes = (byte[])bytes.Clone();
        using var algorithm = SHA256.Create();
        Fingerprint = BitConverter.ToString(algorithm.ComputeHash(_bytes)).Replace("-", string.Empty, StringComparison.Ordinal);
    }

    /// <summary>Gets the SHA-256 fingerprint of the original file bytes.</summary>
    public string Fingerprint
    {
        get;
    }

    /// <summary>Returns a copy suitable for an atomic raw-file write.</summary>
    /// <returns>Original bytes, with independent ownership.</returns>
    public byte[] ToArray() => (byte[])_bytes.Clone();

    /// <summary>Decodes serialized text using the same BOM detection as ordinary file storage reads.</summary>
    /// <returns>Serialized text, without changing the original backup bytes.</returns>
    public string ReadText()
    {
        using var stream = new MemoryStream(_bytes, writable: false);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }
}
