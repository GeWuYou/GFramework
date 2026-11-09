// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GFramework.Core.Abstractions.Pause;
using GFramework.Game.Abstractions.UI;
using Godot;

using DodgeTheCreeps.Advanced.Settings;

namespace DodgeTheCreeps.Advanced;

/// <summary>Checks complete settings, explicit draft decisions and fresh-process persistence in native Godot.</summary>
public partial class GameApplication
{
    private async Task RunSettingsSmokeAsync(bool continuation)
    {
        if (continuation)
        {
            CheckStoredSettings();
            CheckPreferenceAudio(0.4f, 0.3f, 0.2f);
            GD.Print("DODGE_SETTINGS_RESULT PASS continuation");
            FinishSmoke(passed: true);
            return;
        }
        CheckResolutionParity();
        await StartGameAsync(restore: false).ConfigureAwait(true);
        Require(CurrentWorld!.GetNode<AudioStreamPlayer>("Music").Bus.ToString().Equals("BGM", StringComparison.Ordinal) &&
            CurrentWorld.GetNode<AudioStreamPlayer>("GameOverSound").Bus.ToString().Equals("SFX", StringComparison.Ordinal),
            "Gameplay audio is not routed through independent music/effects buses.");
        await SaveGameAsync().ConfigureAwait(true);
        var progressPath = ProjectSettings.GlobalizePath(_dataRoot + "/saves/slot_1/save.json");
        var progress = await File.ReadAllBytesAsync(progressPath, cancellationToken: global::System.Threading.CancellationToken.None).ConfigureAwait(true);
        await SaveBestScoreAsync(83).ConfigureAwait(true);
        Click("Pause");
        await UntilAsync(() => HasView("pause") && GetTree().Paused).ConfigureAwait(true);
        await CheckSettingsDiscardAndCancelAsync().ConfigureAwait(true);
        await CheckSettingsApplyAndResetAsync().ConfigureAwait(true);
        await CheckSettingsFailuresAsync().ConfigureAwait(true);
        await Task.WhenAll(SaveBestScoreAsync(91), SetLanguageAsync("zh-CN")).ConfigureAwait(true);
        CheckStoredSettings();
        Require((await File.ReadAllBytesAsync(progressPath, cancellationToken: global::System.Threading.CancellationToken.None).ConfigureAwait(true)).SequenceEqual(progress), "Settings or reset changed saved gameplay progress.");
        GD.Print("DODGE_SETTINGS_NEXT " + _smokeId);
        GD.Print("DODGE_SETTINGS_RESULT PASS seed");
        FinishSmoke(passed: true);
    }

    private static void CheckResolutionParity()
    {
        var required = new[] { (360, 540), (480, 720), (640, 960) };
        Require(required.All(size => DodgePreferences.ResolutionChoices.Contains(size)), "The settings omit a supported portrait preset.");
        Require(DodgePreferences.ResolutionChoices.Count == required.Length, "Unexpected resolution choices were introduced.");
        Require(DodgePreferences.ResolutionChoices.All(static size => size.Width * 3 == size.Height * 2), "A landscape preset was offered for the portrait game.");
    }

