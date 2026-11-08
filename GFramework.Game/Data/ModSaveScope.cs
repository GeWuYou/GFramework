// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Data;

/// <summary>A mod-bound state capability; the game must expose only the matching handle to each mod.</summary>
public sealed class ModSaveScope
{
    private readonly ModSaveSession _session;
    private readonly string _modId;

    /// <summary>Creates a handle for an already validated active owner.</summary>
    internal ModSaveScope(ModSaveSession session, string modId)
    {
        _session = session;
        _modId = modId;
    }

    /// <summary>Reads an independent current-schema state DTO.</summary>
    /// <typeparam name="T">Plain state DTO.</typeparam>
    /// <returns>Stored data or null when absent.</returns>
    public T? Read<T>() where T : class => _session.Read<T>(_modId);

    /// <summary>Writes a draft without persisting the enclosing save.</summary>
    /// <typeparam name="T">Plain state DTO.</typeparam>
    /// <param name="data">State to store.</param>
    public void Write<T>(T data) where T : class => _session.Write(_modId, data);
}
