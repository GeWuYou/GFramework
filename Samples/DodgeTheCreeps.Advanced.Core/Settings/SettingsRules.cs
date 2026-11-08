// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;

namespace DodgeTheCreeps.Advanced.Core.Settings;

/// <summary>Validates preference drafts before any native or persistence effect.</summary>
public static class SettingsRules
{
    /// <summary>Rejects non-finite audio values, unsupported sizes and invalid scores.</summary>
    /// <param name="preferences">The complete proposed preference snapshot.</param>
    public static void Validate(DodgePreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        foreach (var volume in new[] { preferences.Volume, preferences.BgmVolume, preferences.SfxVolume })
            if (!float.IsFinite(volume) || volume is < 0 or > 1)
                throw new InvalidOperationException("Preference volumes must be finite values from zero through one.");
        if (preferences.BestScore < 0 || !DodgePreferences.ResolutionChoices.Any(size =>
                size.Width == preferences.ResolutionWidth && size.Height == preferences.ResolutionHeight))
            throw new InvalidOperationException("The preferences contain an unsupported resolution or invalid score.");
        // Language is interpreted by the localization boundary: unknown legacy values
        // display English without making startup fail or rewriting the player's file.
    }

}