    private async Task CheckSettingsDiscardAndCancelAsync()
    {
        await OpenSettingsAsync().ConfigureAwait(true);
        await UntilAsync(() => HasView("settings") && _pause.GetPauseDepth(PauseGroup.Global) == 2).ConfigureAwait(true);
        var handle = _ui.Snapshot.Views.Single(static view => string.Equals(view.RouteKey, "settings", StringComparison.Ordinal)).Handle;
        ((HSlider)FindNative("Volume", "HSlider")).Value = 0.2;
        ((HSlider)FindNative("BgmVolume", "HSlider")).Value = 0.25;
        ((HSlider)FindNative("SfxVolume", "HSlider")).Value = 0;
        SelectSettingsOption("Language", 1);
        await UntilAsync(() => string.Equals(Preferences.Language, "zh-CN", StringComparison.Ordinal)).ConfigureAwait(true);
        Require(Math.Abs(Preferences.Volume - 0.65f) < 0.001 && Math.Abs(Preferences.BgmVolume - 1) < 0.001,
            "Immediate language persistence accidentally saved an audio draft.");
        CheckPreferenceAudio(0.65f, 1, 1);
        Click("Back");
        await UntilAsync(() => HasView("settings-unsaved")).ConfigureAwait(true);
        Click("KeepEditingSettings");
        await UntilAsync(() => !HasView("settings-unsaved") && _actions.TrueForAll(static task => task.IsCompleted)).ConfigureAwait(true);
        Require(_ui.Snapshot.Views.Single(static view => string.Equals(view.RouteKey, "settings", StringComparison.Ordinal)).Handle == handle,
            "Cancel recreated the settings view instead of retaining its draft.");
        Require(_pause.GetPauseDepth(PauseGroup.Global) == 2, "Cancelling the settings decision changed pause ownership.");
        await _ui.DispatchActionAsync(UiInputAction.Cancel, System.Threading.CancellationToken.None).ConfigureAwait(true);
        await UntilAsync(() => HasView("settings-unsaved")).ConfigureAwait(true);
        Click("DiscardSettingsAndExit");
        await UntilAsync(() => !HasView("settings") && _actions.TrueForAll(static task => task.IsCompleted)).ConfigureAwait(true);
        Require(_pause.GetPauseDepth(PauseGroup.Global) == 1 && GetTree().Paused, "Discard released the lower pause window's lease.");
        CheckPreferenceAudio(0.65f, 1, 1);
    }

    private async Task CheckSettingsApplyAndResetAsync()
    {
        await OpenSettingsAsync().ConfigureAwait(true);
        ((HSlider)FindNative("Volume", "HSlider")).Value = 0.4;
        ((HSlider)FindNative("BgmVolume", "HSlider")).Value = 0.3;
        ((HSlider)FindNative("SfxVolume", "HSlider")).Value = 0;
        SelectSettingsOption("Resolution", 2);
        SelectSettingsOption("Fullscreen", 0);
        Click("SaveSettings");
        await UntilAsync(() => Preferences.Fullscreen && Math.Abs(Preferences.Volume - 0.4f) < 0.001 &&
            _actions.TrueForAll(static task => task.IsCompleted)).ConfigureAwait(true);
        CheckPreferenceAudio(0.4f, 0.3f, 0);
        ((HSlider)FindNative("SfxVolume", "HSlider")).Value = 0.2;
        Click("SaveSettings");
        await UntilAsync(() => Math.Abs(Preferences.SfxVolume - 0.2f) < 0.001 && _actions.TrueForAll(static task => task.IsCompleted)).ConfigureAwait(true);
        CheckPreferenceAudio(0.4f, 0.3f, 0.2f);
        SelectSettingsOption("Fullscreen", 1);
        Click("Back");
        await UntilAsync(() => HasView("settings-unsaved")).ConfigureAwait(true);
        Click("ApplySettingsAndExit");
        await UntilAsync(() => !HasView("settings") && _actions.TrueForAll(static task => task.IsCompleted)).ConfigureAwait(true);
        Require(!Preferences.Fullscreen && Preferences.ResolutionWidth == 640 && Preferences.ResolutionHeight == 960,
            "Apply-and-exit did not persist graphics preferences.");
        await CheckPhysicalWindowAsync().ConfigureAwait(true);
        await OpenSettingsAsync().ConfigureAwait(true);
        Click("ResetSettings");
        await UntilAsync(() => _actions.TrueForAll(static task => task.IsCompleted)).ConfigureAwait(true);
        Require(Math.Abs(((HSlider)FindNative("Volume", "HSlider")).Value - 0.65) < 0.001 &&
            Math.Abs(((HSlider)FindNative("BgmVolume", "HSlider")).Value - 1) < 0.001 &&
            ((OptionButton)FindNative("Resolution", "OptionButton")).Selected == 1,
            "Reset did not populate the default settings draft.");
        Require(Preferences.BestScore == 83 && Math.Abs(Preferences.Volume - 0.4f) < 0.001,
            "Reset erased progress or silently applied the audio draft.");
        Click("Back");
        await UntilAsync(() => HasView("settings-unsaved")).ConfigureAwait(true);
        Click("DiscardSettingsAndExit");
        await UntilAsync(() => !HasView("settings") && _actions.TrueForAll(static task => task.IsCompleted)).ConfigureAwait(true);
        CheckPreferenceAudio(0.4f, 0.3f, 0.2f);
    }

