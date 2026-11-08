// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Data;

/// <summary>
/// Game-owned complete mod snapshot for one run. A loaded session retains that run's unavailable owners;
/// a new session starts empty. A scoped handle can only address its assigned mod namespace.
/// Use on the game's owning thread; persistence takes a deep snapshot and never stores runtime mod objects.
/// </summary>
public sealed class ModSaveSession
{
    private readonly Dictionary<string, SaveDataBlock> _blocks;
    private readonly IReadOnlyDictionary<string, ModSavePolicy> _policies;
    private readonly ISaveDocumentCodec _codec;

    /// <summary>Creates a private draft, retaining inactive blocks but only exposing active owners.</summary>
    internal ModSaveSession(Guid owner, IEnumerable<KeyValuePair<string, SaveDataBlock>> blocks, IReadOnlyDictionary<string, ModSavePolicy> policies,
        ISaveDocumentCodec codec)
    {
        Owner = owner;
        _blocks = blocks.ToDictionary(static pair => pair.Key, static pair => pair.Value.Clone(), StringComparer.Ordinal);
        _policies = policies;
        _codec = codec;
    }

    /// <summary>Gets the issuing repository, preventing cross-game draft substitution.</summary>
    internal Guid Owner
    {
        get;
    }

    /// <summary>Returns a capability bound to one active mod; callers cannot choose another namespace on that handle.</summary>
    /// <param name="modId">Active mod ID.</param>
    /// <returns>A scoped data handle.</returns>
    public ModSaveScope ForMod(string modId)
    {
        if (!_policies.ContainsKey(modId))
            throw new InvalidOperationException("Mod data was not enabled for: " + modId);
        return new ModSaveScope(this, modId);
    }

    /// <summary>Captures all blocks, including unavailable mod data, without sharing mutable state.</summary>
    internal Dictionary<string, SaveDataBlock> Snapshot() =>
        _blocks.ToDictionary(static pair => pair.Key, static pair => pair.Value.Clone(), StringComparer.Ordinal);

    /// <summary>Returns a fresh DTO for the current owner or null when that owner has not saved any state.</summary>
    internal T? Read<T>(string modId) where T : class =>
        _blocks.TryGetValue(modId, out var block) ? _codec.DeserializeData<T>(block.Data) : null;

    /// <summary>Replaces only this active owner's payload; unavailable blocks cannot be implicitly deleted.</summary>
    internal void Write<T>(string modId, T data) where T : class
    {
        ArgumentNullException.ThrowIfNull(data);
        _blocks[modId] = new SaveDataBlock { Version = _policies[modId].CurrentVersion, Data = _codec.SerializeData(data) };
    }
}
