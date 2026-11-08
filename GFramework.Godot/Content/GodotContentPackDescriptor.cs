// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using GFramework.Game.Abstractions.Content;

namespace GFramework.Godot.Content;

/// <summary>Identifies an optional native resource pack selected by the host's content plan.</summary>
public sealed class GodotContentPackDescriptor
{
    /// <summary>Creates a native pack descriptor; its label is not evidence of trust.</summary>
    /// <param name="packId">Canonical lowercase content package ID.</param>
    /// <param name="packageDirectory">Host-selected physical package directory.</param>
    /// <param name="relativePackPath">Package-relative PCK filename.</param>
    public GodotContentPackDescriptor(string packId, string packageDirectory, string relativePackPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packId);
        if (!ContentId.IsCanonicalSegment(packId))
        {
            throw new ArgumentException("Native package IDs must use canonical content package syntax.", nameof(packId));
        }
        PackId = packId;
        FilePath = new GodotContentFileResolver(packageDirectory).Resolve(relativePackPath);
        if (!string.Equals(Path.GetExtension(FilePath), ".pck", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Official native packs must use the .pck extension.", nameof(relativePackPath));
        }
    }

    /// <summary>Gets the package identity used by the host trust policy.</summary>
    public string PackId
    {
        get;
    }

    /// <summary>Gets the validated physical PCK path.</summary>
    public string FilePath
    {
        get;
    }

    /// <summary>Gets the namespace required for resources exported by this package.</summary>
    public string ResourceRoot => "res://content/" + PackId + "/";
}
