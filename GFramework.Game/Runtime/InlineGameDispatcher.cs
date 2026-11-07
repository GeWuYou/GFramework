// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Runtime;

namespace GFramework.Game.Runtime;

/// <summary>Executes operations directly for hosts without thread-affine engine objects.</summary>
/// <remarks>Do not use this dispatcher to access an engine scene tree from arbitrary threads.</remarks>
public sealed class InlineGameDispatcher : IGameDispatcher
{
    /// <inheritdoc />
    public Task InvokeAsync(Func<Task> callback, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(callback);
        cancellationToken.ThrowIfCancellationRequested();
        return callback();
    }
}
