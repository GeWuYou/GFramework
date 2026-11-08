// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.State;

/// <summary>Serializes state transitions and terminal cleanup. Registration freezes at the first transition.</summary>
/// <remarks>Registered states are borrowed: the machine invokes business cleanup but never disposes their resources.
/// Callbacks retain the caller synchronization context; hosts must dispatch operations onto their owning thread.</remarks>
public interface IStateMachine : IAsyncDisposable
{
    /// <summary>The active state, or null before entry and after stopping.</summary>
    IState? Current
    {
        get;
    }
    /// <summary>The first callback failure; a faulted machine only permits cleanup.</summary>
    Exception? Fault
    {
        get;
    }
    /// <summary>Register a unique concrete state type before the first transition.</summary>
    /// <param name="state">Borrowed state.</param>
    /// <returns>This machine.</returns>
    IStateMachine Register(IState state);
    /// <summary>Remove a state during configuration.</summary>
    /// <typeparam name="T">Concrete type.</typeparam>
    /// <returns>Whether a registration was removed.</returns>
    bool Unregister<T>() where T : IState;
    /// <summary>Check a transition without entering or exiting states.</summary>
    /// <typeparam name="T">Target type.</typeparam>
    /// <param name="cancellationToken">Cancels waiting and asynchronous guards.</param>
    /// <returns>Whether the current state permits the registered target.</returns>
    Task<bool> CanChangeToAsync<T>(CancellationToken cancellationToken = default) where T : IState;
    /// <summary>Enter a registered target after its outgoing guard accepts.</summary>
    /// <typeparam name="T">Target type.</typeparam>
    /// <param name="cancellationToken">Cancels waiting and asynchronous callbacks.</param>
    /// <returns>False for a rejected or same-state transition.</returns>
    Task<bool> ChangeToAsync<T>(CancellationToken cancellationToken = default) where T : IState;
    /// <summary>Check whether a state type is registered.</summary>
    /// <typeparam name="T">Concrete type.</typeparam>
    /// <returns>Whether registered.</returns>
    bool IsRegistered<T>() where T : IState;
    /// <summary>Read a borrowed state registration.</summary>
    /// <typeparam name="T">Concrete type.</typeparam>
    /// <returns>The state or null.</returns>
    T? GetState<T>() where T : class, IState;
    /// <summary>Snapshot registered types.</summary>
    /// <returns>An immutable membership snapshot.</returns>
    IEnumerable<Type> GetRegisteredStateTypes();
    /// <summary>Read the latest successful previous state.</summary>
    /// <returns>Previous state or null.</returns>
    IState? GetPreviousState();
    /// <summary>Snapshot history, most recent first.</summary>
    /// <returns>Borrowed state references.</returns>
    IReadOnlyList<IState> GetStateHistory();
    /// <summary>Return to the latest previous state using the same guards and notifications.</summary>
    /// <param name="cancellationToken">Cancels waiting and callbacks.</param>
    /// <returns>Whether a transition succeeded.</returns>
    Task<bool> GoBackAsync(CancellationToken cancellationToken = default);
    /// <summary>Clear history without changing registrations or the active state.</summary>
    void ClearHistory();
    /// <summary>Stop once, waiting for the active transition and attempting every started state's cleanup.</summary>
    /// <returns>The shared terminal cleanup result.</returns>
    ValueTask StopAsync();
}
