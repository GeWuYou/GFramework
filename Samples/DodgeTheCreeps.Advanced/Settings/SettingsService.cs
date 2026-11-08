// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;

namespace DodgeTheCreeps.Advanced;

/// <summary>
/// Owns serialized preference updates and the native audio/window application boundary.
/// Views edit copies; accepted updates validate first, apply natively, and persist atomically.
/// A failed native application or write restores the prior native state and published preferences.
/// </summary>
internal sealed class SettingsService : IDisposable
{
    private readonly AdvancedGameRoot _host;
    private readonly GFramework.Core.Abstractions.Storage.IStorage _storage;
    private readonly string _dataRoot;
    private readonly DodgeLocalization _text;
    private readonly GamePresentationState _presentation;
    private readonly CancellationToken _cancellationToken;

    /// <summary>Creates the native settings boundary with its storage and feedback dependencies.</summary>
    internal SettingsService(AdvancedGameRoot host, GFramework.Core.Abstractions.Storage.IStorage storage,
        GameLaunchOptions launch, DodgeLocalization text, GamePresentationState presentation, GameLifetime lifetime)
    {
        _host = host;
        _storage = storage;
        _dataRoot = launch.DataRoot;
        _text = text;
        _presentation = presentation;
        _cancellationToken = lifetime.Token;
    }

    /// <summary>The last fully applied and persisted snapshot; drafts are separate copies.</summary>
    internal DodgePreferences Preferences { get; private set; } = new();

    /// <summary>Loads settings before publishing them to the native display and audio services.</summary>
    internal async Task LoadAsync()
    {
        if (await _storage.ExistsAsync(_dataRoot + "/preferences.json").ConfigureAwait(true))
            Preferences = await _storage.ReadAsync<DodgePreferences>(_dataRoot + "/preferences.json").ConfigureAwait(true);
        ApplyPreferences();
    }

    /// <summary>Releases update serialization after the application has awaited all actions.</summary>
    public void Dispose() => _preferencesGate.Dispose();

    private Window GetWindow() => _host.GetWindow();

    private readonly SemaphoreSlim _preferencesGate = new(1, 1);
    private static readonly string[] PreferenceBuses = ["Master", "BGM", "SFX"];

    // #region tutorial-save-settings
    /// <summary>Saves an audio/display draft while retaining the current score and immediately saved language.</summary>
    internal Task SaveSettingsAsync(DodgePreferences draft)
    {
        ArgumentNullException.ThrowIfNull(draft);
        var captured = draft.Copy();
        return UpdatePreferencesAsync(current =>
        {
            captured.BestScore = current.BestScore;
            captured.Language = current.Language;
            return captured;
        });
    }
    // #endregion tutorial-save-settings

    /// <summary>Preserves the earlier master-volume/fullscreen entry point without dropping other preferences.</summary>
    internal Task SavePreferencesAsync(float volume, bool fullscreen, string? language = null) => UpdatePreferencesAsync(current =>
    {
        current.Volume = volume;
        current.Fullscreen = fullscreen;
        if (language is not null)
            current.Language = DodgeLocalization.Normalize(language);
        return current;
    });

    /// <summary>Persists only a language selection, leaving every unsaved settings-view draft untouched.</summary>
    internal Task SetLanguageAsync(string language) => UpdatePreferencesAsync(current =>
    {
        current.Language = DodgeLocalization.Normalize(language);
        return current;
    });

