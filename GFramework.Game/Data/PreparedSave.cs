// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Content;
using GFramework.Game.Abstractions.Data;
using GFramework.Game.Abstractions.Storage;

namespace GFramework.Game.Data;

/// <summary>
/// Immutable approval candidate. Public DTO/document/session access returns independent copies; commit uses
/// the private encoded candidate, never a caller-mutated object. Preparing never persists or activates gameplay.
/// </summary>
/// <typeparam name="TSaveData">Current game DTO.</typeparam>
public sealed class PreparedSave<TSaveData> where TSaveData : class, IData, new()
{
    private readonly ISaveDocumentCodec _codec;
    private readonly IReadOnlyDictionary<string, ModSavePolicy> _modPolicies;

    /// <summary>Captures exact candidate bytes and the inspection inputs.</summary>
    internal PreparedSave(SavePreparationState state, ISaveDocumentCodec codec,
        IReadOnlyDictionary<string, ModSavePolicy> modPolicies)
    {
        Owner = state.Owner;
        Slot = state.Slot;
        Original = state.Original;
        Candidate = state.Candidate;
        _codec = codec;
        CurrentContent = state.CurrentContent;
        Issues = Array.AsReadOnly(state.Issues.ToArray());
        MigrationPath = Array.AsReadOnly(state.MigrationPath.ToArray());
        RequiresCommit = state.RequiresCommit;
        RequiresConfirmation = state.RequiresConfirmation;
        _modPolicies = modPolicies;
    }

    /// <summary>Gets the inspected slot.</summary>
    public int Slot { get; }
    /// <summary>Gets a fresh current DTO; editing it does not change the approved candidate.</summary>
    public TSaveData Data => _codec.DeserializeData<TSaveData>(_codec.Decode(Candidate).Game.Data);
    /// <summary>Gets an independent document for inspection.</summary>
    public SaveDocument Document => _codec.Decode(Candidate);
    /// <summary>Gets the original package stamps for displaying source versions without exposing raw source bytes.</summary>
    public ContentSnapshot? SavedContent => _codec.Decode(Original).Content;
    /// <summary>Gets an independent mod draft, including opaque data from unavailable mods.</summary>
    public ModSaveSession ModState => new(Owner, Document.Mods, _modPolicies, _codec);
    /// <summary>Gets whether transformed or updated metadata must be explicitly committed before ordinary loading.</summary>
    public bool RequiresCommit { get; }
    /// <summary>Gets whether downgrade, lossy conversion or content repair needs an explicit player decision.</summary>
    public bool RequiresConfirmation { get; }
    /// <summary>Gets structured original content differences.</summary>
    public IReadOnlyList<ContentSaveIssue> Issues { get; }
    /// <summary>Gets selected game and mod conversion steps in execution order.</summary>
    public IReadOnlyList<SaveMigrationStep> MigrationPath { get; }
    /// <summary>Gets the exact source file identity.</summary>
    public string SourceFingerprint => Original.Fingerprint;
    /// <summary>Gets the issuing repository identity.</summary>
    internal Guid Owner { get; }
    /// <summary>Gets the original lossless file.</summary>
    internal StorageSnapshot Original { get; }
    /// <summary>Gets the private prepared bytes, inaccessible to game mutation.</summary>
    internal StorageSnapshot Candidate { get; }
    /// <summary>Gets the active session bound to this decision.</summary>
    internal ContentSnapshot? CurrentContent { get; }
}
