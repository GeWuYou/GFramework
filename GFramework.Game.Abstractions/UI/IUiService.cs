// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Abstractions.UI;

/// <summary>
/// Owns UI state, guarded navigation, window order and cleanup independently of an engine or Architecture.
/// Mutations are serialized; lifecycle callbacks must not await another operation on this service.
/// </summary>
public interface IUiService : IAsyncDisposable
{
    /// <summary>Gets an immutable copy of committed state and operation diagnostics.</summary>
    UiSnapshot Snapshot { get; }
    /// <summary>Pushes a page, hiding the previous page while retaining its navigation history.</summary>
    /// <typeparam name="TArgs">Opening argument type.</typeparam>
    /// <param name="route">Registered page definition.</param>
    /// <param name="args">Factory arguments.</param>
    /// <param name="cancellationToken">Cancellation of admission and preparation.</param>
    /// <returns>Operation outcome and page identity.</returns>
    Task<UiOperationResult> PushAsync<TArgs>(UiRoute<TArgs> route, TArgs args, CancellationToken cancellationToken = default);
    /// <summary>Prepares a page, then replaces page history; independent windows remain open.</summary>
    /// <typeparam name="TArgs">Opening argument type.</typeparam>
    /// <param name="route">Registered page definition.</param>
    /// <param name="args">Factory arguments.</param>
    /// <param name="cancellationToken">Cancellation of admission and preparation.</param>
    /// <returns>Operation outcome and page identity.</returns>
    Task<UiOperationResult> ReplaceAsync<TArgs>(UiRoute<TArgs> route, TArgs args, CancellationToken cancellationToken = default);
    /// <summary>Shows an independent window, reusing the current instance unless multiple instances are enabled.</summary>
    /// <typeparam name="TArgs">Opening argument type.</typeparam>
    /// <param name="route">Registered window definition.</param>
    /// <param name="args">Factory arguments.</param>
    /// <param name="cancellationToken">Cancellation of admission and preparation.</param>
    /// <returns>Operation outcome and window identity.</returns>
    Task<UiOperationResult> ShowAsync<TArgs>(UiRoute<TArgs> route, TArgs args, CancellationToken cancellationToken = default);
    /// <summary>Shows a dialog and waits outside the mutation queue until it closes.</summary>
    /// <typeparam name="TArgs">Opening argument type.</typeparam>
    /// <typeparam name="TResult">Business result type.</typeparam>
    /// <param name="route">Registered dialog definition.</param>
    /// <param name="args">Factory arguments.</param>
    /// <param name="cancellationToken">Cancellation closes this dialog before cancelling the waiter.</param>
    /// <returns>Accepted business value or a dismissed result.</returns>
    Task<DialogResult<TResult>> ShowDialogAsync<TArgs, TResult>(UiDialogRoute<TArgs, TResult> route, TArgs args,
        CancellationToken cancellationToken = default);
    /// <summary>Closes one owned instance after its leave guards accept.</summary>
    /// <param name="handle">Instance owned by this service.</param>
    /// <param name="cancellationToken">Cancellation before cleanup starts.</param>
    /// <returns>Close outcome after cleanup.</returns>
    Task<UiOperationResult> CloseAsync(UiInstanceHandle handle, CancellationToken cancellationToken = default);
    /// <summary>Closes the top back-enabled window, then the top back-enabled page when no window exists.</summary>
    /// <param name="cancellationToken">Cancellation before cleanup starts.</param>
    /// <returns>Back outcome.</returns>
    Task<UiOperationResult> BackAsync(CancellationToken cancellationToken = default);
    /// <summary>Closes every view only if all leave guards accept.</summary>
    /// <param name="cancellationToken">Cancellation before cleanup starts.</param>
    /// <returns>Clear outcome.</returns>
    Task<UiOperationResult> ClearAsync(CancellationToken cancellationToken = default);
    /// <summary>Dispatches to the highest eligible capturing view, using normal back behavior for unhandled Cancel.</summary>
    /// <param name="action">Semantic action.</param>
    /// <param name="cancellationToken">Cancellation of dispatch.</param>
    /// <returns>Whether any view captured the action.</returns>
    Task<bool> DispatchActionAsync(UiInputAction action, CancellationToken cancellationToken = default);
    /// <summary>Closes admission, joins accepted work, then cleans all instances without guards. Repeated calls share one result.</summary>
    /// <returns>Completion of cleanup.</returns>
    Task StopAsync();
}
