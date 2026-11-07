// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Abstractions.Input;

/// <summary>
///     Maps logical action names to the standalone UI service without native engine event dependencies.
/// </summary>
public interface IUiInputDispatcher
{
    /// <summary>
    ///     Dispatches a logical action through the same asynchronous queue used for UI navigation.
    /// </summary>
    /// <param name="actionName">Configured logical action name.</param>
    /// <param name="cancellationToken">Cancellation of the dispatch request.</param>
    /// <returns>Whether a visible UI captured the mapped action.</returns>
    Task<bool> DispatchAsync(string actionName, CancellationToken cancellationToken = default);
}
