// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DodgeTheCreeps.Content;
using GFramework.Game.Abstractions.Content;
using Godot;

namespace DodgeTheCreeps;

/// <summary>Checks content selection, native PCK/data assets and save recovery across real processes.</summary>
public partial class DodgeApp
{
    private async Task RunContentSmokeAsync()
    {
        switch (_contentSmokePhase)
        {
            case "seed":
                await SeedContentSmokeAsync().ConfigureAwait(true);
                break;
            case "active":
                await ActiveContentSmokeAsync().ConfigureAwait(true);
                break;
            case "recover":
                await RecoverContentSmokeAsync().ConfigureAwait(true);
                break;
            case "broken":
                await BrokenContentSmokeAsync().ConfigureAwait(true);
                break;
            case "profile":
                await BrokenProfileSmokeAsync().ConfigureAwait(true);
                break;
            case "done":
                Require(CanStart && _content.Catalog.Snapshot.Packs.Count == 1, "Base-only reset did not survive restart.");
                await StartGameAsync(false).ConfigureAwait(true);
                Require(_world is { IsRunning: true }, "Gameplay did not recover after fixing the startup selection.");
                break;
            default:
                throw new InvalidOperationException("Unknown content smoke phase.");
        }
        GD.Print("DODGE_CONTENT_RESULT PASS " + _contentSmokePhase);
        FinishSmoke(true);
    }

    private async Task SeedContentSmokeAsync()
    {
        Require(CanStart && _content.Catalog.Snapshot.Packs.Count == 1, "Optional packs were enabled without a player choice.");
        await OpenContentAsync().ConfigureAwait(true);
        foreach (var id in new[] { "moon", "coral", "slow", "fast" })
            await ClickContentActionAsync("Toggle_" + id).ConfigureAwait(true);
        Require(_content.PendingRestart, "Activation did not persist a next-start selection.");
        Require(WalkerSpeed() == 125, "Activation mutated the active session before restart.");
        GD.Print("DODGE_CONTENT_NEXT " + _smokeId + ":active");
    }

    private async Task ActiveContentSmokeAsync()
    {
        Require(CanStart && _content.Catalog.Snapshot.Packs.Count == 5, "Enabled content did not survive restart.");
        Require(WalkerSpeed() == 70 && _content.DescribeWalkerSpeed().Contains("slow", StringComparison.Ordinal),
            "The later field patch or its provenance was not applied.");
        // Active media was decoded during startup; playing the data arena exercises its native consumers.
        await OpenContentAsync().ConfigureAwait(true);
        await ClickContentActionAsync("ContentArena").ConfigureAwait(true);
        Require(string.Equals(_content.ArenaId, "coral:reef", StringComparison.Ordinal), "Data-mod arena was not discovered.");
        Click("ContentBack");
        await UntilAsync(() => !HasView("content")).ConfigureAwait(true);
        await StartGameAsync(false).ConfigureAwait(true);
        Require(_world is { IsRunning: true }, "Data-mod arena did not start.");
        await OpenContentAsync().ConfigureAwait(true);
        await ClickContentActionAsync("ContentArena").ConfigureAwait(true);
        Require(string.Equals(_content.ArenaId, "moon:crater", StringComparison.Ordinal), "Official DLC arena was not available.");
        Click("ContentBack");
        await UntilAsync(() => !HasView("content")).ConfigureAwait(true);
        await StartGameAsync(false).ConfigureAwait(true);
        Require(_world!.GetNodeOrNull<Node>("MoonArena") is not null, "Trusted PCK scene was not instantiated.");
        _world.RestoreRun(37, new Vector2(140, 500));
        await SaveGameAsync().ConfigureAwait(true);
        await ReturnToMenuAsync().ConfigureAwait(true);
        await OpenContentAsync().ConfigureAwait(true);
        await ClickContentActionAsync("Down_fast").ConfigureAwait(true);
        await ClickContentActionAsync("Down_fast").ConfigureAwait(true);
        await ClickContentActionAsync("Toggle_moon").ConfigureAwait(true);
        Require(WalkerSpeed() == 70, "Priority changes mutated the running session.");
        GD.Print("DODGE_CONTENT_NEXT " + _smokeId + ":recover");
    }

