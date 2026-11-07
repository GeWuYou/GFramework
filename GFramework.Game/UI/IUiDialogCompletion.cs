// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.UI;

namespace GFramework.Game.UI;

/// <summary>Completes a typed dialog only after all owned cleanup has finished.</summary>
internal interface IUiDialogCompletion
{
    /// <summary>Publishes a submitted business value or normal dismissal.</summary>
    /// <param name="accepted">Whether a business value was submitted.</param>
    /// <param name="value">Typed value boxed inside the runtime.</param>
    void Finish(bool accepted, object? value);
    /// <summary>Publishes a lifecycle or cleanup failure.</summary>
    /// <param name="exception">Failure that prevented a clean completion.</param>
    void Fail(Exception exception);
    /// <summary>Publishes caller cancellation after reclaiming the dialog.</summary>
    /// <param name="cancellationToken">Original caller cancellation.</param>
    void Cancel(CancellationToken cancellationToken);
}
