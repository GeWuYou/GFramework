// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Abstractions.Runtime;

/// <summary>Runs a complete asynchronous game operation on its owning host context.</summary>
/// <remarks>The caller retains ownership until the returned task completes. Cancellation must not abandon a started callback.</remarks>
public interface IGameDispatcher
{
    /// <summary>Dispatches an operation and observes its completion, including failures.</summary>
    /// <param name="callback">The host-bound operation. Its awaits must preserve the host context.</param>
    /// <param name="cancellationToken">Cancels dispatch before the callback starts.</param>
    /// <returns>The callback's completion.</returns>
    Task InvokeAsync(Func<Task> callback, CancellationToken cancellationToken = default);
}
