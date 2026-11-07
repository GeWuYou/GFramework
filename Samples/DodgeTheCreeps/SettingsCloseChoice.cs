// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace DodgeTheCreeps;

/// <summary>Explicit settings-close outcomes, separate from cancelling the dialog itself.</summary>
internal enum SettingsCloseChoice
{
    /// <summary>Keep editing the existing draft.</summary>
    Cancel,
    /// <summary>Save the draft and close the settings window.</summary>
    Apply,
    /// <summary>Close without saving the audio/display draft.</summary>
    Discard
}