    /// <summary>Serializes best-score updates with settings writes without replacing audio, graphics or language.</summary>
    internal Task SaveBestScoreAsync(int score)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(score);
        return UpdatePreferencesAsync(current =>
        {
            current.BestScore = Math.Max(current.BestScore, score);
            return current;
        });
    }

    /// <summary>Returns whether an audio/display draft differs; language is saved immediately and is excluded.</summary>
    internal bool HasPendingSettings(DodgePreferences draft) =>
        !draft.Volume.Equals(Preferences.Volume) || !draft.BgmVolume.Equals(Preferences.BgmVolume) ||
        !draft.SfxVolume.Equals(Preferences.SfxVolume) || draft.Fullscreen != Preferences.Fullscreen ||
        draft.ResolutionWidth != Preferences.ResolutionWidth || draft.ResolutionHeight != Preferences.ResolutionHeight;

    /// <summary>Applies a validated startup snapshot after storage has successfully loaded it.</summary>
    private void ApplyPreferences()
    {
        SettingsRules.Validate(Preferences);
        EnsureAudioBuses();
        ApplyNativePreferences(Preferences, previous: null);
    }

    /// <summary>Checks every persisted field before publishing preferences or invoking native systems.</summary>
    private async Task UpdatePreferencesAsync(Func<DodgePreferences, DodgePreferences> update)
    {
        await _preferencesGate.WaitAsync(_cancellationToken).ConfigureAwait(true);
        try
        {
            var candidate = update(Preferences.Copy());
            SettingsRules.Validate(candidate);
            EnsureAudioBuses();
            var native = CaptureNativePreferences();
            try
            {
                ApplyNativePreferences(candidate, Preferences);
                // Storage already provides a complete temporary-file replacement. A write failure
                // retains its old file; native state is restored before the error reaches the view.
                await _storage.WriteAsync(_dataRoot + "/preferences.json", candidate).ConfigureAwait(true);
            }
            catch (Exception exception)
            {
                try
                {
                    RestoreNativePreferences(native);
                }
                catch (Exception rollback) { throw new AggregateException("Preferences failed and native rollback also failed.", exception, rollback); }
                throw;
            }
            Preferences = candidate;
            _presentation.SetStatus(() => _text.Get("settings_saved"));
        }
        finally { _preferencesGate.Release(); }
    }

    /// <summary>Creates only missing sample audio buses, preserving existing project routing and gain settings.</summary>
    private static void EnsureAudioBuses()
    {
        foreach (var name in PreferenceBuses)
        {
            if (AudioServer.GetBusIndex(name) >= 0)
                continue;
            AudioServer.AddBus();
            var index = AudioServer.BusCount - 1;
            AudioServer.SetBusName(index, name);
            AudioServer.SetBusSend(index, "Master");
        }
    }

    /// <summary>Updates only changed native categories so language or score writes do not reset the current display.</summary>
    private void ApplyNativePreferences(DodgePreferences next, DodgePreferences? previous)
    {
        if (previous is null || !next.Volume.Equals(previous.Volume) || !next.BgmVolume.Equals(previous.BgmVolume) ||
            !next.SfxVolume.Equals(previous.SfxVolume))
        {
            SetAudioBus("Master", next.Volume);
            SetAudioBus("BGM", next.BgmVolume);
            SetAudioBus("SFX", next.SfxVolume);
        }
        if (previous is null || next.Fullscreen != previous.Fullscreen || next.ResolutionWidth != previous.ResolutionWidth ||
            next.ResolutionHeight != previous.ResolutionHeight)
            ApplyGraphicsPreferences(next);
        if (previous is null || !string.Equals(next.Language, previous.Language, StringComparison.Ordinal))
            _text.ApplySaved(next.Language);
    }

    /// <summary>Converts normalized volume to native gain while preserving exact silence through the mute flag.</summary>
    private static void SetAudioBus(string name, float volume)
    {
        var index = AudioServer.GetBusIndex(name);
        if (index < 0)
            throw new InvalidOperationException("Required audio bus is absent: " + name);
        AudioServer.SetBusVolumeDb(index, Mathf.LinearToDb(Math.Max(volume, 0.0001f)));
        AudioServer.SetBusMute(index, volume <= 0);
    }

    /// <summary>Applies display preferences with a fixed logical arena and aspect-correct desktop bounds.</summary>
    private void ApplyGraphicsPreferences(DodgePreferences preferences)
    {
        // Output size and logical gameplay coordinates are separate: wide windows letterbox
        // the original portrait arena instead of stretching its sprites or collision geometry.
        var window = GetWindow();
        window.ContentScaleSize = new Vector2I(480, 720);
        window.ContentScaleMode = Window.ContentScaleModeEnum.CanvasItems;
        window.ContentScaleAspect = Window.ContentScaleAspectEnum.Keep;
        if (string.Equals(DisplayServer.GetName(), "headless", StringComparison.Ordinal))
            return;
        DisplayServer.WindowSetMode(preferences.Fullscreen ? DisplayServer.WindowMode.Fullscreen : DisplayServer.WindowMode.Windowed);
        if (!preferences.Fullscreen)
        {
            var screen = DisplayServer.WindowGetCurrentScreen();
            var usable = DisplayServer.ScreenGetUsableRect(screen);
            // Presets remain preferences. Fit the actual client size to available desktop
            // space, reserving room for window chrome and keeping the exact 2:3 aspect.
            var maxWidth = Math.Max(2, usable.Size.X - 24);
            var maxHeight = Math.Max(3, usable.Size.Y - 64);
            var unit = Math.Max(1, Math.Min(preferences.ResolutionWidth / 2, Math.Min(maxWidth / 2, maxHeight / 3)));
            var size = new Vector2I(unit * 2, unit * 3);
            DisplayServer.WindowSetSize(size);
            DisplayServer.WindowSetPosition(usable.Position + (usable.Size - size) / 2);
        }
    }

    /// <summary>Captures applied native values before a transaction, excluding display operations in headless mode.</summary>
    private NativePreferencesSnapshot CaptureNativePreferences()
    {
        var audio = PreferenceBuses.Select(name =>
        {
            var index = AudioServer.GetBusIndex(name);
            return (index, AudioServer.GetBusVolumeDb(index), AudioServer.IsBusMute(index));
        }).ToArray();
        WindowPreferencesSnapshot? window = null;
        if (!string.Equals(DisplayServer.GetName(), "headless", StringComparison.Ordinal))
            window = new WindowPreferencesSnapshot(DisplayServer.WindowGetMode(), DisplayServer.WindowGetSize(), DisplayServer.WindowGetPosition());
        return new NativePreferencesSnapshot(audio, window, _text.CurrentLanguage);
    }

    /// <summary>Rolls back native audio, window and language values when applying or persisting a draft fails.</summary>
    private void RestoreNativePreferences(NativePreferencesSnapshot snapshot)
    {
        foreach (var (index, decibels, muted) in snapshot.Audio)
        {
            AudioServer.SetBusVolumeDb(index, decibels);
            AudioServer.SetBusMute(index, muted);
        }
        if (snapshot.Window is { } window)
        {
            DisplayServer.WindowSetMode(window.Mode);
            if (window.Mode == DisplayServer.WindowMode.Windowed)
                DisplayServer.WindowSetSize(window.Size);
            DisplayServer.WindowSetPosition(window.Position);
        }
        _text.SetLanguage(snapshot.Language);
    }

    private sealed record NativePreferencesSnapshot((int Index, float Decibels, bool Muted)[] Audio,
        WindowPreferencesSnapshot? Window, string Language);

    private sealed record WindowPreferencesSnapshot(DisplayServer.WindowMode Mode, Vector2I Size, Vector2I Position);
}
