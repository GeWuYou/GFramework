// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Content;
using GFramework.Game.Content;

namespace GFramework.Game.Data;

/// <summary>Envelope-level content checks, independent of current game DTO construction.</summary>
public sealed partial class SaveRepository<TSaveData>
{
    /// <summary>Collects original package differences and current-schema references without activation or writes.</summary>
    private ContentSaveReport BuildContentReport(int slot, string fingerprint, SaveDocument document, ContentCatalog catalog)
    {
        var issues = new List<ContentSaveIssue>();
        if (document.Content is null)
            issues.Add(new ContentSaveIssue(ContentSaveIssueKind.MissingMetadata, "The save has no content metadata.", true));
        else
            CollectPackageIssues(document.Content, catalog, issues);
        foreach (var reference in EnumerateReferences(document))
            if (!catalog.Contains(reference))
                issues.Add(new ContentSaveIssue(ContentSaveIssueKind.MissingContent,
                    $"Content '{reference.Id}' in '{reference.TableName}' is unavailable.", true, reference.Id.PackId, reference));
        return new ContentSaveReport(slot, fingerprint, document.Content, catalog.Snapshot, issues);
    }

    /// <summary>Compares exact package stamps with explicit host/declaration compatibility and preserves order diagnostics.</summary>
    private void CollectPackageIssues(ContentSnapshot saved, ContentCatalog catalog, List<ContentSaveIssue> issues)
    {
        var previous = saved.Packs.ToDictionary(static pack => pack.Id, StringComparer.Ordinal);
        var active = catalog.Snapshot.Packs.ToDictionary(static pack => pack.Id, StringComparer.Ordinal);
        foreach (var oldPack in saved.Packs)
        {
            if (!active.TryGetValue(oldPack.Id, out var current))
                issues.Add(new ContentSaveIssue(ContentSaveIssueKind.MissingPack,
                    $"Package '{oldPack.Id}' ({oldPack.Version}) is not enabled.", true, oldPack.Id));
            else if (!IsPackCompatible(oldPack, current, catalog))
                issues.Add(new ContentSaveIssue(ContentSaveIssueKind.IncompatiblePack,
                    $"Package '{oldPack.Id}' changed from {oldPack.Version} to {current.Version} without declared compatibility.", true, oldPack.Id));
        }
        foreach (var added in catalog.Snapshot.Packs.Where(pack => !previous.ContainsKey(pack.Id)))
            issues.Add(new ContentSaveIssue(ContentSaveIssueKind.AdditionalPack,
                $"Package '{added.Id}' is newly enabled.", false, added.Id));
        var oldOrder = saved.Packs.Where(pack => active.ContainsKey(pack.Id)).Select(static pack => pack.Id);
        var newOrder = catalog.Snapshot.Packs.Where(pack => previous.ContainsKey(pack.Id)).Select(static pack => pack.Id);
        if (!oldOrder.SequenceEqual(newOrder, StringComparer.Ordinal))
            issues.Add(new ContentSaveIssue(ContentSaveIssueKind.LoadOrderChanged,
                "The effective order of previously enabled packages changed.", false));
    }

    /// <summary>Accepts exact identity or an explicit compatibility contract; same-version edits never use version ranges.</summary>
    private bool IsPackCompatible(ContentPackStamp previous, ContentPackStamp current, ContentCatalog catalog)
    {
        if (PackStampsEqual(previous, current)) return true;
        if (_options.Content!.IsPackCompatible is { } policy) return policy(previous, current);
        if (string.Equals(previous.Version, current.Version, StringComparison.Ordinal)) return false;
        var installed = catalog.Packages.Single(pack => string.Equals(pack.Manifest.Id, current.Id, StringComparison.Ordinal));
        return installed.Manifest.SaveCompatibleVersions is { } range && ContentVersionRules.Contains(previous.Version, range);
    }

    /// <summary>Rejects a broken host enumerator instead of silently omitting reference validation.</summary>
    private IEnumerable<ContentReference> EnumerateReferences(SaveDocument document) =>
        (_options.Content!.EnumerateReferences(document.Clone()) ?? throw new InvalidOperationException("Save references cannot be null.")).ToArray();

    /// <summary>Compares full immutable startup package order and identity.</summary>
    private static bool SnapshotsEqual(ContentSnapshot? left, ContentSnapshot? right) =>
        left is null ? right is null : right is not null && left.Packs.Count == right.Packs.Count &&
        left.Packs.Zip(right.Packs).All(static pair => PackStampsEqual(pair.First, pair.Second));

    /// <summary>Uses ordinal identity for stable comparison across cultures.</summary>
    private static bool PackStampsEqual(ContentPackStamp left, ContentPackStamp right) =>
        string.Equals(left.Id, right.Id, StringComparison.Ordinal) &&
        string.Equals(left.Version, right.Version, StringComparison.Ordinal) &&
        string.Equals(left.Fingerprint, right.Fingerprint, StringComparison.Ordinal);
}
