// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Content;

namespace GFramework.Game.Data;

/// <summary>
/// A read-only inspection tied to a repository, exact file bytes and target content configuration.
/// Passing it to recovery means the game has obtained player confirmation for these differences.
/// </summary>
public sealed class ContentSaveReport
{
    /// <summary>Creates a report issued by one repository instance.</summary>
    internal ContentSaveReport(Guid owner, int slot, string sourceFingerprint, ContentSnapshot? savedContent,
        ContentSnapshot currentContent, IReadOnlyList<ContentSaveIssue> issues)
    {
        Owner = owner;
        Slot = slot;
        SourceFingerprint = sourceFingerprint;
        SavedContent = savedContent;
        CurrentContent = currentContent;
        Issues = Array.AsReadOnly(issues.ToArray());
    }

    /// <summary>Gets the repository identity that issued the approval token.</summary>
    internal Guid Owner { get; }
    /// <summary>Gets the inspected slot.</summary>
    public int Slot { get; }
    /// <summary>Gets the exact original serialized-file hash used to reject stale confirmation.</summary>
    public string SourceFingerprint { get; }
    /// <summary>Gets the save's previous content metadata.</summary>
    public ContentSnapshot? SavedContent { get; }
    /// <summary>Gets the content session against which the player approved recovery.</summary>
    public ContentSnapshot CurrentContent { get; }
    /// <summary>Gets all blocking and advisory differences in deterministic order.</summary>
    public IReadOnlyList<ContentSaveIssue> Issues { get; }
    /// <summary>Gets whether the file requires explicit legacy import.</summary>
    public bool IsLegacy => SavedContent is null;
    /// <summary>Gets whether ordinary loading is blocked.</summary>
    public bool RequiresRecovery => Issues.Any(static issue => issue.Blocking);
    /// <summary>Gets whether any blocking or advisory differences were found.</summary>
    public bool HasChanges => Issues.Count != 0;
}
