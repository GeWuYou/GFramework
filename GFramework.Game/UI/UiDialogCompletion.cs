// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.UI;

namespace GFramework.Game.UI;

/// <summary>Keeps dialog result typing at its public boundary without reflection or dynamic activation.</summary>
/// <typeparam name="TResult">Business result type.</typeparam>
internal sealed class UiDialogCompletion<TResult> : IUiDialogCompletion
{
    private readonly TaskCompletionSource<DialogResult<TResult>> _source =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Gets the shared result task for a single-instance dialog.</summary>
    public Task<DialogResult<TResult>> Task => _source.Task;
    /// <inheritdoc />
    public void Finish(bool accepted, object? value) =>
        _source.TrySetResult(new DialogResult<TResult>(accepted, accepted ? (TResult)value! : default));
    /// <inheritdoc />
    public void Fail(Exception exception) => _source.TrySetException(exception);
    /// <inheritdoc />
    public void Cancel(CancellationToken cancellationToken) => _source.TrySetCanceled(cancellationToken);
}
