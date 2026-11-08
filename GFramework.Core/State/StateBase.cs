// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.State;

namespace GFramework.Core.State;

/// <summary>Optional synchronous state convenience; dependencies belong in the subclass constructor.</summary>
public abstract class StateBase : IState
{
    /// <inheritdoc />
    public virtual void OnEnter(IState? from)
    {
    }
    /// <inheritdoc />
    public virtual void OnExit(IState? to)
    {
    }
    /// <inheritdoc />
    public virtual bool CanTransitionTo(IState target) => true;
}
