// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;

namespace GFramework.Godot.Content;

/// <summary>
/// Resolves loose content files inside one physical package directory. The host supplies
/// the root; package data may supply only relative paths. Symbolic links are rejected so
/// a package cannot redirect reads outside its installation after textual validation.
/// </summary>
public sealed class GodotContentFileResolver
{
    private readonly string _root;

    /// <summary>Creates a resolver for an existing physical package directory.</summary>
    /// <param name="rootDirectory">The host-selected package directory, not a Godot virtual path.</param>
    public GodotContentFileResolver(string rootDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        _root = Path.GetFullPath(rootDirectory);
        RejectLinks(_root);
    }

    /// <summary>Resolves a package-relative path without allowing absolute paths, traversal or links.</summary>
    /// <param name="relativePath">A forward-slash separated package-relative path.</param>
    /// <returns>The validated physical file path.</returns>
    public string Resolve(string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        var normalized = relativePath.Replace('\\', '/');
        if (Path.IsPathRooted(relativePath) || normalized.StartsWith("/", StringComparison.Ordinal) ||
            normalized.Contains(':', StringComparison.Ordinal))
        {
            throw new ArgumentException("Content paths must be relative to their package.", nameof(relativePath));
        }

        var segments = normalized.Split('/');
        var current = _root;
        foreach (var segment in segments)
        {
            if (segment.Length == 0 || segment is "." or "..")
            {
                throw new ArgumentException("Content paths cannot contain empty or traversal segments.", nameof(relativePath));
            }
            current = Path.Combine(current, segment);
            RejectLinks(current);
        }
        return current;
    }

    private static void RejectLinks(string path)
    {
        if ((File.Exists(path) || Directory.Exists(path)) &&
            File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint))
        {
            throw new ArgumentException("Content roots and paths cannot contain symbolic links.", nameof(path));
        }
    }
}
