// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Threading.Tasks;
using DodgeTheCreeps.Advanced.Modding;
using Newtonsoft.Json.Linq;
using Godot;


namespace DodgeTheCreeps.Advanced;

/// <summary>Exercises independently built DLLs, scoped Harmony cleanup and opaque mod state across real processes.</summary>
public partial class GameApplication
{
    /// <summary>Uses only this smoke run's GUID namespace and persists the next stage explicitly.</summary>
    private async Task RunModdingSmokeAsync(bool continuation)
    {
        using var paused = _pause.PauseScope("External DLL adoption acceptance");
        Require(CanStart, "Mod session did not prepare: " + ActiveContent.Error);
        var key = _dataRoot + "/modding-stage.json";
        var stage = continuation ? await _storage.ReadAsync<int>(key).ConfigureAwait(true) : 0;
        if (stage == 0)
        {
            await PrepareCodeSelectionSmokeAsync().ConfigureAwait(true);
        }
        else if (stage == 1)
        {
            Require(ScoreModifierCount == 1 && DodgeScoreRules.Calculate() == 2, "External API or Harmony mod did not start.");
            await StartGameAsync(restore: false).ConfigureAwait(true);
            AdvanceModScore();
            Require(CurrentWorld!.Score == 3 && ReadBonusAwards() == 1, "The two external DLLs did not affect gameplay or mod state.");
            await SaveGameAsync().ConfigureAwait(true);
            await ActiveContent.ToggleAsync("code-bonus", _lifetime.Token).ConfigureAwait(true);
            await ActiveContent.ToggleAsync("code-double", _lifetime.Token).ConfigureAwait(true);
        }
        else if (stage == 2)
        {
            Require(ScoreModifierCount == 0 && DodgeScoreRules.Calculate() == 1, "Disabled mods remained loaded after restart.");
            var prepared = await Saves.PrepareLoadAsync(1, _lifetime.Token).ConfigureAwait(true);
            Require(prepared.RequiresConfirmation && prepared.Document.Mods.ContainsKey("code-bonus"),
                "Missing mod state was erased before explicit content recovery.");
            await ContinueModSaveAsync().ConfigureAwait(true);
            Require(CurrentWorld!.Score == 3, "Missing-code recovery lost the game score.");
            AdvanceModScore();
            await SaveGameAsync().ConfigureAwait(true);
            var saved = await Saves.PrepareLoadAsync(1, _lifetime.Token).ConfigureAwait(true);
            Require((int?)saved.Document.Mods["code-bonus"].Data["score"]?["Awards"] == 1,
                "Saving without the mod erased its opaque state.");
            await ActiveContent.ToggleAsync("code-bonus", _lifetime.Token).ConfigureAwait(true);
            await ActiveContent.ToggleAsync("code-double", _lifetime.Token).ConfigureAwait(true);
        }
        else if (stage == 3)
        {
            await RestoreModStateSmokeAsync().ConfigureAwait(true);
        }
        else
            throw new InvalidOperationException("Unknown modding acceptance stage.");
        await _storage.WriteAsync(key, stage + 1).ConfigureAwait(true);
        GD.Print("DODGE_MODDING_RESULT PASS " + stage.ToString(System.Globalization.CultureInfo.InvariantCulture));
        if (stage < 3)
            GD.Print("DODGE_MODDING_NEXT " + _smokeId);
        FinishSmoke(passed: true);
    }

    /// <summary>Checks restored mod state and independent API/patch cleanup after another process enabled the code again.</summary>
    private async Task RestoreModStateSmokeAsync()
    {
        await ContinueModSaveAsync().ConfigureAwait(true);
        Require(CurrentWorld!.Score == 4 && ReadBonusAwards() == 1, "Reinstalled mods did not recover their previous state.");
        AdvanceModScore();
        Require(CurrentWorld.Score == 7 && ReadBonusAwards() == 2, "Recovered mod state or score behavior did not continue.");
        await SaveGameAsync().ConfigureAwait(true);
        CurrentWorld.StopRun();
        await ActiveContent.StopModsAsync().ConfigureAwait(true);
        Require(ScoreModifierCount == 0 && DodgeScoreRules.Calculate() == 1, "Mod shutdown left an API registration or patch behind.");
    }

    /// <summary>Exercises decline and exact-code approval without executing newly selected mods in this process.</summary>
    private async Task PrepareCodeSelectionSmokeAsync()
    {
        CheckRunStateAdmission();
        Require(ActiveContent.Packages.Where(pack => pack.Manifest.Code is not null).Take(3).Count() == 2,
            "Build the two external mod examples before native acceptance.");
        await OpenContentAsync().ConfigureAwait(true);
        // First reject one execution request through the same dialog as normal installation.
        Click("Toggle_code-bonus");
        await UntilAsync(() => HasView("code-permission")).ConfigureAwait(true);
        Click("DenyCode");
        await UntilAsync(() => _actions.TrueForAll(static task => task.IsCompleted)).ConfigureAwait(true);
        Require(!ActiveContent.RequestedProfile.EnabledPackages.Contains("code-bonus", StringComparer.Ordinal), "Denied code was enabled.");
        foreach (var id in new[] { "code-bonus", "code-double" })
        {
            Click("Toggle_" + id);
            await UntilAsync(() => HasView("code-permission")).ConfigureAwait(true);
            Click("AllowCode");
            await UntilAsync(() => _actions.TrueForAll(static task => task.IsCompleted)).ConfigureAwait(true);
        }
        Require(ScoreModifierCount == 0 && DodgeScoreRules.Calculate() == 1,
            "Recording next-start code choices changed this process.");
    }

    /// <summary>Verifies that initialization-time data access produces a contract error rather than an uninitialized-field failure.</summary>
    private void CheckRunStateAdmission()
    {
        try
        {
            _ = _mods.CreateApi("code-bonus").ReadState<JObject>("score");
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
        var action = StartGameAsync(restore: true);
        await UntilAsync(() => HasView("recover") || action.IsCompleted).ConfigureAwait(true);
        if (HasView("recover"))
            Click("RecoveryProceed");
        await action.ConfigureAwait(true);
    }

    /// <summary>Inspects the sample mod's plain JSON through its active owner scope.</summary>
    private int ReadBonusAwards() => (int?)ModState.ForMod("code-bonus").Read<JObject>()?["score"]?["Awards"] ?? 0;

    /// <summary>Crosses one score boundary on a deterministic protected board rather than racing random collisions.</summary>
    private void AdvanceModScore()
    {
        var board = CurrentWorld!.CaptureProgress();
        board.GraceRemaining = 1.5;
        board.SpawnRemaining = 10;
        board.ScoreElapsed = 0;
        CurrentWorld.RestoreProgress(board);
        CurrentWorld._PhysicsProcess(1);
    }
}
