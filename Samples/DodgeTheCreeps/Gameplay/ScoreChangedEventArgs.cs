// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;

namespace DodgeTheCreeps.Gameplay;

/// <summary>Describes the latest survival score without exposing the gameplay node's internal state.</summary>
/// <param name="score">Completed survival seconds.</param>
public sealed class ScoreChangedEventArgs(int score) : EventArgs
{
    /// <summary>Gets the completed survival seconds.</summary>
    public int Score { get; } = score;
}
