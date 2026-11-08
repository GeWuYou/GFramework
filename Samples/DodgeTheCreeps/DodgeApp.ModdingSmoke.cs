// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Threading.Tasks;
using DodgeTheCreeps.Modding;
using Newtonsoft.Json.Linq;
using Godot;

namespace DodgeTheCreeps;

/// <summary>Exercises independently built DLLs, scoped Harmony cleanup and opaque mod state across real processes.</summary>
public partial class DodgeApp
{
    /// <summary>Uses only this smoke run's GUID namespace and persists the next stage explicitly.</summary>
    private async Task RunModdingSmokeAsync(bool continuation)
    {
        using var paused = _pause.PauseScope("External DLL adoption acceptance");
        Require(CanStart, "Mod session did not prepare: " + _content.Error);
        var key = _dataRoot + "/modding-stage.json";
        var stage = continuation ? await _storage.ReadAsync<int>(key).ConfigureAwait(true) : 0;
        if (stage == 0)
        {
            await PrepareCodeSelectionSmokeAsync().ConfigureAwait(true);
        }
        else if (stage == 1)
        {
            Require(_scoreModifiers.Count == 1 && DodgeScoreRules.Calculate() == 2, "External API or Harmony mod did not start.");
            await StartGameAsync(false).ConfigureAwait(true);
            AdvanceModScore();
            Require(_world!.Score == 3 && ReadBonusAwards() == 1, "The two external DLLs did not affect gameplay or mod state.");
            await SaveGameAsync().ConfigureAwait(true);
            await _content.ToggleAsync("code-bonus", _lifetime.Token).ConfigureAwait(true);
            await _content.ToggleAsync("code-double", _lifetime.Token).ConfigureAwait(true);
        }
        else if (stage == 2)
        {
            Require(_scoreModifiers.Count == 0 && DodgeScoreRules.Calculate() == 1, "Disabled mods remained loaded after restart.");
            var prepared = await _saves.PrepareLoadAsync(1, _lifetime.Token).ConfigureAwait(true);
            Require(prepared.RequiresConfirmation && prepared.Document.Mods.ContainsKey("code-bonus"),
                "Missing mod state was erased before explicit content recovery.");
            await ContinueModSaveAsync().ConfigureAwait(true);
            Require(_world!.Score == 3, "Missing-code recovery lost the game score.");
            AdvanceModScore();
            await SaveGameAsync().ConfigureAwait(true);
            var saved = await _saves.PrepareLoadAsync(1, _lifetime.Token).ConfigureAwait(true);
            Require((int?)saved.Document.Mods["code-bonus"].Data["score"]?["Awards"] == 1,
                "Saving without the mod erased its opaque state.");
            await _content.ToggleAsync("code-bonus", _lifetime.Token).ConfigureAwait(true);
            await _content.ToggleAsync("code-double", _lifetime.Token).ConfigureAwait(true);
        }
        else if (stage == 3)
        {
            await RestoreModStateSmokeAsync().ConfigureAwait(true);
        }
        else throw new InvalidOperationException("Unknown modding acceptance stage.");
        await _storage.WriteAsync(key, stage + 1).ConfigureAwait(true);
        GD.Print("DODGE_MODDING_RESULT PASS " + stage);
        if (stage < 3) GD.Print("DODGE_MODDING_NEXT " + _smokeId);
        FinishSmoke(true);
    }

    /// <summary>Checks restored mod state and independent API/patch cleanup after another process enabled the code again.</summary>
    private async Task RestoreModStateSmokeAsync()
    {
        await ContinueModSaveAsync().ConfigureAwait(true);
        Require(_world!.Score == 4 && ReadBonusAwards() == 1, "Reinstalled mods did not recover their previous state.");
        AdvanceModScore();
        Require(_world.Score == 7 && ReadBonusAwards() == 2, "Recovered mod state or score behavior did not continue.");
        await SaveGameAsync().ConfigureAwait(true);
        _world.StopRun();
        await _content.StopModsAsync().ConfigureAwait(true);
        Require(_scoreModifiers.Count == 0 && DodgeScoreRules.Calculate() == 1, "Mod shutdown left an API registration or patch behind.");
    }

    /// <summary>Exercises decline and exact-code approval without executing newly selected mods in this process.</summary>
    private async Task PrepareCodeSelectionSmokeAsync()
    {
        CheckRunStateAdmission();
        Require(_content.Packages.Count(pack => pack.Manifest.Code is not null) == 2,
            "Build the two external mod examples before native acceptance.");
        await OpenContentAsync().ConfigureAwait(true);
        // First reject one execution request through the same dialog as normal installation.
        Click("Toggle_code-bonus");
        await UntilAsync(() => HasView("code-permission")).ConfigureAwait(true);
        Click("DenyCode");
        await UntilAsync(() => _actions.All(static task => task.IsCompleted)).ConfigureAwait(true);
        Require(!_content.RequestedProfile.EnabledPackages.Contains("code-bonus", StringComparer.Ordinal), "Denied code was enabled.");
        foreach (var id in new[] { "code-bonus", "code-double" })
        {
            Click("Toggle_" + id);
            await UntilAsync(() => HasView("code-permission")).ConfigureAwait(true);
            Click("AllowCode");
            await UntilAsync(() => _actions.All(static task => task.IsCompleted)).ConfigureAwait(true);
        }
        Require(_scoreModifiers.Count == 0 && DodgeScoreRules.Calculate() == 1,
            "Recording next-start code choices changed this process.");
    }

    /// <summary>Verifies that initialization-time data access produces a contract error rather than an uninitialized-field failure.</summary>
    private void CheckRunStateAdmission()
    {
        try
        {
            _ = new DodgeModServices(this, "code-bonus").ReadState<JObject>("score");
            throw new InvalidOperationException("Run state was exposed before a run started.");
        }
        catch (InvalidOperationException exception)
        {
            Require(exception.Message.Contains("Start or load a run", StringComparison.Ordinal),
                "Initialization-time state access did not report the run lifecycle contract.");
        }
    }

    /// <summary>Accepts any required prepared-save commit through the native confirmation dialog.</summary>
    private async Task ContinueModSaveAsync()
    {
        var action = StartGameAsync(true);
        await UntilAsync(() => HasView("recover") || action.IsCompleted).ConfigureAwait(true);
        if (HasView("recover")) Click("RecoveryProceed");
        await action.ConfigureAwait(true);
    }

    /// <summary>Inspects the sample mod's plain JSON through its active owner scope.</summary>
    private int ReadBonusAwards() => (int?)_modState.ForMod("code-bonus").Read<JObject>()?["score"]?["Awards"] ?? 0;

    /// <summary>Crosses one score boundary on a deterministic protected board rather than racing random collisions.</summary>
    private void AdvanceModScore()
    {
        var board = _world!.CaptureProgress();
        board.GraceRemaining = 1.5;
        board.SpawnRemaining = 10;
        board.ScoreElapsed = 0;
        _world.RestoreProgress(board);
        _world._PhysicsProcess(1);
    }
}
