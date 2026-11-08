// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Abstractions.UI;

/// <summary>Gives a view instance explicit access to its own close operation without global service resolution.</summary>
/// <remarks>Creates a context connected to the owning service.</remarks>
/// <param name="handle">Instance identity.</param>
/// <param name="close">Owning service close callback.</param>
public class UiViewContext(UiInstanceHandle handle, Func<CancellationToken, Task<UiOperationResult>> close)
{
    private readonly Func<CancellationToken, Task<UiOperationResult>> _close = close ?? throw new ArgumentNullException(nameof(close));

    /// <summary>Gets the view instance identity.</summary>
    public UiInstanceHandle Handle { get; } = handle;
    /// <summary>Requests normal guarded closure from an event callback, outside lifecycle callbacks.</summary>
    /// <param name="cancellationToken">Cancellation of the queued request.</param>
    /// <returns>Close outcome after cleanup.</returns>
    public Task<UiOperationResult> CloseAsync(CancellationToken cancellationToken = default) => _close(cancellationToken);
}
