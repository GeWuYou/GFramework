// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Threading.Tasks;
using DodgeTheCreeps.Advanced.Content;
using GFramework.Game.Abstractions.Content;
using Godot;
using Newtonsoft.Json.Linq;

namespace DodgeTheCreeps.Advanced;

/// <summary>Contains isolated native regression scenarios for content repair, live feedback and discard navigation.</summary>
public partial class GameApplication
{
    /// <summary>Exercises recoverable content removal and post-save movement through production views and actions.</summary>
    private async Task RunHistoricalReviewSmokeAsync()
    {
        await CheckRequiredContentRepairAsync("arenas", "base:yard").ConfigureAwait(true);
        await CheckRequiredContentRepairAsync("enemies", "base:walker").ConfigureAwait(true);
        await StartGameAsync(restore: false).ConfigureAwait(true);
        Click("Pause");
        await UntilAsync(() => HasView("pause")).ConfigureAwait(true);
        await CheckNestedSettingsAsync().ConfigureAwait(true);
        await SaveGameAsync().ConfigureAwait(true);
        Require(string.Equals(((Label)FindNative("Status", "Label")).Text, Text.Get("run_saved"), StringComparison.Ordinal),
            "Closing settings left feedback targeting the freed settings label");
        await CheckMovementOnlyDiscardAsync().ConfigureAwait(true);
        GD.Print("DODGE_REVIEW_RESULT PASS content-required-rows,menu-feedback,unsaved-position");
        FinishSmoke(passed: true);
    }

    private static readonly string[] RequiredRemovalPackages = ["review-removal"];

    /// <summary>Removes an otherwise legal required row and verifies that Reset remains reachable in the actual content manager.</summary>
    /// <param name="table">The table containing the required row.</param>
    /// <param name="row">The required stable row identity.</param>
    private async Task CheckRequiredContentRepairAsync(string table, string row)
    {
        var dataRoot = _dataRoot + "/review-" + table;
        await WriteRequiredRemovalFixtureAsync(dataRoot, table, row).ConfigureAwait(true);
        var original = _content;
        using var candidate = new DodgeContent(_storage, dataRoot);
        await candidate.InitializeAsync(_lifetime.Token).ConfigureAwait(true);
        Require(!candidate.IsReady && candidate.Error is not null &&
            candidate.Error.StartsWith("Required sample content is missing:", StringComparison.Ordinal) &&
            candidate.Error.Contains(row, StringComparison.Ordinal),
            "A catalog missing required sample content was published as ready: " + row);
        try
        {
            _content = candidate;
            await OpenContentAsync().ConfigureAwait(true);
            Require(FindNative("ResetContent", "Button") is Button { Disabled: false },
                "Required-row removal made the content repair action inaccessible");
            await ClickContentActionAsync("ResetContent").ConfigureAwait(true);
            Require(candidate.RequestedProfile.EnabledPackages.Count == 0, "Repair did not clear the removing package selection");
        }
        finally
        {
            await _ui.ClearAsync(_lifetime.Token).ConfigureAwait(true);
            _content = original;
            await candidate.StopModsAsync().ConfigureAwait(true);
            await _ui.ReplaceAsync(DodgeRoutes.Menu, args: false, _lifetime.Token).ConfigureAwait(true);
        }
    }

    /// <summary>Creates a test-owned legal data package that removes one row, leaving ordinary game installations untouched.</summary>
    /// <param name="dataRoot">The isolated smoke data root.</param>
    /// <param name="table">The table to modify.</param>
    /// <param name="row">The row to remove.</param>
    private async Task WriteRequiredRemovalFixtureAsync(string dataRoot, string table, string row)
    {
        var directory = Path.Combine(ProjectSettings.GlobalizePath(dataRoot + "/content/installed"), "review-removal");
        Directory.CreateDirectory(directory);
        var manifest = JObject.Parse(Godot.FileAccess.GetFileAsString("res://content_sources/slow/content-pack.json"));
        manifest["id"] = "review-removal";
        manifest["name"] = "Required row removal probe";
        await File.WriteAllTextAsync(Path.Combine(directory, "content-pack.json"), manifest.ToString(Newtonsoft.Json.Formatting.None), cancellationToken: global::System.Threading.CancellationToken.None).ConfigureAwait(true);
        var patch = $"- op: remove\n  table: {table}\n  id: {row}\n";
        if (string.Equals(table, "enemies", StringComparison.Ordinal))
        {
            // Keep catalog references legal: this fixture isolates the sample's unconditional walker requirement.
            patch += "- op: patch\n  table: arenas\n  id: base:yard\n  value:\n    enemies: [base:swimmer, base:flyer]\n";
        }
        await File.WriteAllTextAsync(Path.Combine(directory, "patch.yaml"), patch, cancellationToken: global::System.Threading.CancellationToken.None).ConfigureAwait(true);
        await _storage.WriteAsync(dataRoot + "/content.json", new DodgeContentPreferences
        {
            Profile = new ContentPackProfile { EnabledPackages = RequiredRemovalPackages, LoadOrder = [] },
        }).ConfigureAwait(true);
    }

    /// <summary>Moves the native player after a save without advancing score, then observes the real discard dialog.</summary>
    private async Task CheckMovementOnlyDiscardAsync()
    {
        var score = _world!.Score;
        var player = _world.GetNode<Node2D>("Player");
        player.Position += Vector2.Right;
        Require(!_dirty && _world.Score == score, "Movement-only probe unexpectedly changed the score dirty flag");
        var leaving = ReturnToMenuAsync();
        await UntilAsync(() => HasView("confirm")).ConfigureAwait(true);
        Click("KeepPlaying");
        await leaving.ConfigureAwait(true);
        Require(_world is { IsRunning: true } && HasView("pause"), "Discard cancellation did not preserve the live run");
    }
}
