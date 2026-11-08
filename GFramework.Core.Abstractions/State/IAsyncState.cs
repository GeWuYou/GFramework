// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.State;

/// <summary>Asynchronous state callbacks selected in preference to synchronous callbacks.</summary>
public interface IAsyncState : IState
{
    /// <summary>Enter from the previous state; honor cancellation while acquiring resources.</summary>
    /// <param name="from">Previous state.</param>
    /// <param name="cancellationToken">Transition cancellation.</param>
    /// <returns>Entry completion.</returns>
    Task OnEnterAsync(IState? from, CancellationToken cancellationToken = default);
    /// <summary>Exit to the next state; null indicates terminal cleanup.</summary>
    /// <param name="to">Next state.</param>
    /// <param name="cancellationToken">Transition cancellation; terminal cleanup is not canceled.</param>
    /// <returns>Exit completion.</returns>
    Task OnExitAsync(IState? to, CancellationToken cancellationToken = default);
    /// <summary>Check whether leaving for the target is allowed.</summary>
    /// <param name="target">Target state.</param>
    /// <param name="cancellationToken">Transition cancellation.</param>
    /// <returns>Whether the transition is allowed.</returns>
    Task<bool> CanTransitionToAsync(IState target, CancellationToken cancellationToken = default);
}
