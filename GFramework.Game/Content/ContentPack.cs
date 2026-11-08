// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GFramework.Game.Abstractions.Content;

namespace GFramework.Game.Content;

/// <summary>A discovered package with scoped file access; paths never grant script execution or host trust.</summary>
public sealed class ContentPack
{
    /// <summary>Creates a package descriptor, normally through discovery.</summary>
    /// <param name="rootPath">Physical package root.</param>
    /// <param name="manifest">Validated manifest.</param>
    /// <param name="fingerprint">Fingerprint of the package's file snapshot.</param>
    public ContentPack(string rootPath, ContentPackManifest manifest, string fingerprint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentException.ThrowIfNullOrWhiteSpace(fingerprint);
        RootPath = Path.GetFullPath(rootPath);
        Manifest = manifest;
        Fingerprint = fingerprint;
    }

    /// <summary>Gets the physical package root, for host-specific loading only.</summary>
    public string RootPath
    {
        get;
    }
    /// <summary>Gets package declarations; these are data, not a trust decision.</summary>
    public ContentPackManifest Manifest
    {
        get;
    }
    /// <summary>Gets the exact discovered file fingerprint.</summary>
    public string Fingerprint
    {
        get;
    }

    /// <summary>Resolves a package-relative file after rejecting rooted paths, traversal and symbolic links.</summary>
    /// <param name="relativePath">A slash-separated package-relative path.</param>
    /// <returns>Validated physical file path.</returns>
    public string GetFilePath(string relativePath)
    {
        ValidateRelativePath(relativePath);
        RejectLink(RootPath);
        var current = RootPath;
        foreach (var segment in relativePath.Split('/'))
        {
            current = Path.Combine(current, segment);
            RejectLink(current);
        }
        return current;
    }

    /// <summary>Reads a bounded data document from this package without interpreting runtime types.</summary>
    /// <param name="relativePath">Package-relative document path.</param>
    /// <param name="cancellationToken">Cancels document IO.</param>
    /// <returns>Document text.</returns>
    public async Task<string> ReadTextAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var path = GetFilePath(relativePath);
        if (new FileInfo(path).Length > 4 * 1024 * 1024)
            throw new IOException("A content data document cannot exceed 4 MiB.");
        return await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Checks a path's portable syntax before any filesystem access.</summary>
    /// <param name="relativePath">Untrusted relative path.</param>
    public static void ValidateRelativePath(string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        if (Path.IsPathRooted(relativePath) || relativePath.Contains(':', StringComparison.Ordinal) ||
            relativePath.Contains('\\', StringComparison.Ordinal) ||
            relativePath.Split('/').Any(static segment => segment is "" or "." or ".."))
            throw new ArgumentException("Content paths must be relative, slash-separated, and remain inside their package.", nameof(relativePath));
    }

    /// <summary>Rejects filesystem redirects, including links to otherwise valid package files.</summary>
    /// <param name="path">Existing file or directory to inspect.</param>
    internal static void RejectLink(string path)
    {
        if (File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint))
            throw new IOException("Content package paths cannot contain symbolic links or reparse points.");
    }
}
