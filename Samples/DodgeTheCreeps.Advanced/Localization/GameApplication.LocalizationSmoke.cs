// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GFramework.Core.Abstractions.Pause;
using Godot;

namespace DodgeTheCreeps.Advanced;

/// <summary>Validates packaged translations, live native views and language persistence in isolated smoke data.</summary>
public partial class GameApplication
{
    /// <summary>Exercises actual language controls while preserving active worlds, UI handles and nested pause leases.</summary>
    /// <param name="continuation">Checks a saved locale from the preceding isolated process.</param>
    private async Task RunLocalizationSmokeAsync(bool continuation)
    {
        Require(GD.Load<Font>("res://fonts/NotoSansSC.ttf").HasChar('中'), "Bundled font has no Chinese glyphs.");
        CheckChineseFontWeight();
        if (continuation)
        {
            CheckLocalizationContinuation();
            GD.Print("DODGE_LOCALIZATION_RESULT PASS continue");
            FinishSmoke(passed: true);
            return;
        }
        await CheckLocalizedSettingsAsync().ConfigureAwait(true);
        await CheckLocalizedDialogsAsync().ConfigureAwait(true);
        GD.Print("DODGE_LOCALIZATION_NEXT " + _smokeId);
        GD.Print("DODGE_LOCALIZATION_RESULT PASS seed");
        FinishSmoke(passed: true);
    }

    /// <summary>Checks the actual native cache coordinates rather than just the declared font dictionary.</summary>
    private static void CheckChineseFontWeight()
    {
        using var font = DodgeLocalization.CreateChineseFont();
        var server = TextServerManager.GetPrimaryInterface();
        var tag = server.NameToTag("wght");
        var coordinates = server.FontGetVariationCoordinates(font.GetRids()[0]);
        Require(coordinates.ContainsKey(tag) && Math.Abs(coordinates[tag].AsDouble() - 500) < 0.01,
            "The Chinese variable font did not select the readable medium-weight axis.");
    }

    /// <summary>Checks explicit locale persistence and a non-destructive fallback in a fresh process.</summary>
    private void CheckLocalizationContinuation()
    {
        Require(string.Equals(Preferences.Language, "zh-CN", StringComparison.Ordinal) && Text.IsChinese,
            "An explicit Chinese language choice did not survive process restart.");
        Require(string.Equals(((Button)FindNative("Start", "Button")).Text, Text.Get("new_run"), StringComparison.Ordinal),
            "Restarted menu was not translated.");
        var path = ProjectSettings.GlobalizePath(_dataRoot + "/preferences.json");
        var before = File.ReadAllBytes(path);
        var unsupported = Preferences.Copy();
        unsupported.Language = "unsupported-locale";
        SettingsRules.Validate(unsupported);
        Text.ApplySaved(unsupported.Language);
        Require(string.Equals(Text.CurrentLanguage, "en", StringComparison.Ordinal), "Unsupported explicit locale did not use English.");
        Require(File.ReadAllBytes(path).SequenceEqual(before), "Fallback silently replaced the original preference file.");
        Text.ApplySaved(Preferences.Language);
    }

