// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Godot;

namespace GFramework.Godot.Content;

/// <summary>
/// Mounts host-trusted PCKs once during startup, before any of their resources are loaded.
/// Create and use this service on the Godot thread. It validates the complete selection
/// before mounting, disables file replacement and fails closed after a native mount failure.
/// Godot has no matching unmount API: a failed mount session requires process restart.
/// </summary>
public sealed class GodotContentPackMounter
{
    private readonly Func<GodotContentPackDescriptor, string, bool>? _trust;
    private readonly Func<string, bool> _mount;
    private readonly int _threadId;
    private readonly HashSet<string> _mounted = new(StringComparer.Ordinal);
    private bool _attempted;

    /// <summary>Creates a startup mounter; a missing trust policy rejects every native pack.</summary>
    /// <param name="trustPolicy">Host-owned verification of package ID and actual lowercase SHA256 digest.</param>
    public GodotContentPackMounter(Func<GodotContentPackDescriptor, string, bool>? trustPolicy = null)
        : this(trustPolicy, static path => ProjectSettings.LoadResourcePack(path, replaceFiles: false))
    {
    }

    /// <summary>Creates a mounter with a native boundary seam for managed tests.</summary>
    internal GodotContentPackMounter(Func<GodotContentPackDescriptor, string, bool>? trustPolicy, Func<string, bool> mount)
    {
        _trust = trustPolicy;
        _mount = mount ?? throw new ArgumentNullException(nameof(mount));
        _threadId = System.Environment.CurrentManagedThreadId;
    }

    /// <summary>Gets whether mounting failed after entering the irreversible native operation.</summary>
    public bool RequiresRestart
    {
        get; private set;
    }

    /// <summary>Verifies every selected PCK before mounting any, then mounts in effective content order.</summary>
    /// <param name="packs">The host's final active package selection.</param>
    /// <exception cref="InvalidOperationException">The thread, lifetime, trust or native mount contract failed.</exception>
    public void MountAll(IReadOnlyList<GodotContentPackDescriptor> packs)
    {
        ArgumentNullException.ThrowIfNull(packs);
        RequireHostThread();
        if (_attempted)
        {
            throw new InvalidOperationException("Native content was already selected; restart the game to change packs.");
        }
        var paths = new HashSet<string>(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        foreach (var pack in packs)
        {
            ArgumentNullException.ThrowIfNull(pack, nameof(packs));
            if (!paths.Add(pack.FilePath))
            {
                throw new InvalidOperationException("Duplicate native pack: " + pack.PackId);
            }
            using var file = File.OpenRead(pack.FilePath);
            var digest = Convert.ToHexString(SHA256.HashData(file)).ToLowerInvariant();
            if (_trust is null || !_trust(pack, digest))
            {
                throw new InvalidOperationException("Native content is not trusted by this game: " + pack.PackId);
            }
        }

        _attempted = true;
        try
        {
            foreach (var pack in packs)
            {
                if (!_mount(pack.FilePath))
                {
                    throw new IOException("Godot could not mount package '" + pack.PackId + "'. Restart is required.");
                }
                _mounted.Add(pack.PackId);
            }
        }
        catch
        {
            // Native mounting is not transactional, including a false result from the first attempt.
            RequiresRestart = true;
            throw;
        }
    }

    /// <summary>Returns a path inside an already trusted and mounted package's namespace.</summary>
    /// <param name="packId">The mounted package identity.</param>
    /// <param name="relativePath">A nonempty relative resource path inside its namespace.</param>
    /// <returns>A Godot virtual resource path.</returns>
    public string GetResourcePath(string packId, string relativePath)
    {
        RequireHostThread();
        if (RequiresRestart || !_mounted.Contains(packId))
        {
            throw new InvalidOperationException("No usable native content session for package: " + packId);
        }
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        var normalized = relativePath.Replace('\\', '/');
        foreach (var segment in normalized.Split('/'))
        {
            if (segment.Length == 0 || segment is "." or ".." || segment.Contains(':', StringComparison.Ordinal))
            {
                throw new ArgumentException("Native resource paths must stay in their package namespace.", nameof(relativePath));
            }
        }
        return "res://content/" + packId + "/" + normalized;
    }

    private void RequireHostThread()
    {
        if (System.Environment.CurrentManagedThreadId != _threadId)
        {
            throw new InvalidOperationException("Native content operations must run on the owning Godot thread.");
        }
    }
}
