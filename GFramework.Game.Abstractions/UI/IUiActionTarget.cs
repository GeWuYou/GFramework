// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Abstractions.UI;

/// <summary>Optional semantic input handler on a view lease.</summary>
public interface IUiActionTarget
{
    /// <summary>Handles an action after the service has selected its eligible owner.</summary>
    /// <param name="action">Captured semantic action.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Whether business logic handled the action. Unhandled captured actions remain consumed.</returns>
    Task<bool> HandleActionAsync(UiInputAction action, CancellationToken cancellationToken = default);
}
