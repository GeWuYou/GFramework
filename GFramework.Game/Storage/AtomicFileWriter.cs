// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace GFramework.Game.Storage;

/// <summary>
///     Writes a complete text file beside its destination before replacing that destination.
///     Storage implementations retain responsibility for serialization and per-path locking.
/// </summary>
internal static class AtomicFileWriter
{
    /// <summary>
    ///     Writes UTF-8 content to a unique sibling temporary file and replaces the destination only after closing it.
    /// </summary>
    /// <param name="path">Physical destination path, including its complete file name.</param>
    /// <param name="content">Already serialized content.</param>
    /// <param name="bufferSize">File stream buffer size.</param>
    /// <returns>A task that completes after replacement or reports the original write failure.</returns>
    /// <remarks>
    ///     The temporary file is on the same file system as the destination. This is a single-file replacement,
    ///     not a multi-file transaction or a guarantee of durability after a device loses power.
    /// </remarks>
    internal static Task WriteAllTextAsync(string path, string content, int bufferSize = 8192)
    {
        return WriteAsync(path, async stream =>
        {
            // Godot's GetAsText and ordinary .NET readers must see the same JSON, without a leading BOM character.
            var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), leaveOpen: true);
            await using var configuredWriter = writer.ConfigureAwait(false);
            await writer.WriteAsync(content).ConfigureAwait(false);
            await writer.FlushAsync().ConfigureAwait(false);
        }, bufferSize);
    }

    /// <summary>
    ///     Runs a complete write against a temporary stream and commits it only when the writer succeeds.
    /// </summary>
    /// <param name="path">Physical destination path.</param>
    /// <param name="writeAsync">Writes the complete replacement; must not retain the stream after returning.</param>
    /// <param name="bufferSize">File stream buffer size.</param>
    /// <returns>A task that preserves the original destination if writing or replacement fails.</returns>
    internal static async Task WriteAsync(string path, Func<Stream, Task> writeAsync, int bufferSize = 8192)
    {
        var fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        // Independent storage instances must never share a temporary file, even if their locks are not shared.
        var tempPath = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";

        try
        {
            {
                var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                    bufferSize, useAsync: true);
                await using var configuredStream = stream.ConfigureAwait(false);
                await writeAsync(stream).ConfigureAwait(false);
            }

            File.Move(tempPath, fullPath, overwrite: true);
        }
        catch
        {
            // Cleanup must not replace the write/rename error that tells the caller why saving failed.
            try
            {
                File.Delete(tempPath);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }

            throw;
        }
    }
}