    private async Task RecoverContentSmokeAsync()
    {
        Require(CanStart && WalkerSpeed() == 220, "Player priority did not change the winning patch after restart.");
        Require(!_content.Catalog.Contains(new ContentReference("arenas", new ContentId("moon:crater"))),
            "Disabled DLC remained in the active catalog.");
        var savePath = ProjectSettings.GlobalizePath(_dataRoot + "/saves/slot_1/save.json");
        var original = File.ReadAllBytes(savePath);
        Click("Continue");
        await UntilAsync(() => HasView("recover")).ConfigureAwait(true);
        Click("RecoveryCancel");
        await UntilAsync(() => !HasView("recover") && _actions.All(static task => task.IsCompleted)).ConfigureAwait(true);
        Require(File.ReadAllBytes(savePath).SequenceEqual(original), "Cancelled repair changed the original file.");
        Click("Continue");
        await UntilAsync(() => HasView("recover")).ConfigureAwait(true);
        Click("RecoveryProceed");
        await UntilAsync(() => _world is { IsRunning: true } && HasView("hud") && _latestBackup is not null).ConfigureAwait(true);
        Require(string.Equals(_arenaId, "base:yard", StringComparison.Ordinal) && _world!.Score >= 37,
            "Game-owned downgrade did not preserve progress while replacing the missing arena.");
        var backup = Path.Combine(Path.GetDirectoryName(savePath)!, _latestBackup!);
        Require(File.ReadAllBytes(backup).SequenceEqual(original), "Recovery backup did not preserve original bytes.");
        _dirty = false;
        await ReturnToMenuAsync().ConfigureAwait(true);
        await OpenContentAsync().ConfigureAwait(true);
        Click("RestoreContentBackup");
        await UntilAsync(() => HasView("recover")).ConfigureAwait(true);
        Click("RecoveryProceed");
        await UntilAsync(() => !HasView("recover") && _actions.All(static task => task.IsCompleted)).ConfigureAwait(true);
        Require(File.ReadAllBytes(savePath).SequenceEqual(original), "Restoring the backup did not restore exact original bytes.");
        await ClickContentActionAsync("Toggle_moon").ConfigureAwait(true);
        // Test-owned isolated installation only: a modified file must fail host trust on the next start.
        var nativePack = Path.Combine(_content.InstalledDirectory, "moon", "moon.pck");
        await File.AppendAllTextAsync(nativePack, "untrusted modification").ConfigureAwait(true);
        GD.Print("DODGE_CONTENT_NEXT " + _smokeId + ":broken");
    }

    private async Task BrokenContentSmokeAsync()
    {
        Require(!CanStart && _content.Error is not null && _content.Error.Contains("not trusted", StringComparison.Ordinal),
            "Changed PCK passed the host trust boundary.");
        Require(((Button)FindNative("Start", "Button")).Disabled, "Invalid content still allowed starting gameplay.");
        Click("Content");
        await UntilAsync(() => HasView("content")).ConfigureAwait(true);
        await ClickContentActionAsync("ResetContent").ConfigureAwait(true);
        Require(_content.RequestedProfile.EnabledPackages.Count == 0, "Broken startup did not retain a usable repair manager.");
        await File.WriteAllTextAsync(ProjectSettings.GlobalizePath(_dataRoot + "/content.json"), "{\"Profile\":null}").ConfigureAwait(true);
        GD.Print("DODGE_CONTENT_NEXT " + _smokeId + ":profile");
    }

    private async Task BrokenProfileSmokeAsync()
    {
        Require(!CanStart && _content.Error is not null && _content.Error.Contains("settings could not be read", StringComparison.Ordinal),
            "Null profile bypassed the recoverable settings validation.");
        var settings = ProjectSettings.GlobalizePath(_dataRoot + "/content.json");
        Require(string.Equals(File.ReadAllText(settings), "{\"Profile\":null}", StringComparison.Ordinal), "Startup silently overwrote unreadable content preferences.");
        foreach (var malformed in new[]
        {
            "{\"Profile\":{\"EnabledPackages\":null,\"LoadOrder\":[]}}",
            "{\"Profile\":{\"EnabledPackages\":[],\"LoadOrder\":null}}",
            "{\"Profile\":{\"EnabledPackages\":[],\"LoadOrder\":[]},\"ArenaId\":\"../arena\"}"
        })
        {
            await File.WriteAllTextAsync(settings, malformed).ConfigureAwait(true);
            using var candidate = new DodgeContent(_storage, _dataRoot);
            await candidate.InitializeAsync(_lifetime.Token).ConfigureAwait(true);
            Require(!candidate.IsReady && candidate.Error is not null, "Malformed profile escaped startup validation.");
            Require(string.Equals(File.ReadAllText(settings), malformed, StringComparison.Ordinal), "Validation replaced the malformed profile without a player action.");
        }
        Click("Content");
        await UntilAsync(() => HasView("content")).ConfigureAwait(true);
        await ClickContentActionAsync("ResetContent").ConfigureAwait(true);
        GD.Print("DODGE_CONTENT_NEXT " + _smokeId + ":done");
    }

    private float WalkerSpeed() => _content.Catalog.GetTable<DodgeEnemyDefinition>("enemies").Get(new ContentId("base:walker")).SpeedMin;

    private async Task ClickContentActionAsync(string name)
    {
        Click(name);
        await UntilAsync(() => HasView("content") && _actions.All(static task => task.IsCompleted)).ConfigureAwait(true);
    }
}
