// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Abstractions.UI;

/// <summary>Allows a dialog view to submit one typed result through its owning service.</summary>
/// <typeparam name="TResult">Business result type.</typeparam>
/// <remarks>Creates a typed dialog context.</remarks>
/// <param name="handle">Instance identity.</param>
/// <param name="close">Dismiss callback.</param>
/// <param name="complete">Accepted-result callback.</param>
public sealed class UiDialogContext<TResult>(UiInstanceHandle handle, Func<CancellationToken, Task<UiOperationResult>> close,
    Func<TResult, CancellationToken, Task<UiOperationResult>> complete) : UiViewContext(handle, close)
{
    private readonly Func<TResult, CancellationToken, Task<UiOperationResult>> _complete = complete ?? throw new ArgumentNullException(nameof(complete));

    /// <summary>Closes this dialog with a business result, completing its waiter after cleanup.</summary>
    /// <param name="result">Accepted result.</param>
    /// <param name="cancellationToken">Cancellation of the queued close request.</param>
    /// <returns>Close outcome.</returns>
    public Task<UiOperationResult> CompleteAsync(TResult result, CancellationToken cancellationToken = default) =>
        _complete(result, cancellationToken);
}
