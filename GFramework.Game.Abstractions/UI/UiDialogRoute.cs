// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Abstractions.UI;

/// <summary>A window route whose accepted result is delivered after its view has been cleaned up.</summary>
/// <typeparam name="TArgs">Opening arguments.</typeparam>
/// <typeparam name="TResult">Accepted business result.</typeparam>
public sealed class UiDialogRoute<TArgs, TResult> : UiRoute<TArgs>
{
    /// <summary>Creates a dialog definition. Dialogs must use window presentation.</summary>
    /// <param name="key">Stable route identifier.</param>
    /// <param name="options">Window policy; defaults to a modal window.</param>
    public UiDialogRoute(string key, UiRouteOptions? options = null)
        : base(key, options ?? new UiRouteOptions { Kind = UiRouteKind.Window, Modal = true })
    {
        if (Options.Kind != UiRouteKind.Window)
            throw new ArgumentException("Dialogs must use window presentation.", nameof(options));
    }
}
