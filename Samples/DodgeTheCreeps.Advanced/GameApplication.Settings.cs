// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Threading.Tasks;

namespace DodgeTheCreeps.Advanced;

/// <summary>Routes view commands to the independently owned settings service.</summary>
public partial class GameApplication
{
    /// <summary>Applies a settings draft without overwriting score or locale updates.</summary>
    internal Task SaveSettingsAsync(DodgePreferences draft) => _settings.SaveSettingsAsync(draft);
    /// <summary>Updates quick audio/display preferences.</summary>
    internal Task SavePreferencesAsync(float volume, bool fullscreen, string? language = null) => _settings.SavePreferencesAsync(volume, fullscreen, language);
    /// <summary>Immediately saves a language change while preserving the open view's draft.</summary>
    internal Task SetLanguageAsync(string language) => _settings.SetLanguageAsync(language);
    /// <summary>Stores a monotonically increasing high score.</summary>
    internal Task SaveBestScoreAsync(int score) => _settings.SaveBestScoreAsync(score);
    /// <summary>Queries draft changes without mutating the last applied snapshot.</summary>
    internal bool HasPendingSettings(DodgePreferences draft) => _settings.HasPendingSettings(draft);
}