    private async Task CheckPhysicalWindowAsync()
    {
        var window = GetWindow();
        Require(window.ContentScaleSize == new Vector2I(480, 720) && window.ContentScaleAspect == Window.ContentScaleAspectEnum.Keep,
            "Changing output dimensions distorted the logical portrait arena.");
        if (string.Equals(DisplayServer.GetName(), "headless", StringComparison.Ordinal))
        {
            GD.Print("DODGE_SETTINGS_DISPLAY headless: physical window verification requires a rendered run.");
            return;
        }
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        var size = DisplayServer.WindowGetSize();
        var usable = DisplayServer.ScreenGetUsableRect(DisplayServer.WindowGetCurrentScreen()).Size;
        Require(DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Windowed && size.X * 3 == size.Y * 2 &&
            size.X <= 640 && size.Y <= 960 && size.X <= usable.X && size.Y <= usable.Y,
            "Native windowed output did not preserve portrait aspect within the available desktop.");
    }

    private async Task CheckSettingsFailuresAsync()
    {
        var preferencesPath = ProjectSettings.GlobalizePath(_dataRoot + "/preferences.json");
        var original = await File.ReadAllBytesAsync(preferencesPath, cancellationToken: global::System.Threading.CancellationToken.None).ConfigureAwait(true);
        var invalid = Preferences.Copy();
        invalid.Volume = float.NaN;
        var rejected = false;
        try
        {
            await SaveSettingsAsync(invalid).ConfigureAwait(true);
        }
        catch (InvalidOperationException) { rejected = true; }
        Require(rejected && (await File.ReadAllBytesAsync(preferencesPath, cancellationToken: global::System.Threading.CancellationToken.None).ConfigureAwait(true)).SequenceEqual(original), "Invalid settings overwrote the previous preferences.");
        // A separate settings boundary receives a deliberately read-only storage destination.
        // It shares the real native effects, so rollback is observed without mutating a coordinator field.
        using var unwritable = new SettingsService(_host, _storage,
            _launch with
            {
                DataRoot = "res://settings-write-failure",
            }, Text, _menus, _lifetime);
        var candidate = Preferences.Copy();
        candidate.Volume = 0.75f;
        rejected = false;
        try
        {
            await unwritable.SaveSettingsAsync(candidate).ConfigureAwait(true);
        }
        catch (NotSupportedException) { rejected = true; }
        Require(rejected, "The deliberate read-only destination did not reject a settings write.");
        Require((await File.ReadAllBytesAsync(preferencesPath, cancellationToken: global::System.Threading.CancellationToken.None).ConfigureAwait(true)).SequenceEqual(original) && Math.Abs(Preferences.Volume - 0.4f) < 0.001,
            "Failed settings persistence changed the old file or published preferences.");
        CheckPreferenceAudio(0.4f, 0.3f, 0.2f);
    }

    private void CheckStoredSettings()
    {
        Require(Math.Abs(Preferences.Volume - 0.4f) < 0.001 && Math.Abs(Preferences.BgmVolume - 0.3f) < 0.001 &&
            Math.Abs(Preferences.SfxVolume - 0.2f) < 0.001 && !Preferences.Fullscreen &&
            Preferences.ResolutionWidth == 640 && Preferences.ResolutionHeight == 960 && Preferences.BestScore == 91 &&
            string.Equals(Preferences.Language, "zh-CN", StringComparison.Ordinal) && Text.IsChinese,
            "Full settings, language or best score did not survive a serialized update/fresh process.");
    }

    private static void CheckPreferenceAudio(float master, float bgm, float sfx)
    {
        foreach (var (name, expected) in new[] { ("Master", master), ("BGM", bgm), ("SFX", sfx) })
        {
            var index = AudioServer.GetBusIndex(name);
            Require(index >= 0 && Math.Abs(Mathf.DbToLinear(AudioServer.GetBusVolumeDb(index)) - Math.Max(expected, 0.0001f)) < 0.002,
                "Native volume is incorrect for bus " + name);
            Require(AudioServer.IsBusMute(index) == (expected <= 0), "Zero-volume muting is incorrect for bus " + name);
        }
    }

    private void SelectSettingsOption(string name, int index)
    {
        var option = (OptionButton)FindNative(name, "OptionButton");
        option.Select(index);
        option.EmitSignal(OptionButton.SignalName.ItemSelected, index);
    }
}
