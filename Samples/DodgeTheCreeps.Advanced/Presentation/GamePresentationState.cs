// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using DodgeTheCreeps.Advanced.Gameplay;

namespace DodgeTheCreeps.Advanced.Presentation;

/// <summary>Typed presentation notifications; it exposes no service lookup or mutable game objects.</summary>
internal sealed class GamePresentationState
{
    /// <summary>Creates presentation notifications with their explicitly supplied text service.</summary>
    internal GamePresentationState(DodgeLocalization text) => Text = text;

    /// <summary>The localization service used by feature feedback.</summary>
    internal DodgeLocalization Text
    {
        get;
    }

    /// <summary>Raised when the board commits a new score.</summary>
    internal event EventHandler<ScoreChangedEventArgs>? ScoreChanged;
    /// <summary>Raised when a user-visible status message changes.</summary>
    internal event EventHandler<StatusChangedEventArgs>? StatusChanged;
    /// <summary>Raised when the native collision boundary ends the board.</summary>
    internal event EventHandler? RunEnded;
    /// <summary>Publishes score feedback to the current native HUD.</summary>
    internal void SetScore(int score) => ScoreChanged?.Invoke(this, new ScoreChangedEventArgs(score));
    /// <summary>Publishes localized status feedback to the current menu.</summary>
    internal void SetStatus(Func<string> text) => StatusChanged?.Invoke(this, new StatusChangedEventArgs(text));
    /// <summary>Publishes a status message that does not require translation.</summary>
    internal void SetStatus(string text) => SetStatus(() => text);
    /// <summary>Requests the application game-over use case without putting persistence in the world node.</summary>
    internal void EndRun() => RunEnded?.Invoke(this, EventArgs.Empty);
}
