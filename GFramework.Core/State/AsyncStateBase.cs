// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.State;

namespace GFramework.Core.State;

/// <summary>Optional asynchronous state convenience without context lookup or resource ownership.</summary>
public abstract class AsyncStateBase : IAsyncState
{
    /// <inheritdoc />
    public virtual Task OnEnterAsync(IState? from, CancellationToken cancellationToken = default) => Task.CompletedTask;
    /// <inheritdoc />
    public virtual Task OnExitAsync(IState? to, CancellationToken cancellationToken = default) => Task.CompletedTask;
    /// <inheritdoc />
    public virtual Task<bool> CanTransitionToAsync(IState target, CancellationToken cancellationToken = default) => Task.FromResult(true);
    /// <inheritdoc />
    void IState.OnEnter(IState? from) => throw new NotSupportedException("Use the asynchronous state machine entry.");
    /// <inheritdoc />
    void IState.OnExit(IState? to) => throw new NotSupportedException("Use the asynchronous state machine exit.");
    /// <inheritdoc />
    bool IState.CanTransitionTo(IState target) => throw new NotSupportedException("Use the asynchronous state machine guard.");
}