    /// <summary>Uses the native selector while retaining the same world, focus-owning views and pause leases.</summary>
    private async Task CheckLocalizedSettingsAsync()
    {
        await SetLanguageAsync("en").ConfigureAwait(true);
        Click("Start");
        await UntilAsync(() => CurrentWorld is { IsRunning: true } && HasView("hud")).ConfigureAwait(true);
        var world = CurrentWorld;
        Click("Pause");
        await UntilAsync(() => HasView("pause") && GetTree().Paused).ConfigureAwait(true);
        Click("Settings");
        await UntilAsync(() => HasView("settings") && _pause.GetPauseDepth(PauseGroup.Global) == 2).ConfigureAwait(true);
        var handles = _ui.Snapshot.Views.Select(static view => view.Handle).ToArray();
        var position = world!.PlayerPosition;
        var language = (OptionButton)FindNative("Language", "OptionButton");
        language.Select(1);
        language.EmitSignal(OptionButton.SignalName.ItemSelected, 1L);
        await UntilAsync(() => Text.IsChinese && string.Equals(Preferences.Language, "zh-CN", StringComparison.Ordinal))
            .ConfigureAwait(true);
        Require(_ui.Snapshot.Views.Select(static view => view.Handle).SequenceEqual(handles), "Language switching replaced UI instances.");
        Require(ReferenceEquals(world, CurrentWorld) && world.PlayerPosition == position && _pause.GetPauseDepth(PauseGroup.Global) == 2,
            "Language switching changed the active world or nested pause state.");
        Require(string.Equals(((Button)FindNative("SaveSettings", "Button")).Text, Text.Get("apply_settings"), StringComparison.Ordinal),
            "Settings buttons did not refresh live.");
        Click("Back");
        await UntilAsync(() => !HasView("settings") && _pause.GetPauseDepth(PauseGroup.Global) == 1).ConfigureAwait(true);
        Require(string.Equals(((Button)FindNative("Resume", "Button")).Text, Text.Get("resume"), StringComparison.Ordinal),
            "The underlying pause view was not translated.");
    }

    /// <summary>Updates open dialogs in place and keeps long Chinese recovery decisions visible.</summary>
    private async Task CheckLocalizedDialogsAsync()
    {
        var world = CurrentWorld;
        var confirm = _ui.ShowDialogAsync(DodgeRoutes.Confirm, () => Text.Get("leave_question"), _lifetime.Token);
        await UntilAsync(() => HasView("confirm")).ConfigureAwait(true);
        var handles = _ui.Snapshot.Views.Select(static view => view.Handle).ToArray();
        await SetLanguageAsync("en").ConfigureAwait(true);
        Require(_ui.Snapshot.Views.Select(static view => view.Handle).SequenceEqual(handles), "Language switching recreated an open confirmation.");
        Require(string.Equals(((Button)FindNative("KeepPlaying", "Button")).Text, Text.Get("keep_playing"), StringComparison.Ordinal),
            "Open confirmation was not translated.");
        Click("KeepPlaying");
        await confirm.ConfigureAwait(true);

        var recover = _ui.ShowDialogAsync(DodgeRoutes.Recover, () => Text.Get("recovery_question") + "\n\n" +
            string.Join('\n', Enumerable.Repeat(Text.Get("issue_missing_content", ("id", "moon:crater"), ("table", "arenas")), 20)),
            _lifetime.Token);
        await UntilAsync(() => HasView("recover")).ConfigureAwait(true);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        var scroll = (ScrollContainer)FindNative("RecoveryDetails", "ScrollContainer");
        scroll.ScrollVertical = 120;
        handles = [.. _ui.Snapshot.Views.Select(static view => view.Handle)];
        await SetLanguageAsync("zh-CN").ConfigureAwait(true);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Require(_ui.Snapshot.Views.Select(static view => view.Handle).SequenceEqual(handles) && scroll.ScrollVertical > 0,
            "Translating recovery discarded dialog identity or scroll state.");
        var proceed = (Button)FindNative("RecoveryProceed", "Button");
        var bounds = proceed.GetGlobalRect();
        Require(bounds.Position.Y >= 0 && bounds.End.Y <= GetViewport().GetVisibleRect().Size.Y,
            "Chinese recovery text pushed the decision buttons outside the viewport.");
        Require(string.Equals(proceed.Text, Text.Get("back_up_continue"), StringComparison.Ordinal), "Recovery decision was not translated.");
        Click("RecoveryCancel");
        await recover.ConfigureAwait(true);
        Require(_pause.GetPauseDepth(PauseGroup.Global) == 1 && ReferenceEquals(world, CurrentWorld),
            "Closing translated dialogs released the underlying gameplay pause.");
    }
}
