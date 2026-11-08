// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Content;

namespace GFramework.Game.Data;

/// <summary>
/// Read-only source content differences supplied to pure document recovery during preparation.
/// This report does not authorize writes; confirmation applies to the complete immutable prepared candidate.
/// </summary>
public sealed class ContentSaveReport
{
    /// <summary>Creates a report issued by one repository instance.</summary>
    internal ContentSaveReport(int slot, string sourceFingerprint, ContentSnapshot? savedContent,
        ContentSnapshot currentContent, IReadOnlyList<ContentSaveIssue> issues)
    {
        Slot = slot;
        SourceFingerprint = sourceFingerprint;
        SavedContent = savedContent;
        CurrentContent = currentContent;
        Issues = Array.AsReadOnly(issues.ToArray());
    }

    /// <summary>Gets the inspected slot.</summary>
    public int Slot
    {
        get;
    }
    /// <summary>Gets the exact original serialized-file hash for source diagnostics.</summary>
    public string SourceFingerprint
    {
        get;
    }
    /// <summary>Gets the save's previous content metadata.</summary>
    public ContentSnapshot? SavedContent
    {
        get;
    }
    /// <summary>Gets the active content session used during preparation.</summary>
    public ContentSnapshot CurrentContent
    {
        get;
    }
    /// <summary>Gets all blocking and advisory differences in deterministic order.</summary>
    public IReadOnlyList<ContentSaveIssue> Issues
    {
        get;
    }
    /// <summary>Gets whether ordinary loading is blocked.</summary>
    public bool RequiresRecovery => Issues.Any(static issue => issue.Blocking);
    /// <summary>Gets whether any blocking or advisory differences were found.</summary>
    public bool HasChanges => Issues.Count != 0;
}
