// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.ExceptionServices;
using GFramework.Core.Abstractions.Coroutine;

namespace GFramework.Core.Coroutine;

/// <summary>The immutable outcome retained by joins and completion tasks.</summary>
/// <param name="Status">Terminal state, or Unknown when the identity has no retained outcome.</param>
/// <param name="Exception">The original failure, including any additional cleanup failures.</param>
public readonly record struct CoroutineResult(CoroutineCompletionStatus Status, Exception? Exception = null)
{
    /// <summary>Propagates failure or cancellation; an unknown/evicted outcome cannot establish successful completion.</summary>
    public void ThrowIfFailed()
    {
        if (Exception != null)
        {
            ExceptionDispatchInfo.Capture(Exception).Throw();
        }
        if (Status == CoroutineCompletionStatus.Cancelled)
        {
            throw new OperationCanceledException("The awaited coroutine was cancelled.");
        }
        if (Status == CoroutineCompletionStatus.Faulted)
        {
            throw new InvalidOperationException("The awaited coroutine failed without an exception.");
        }
        if (Status == CoroutineCompletionStatus.Unknown)
        {
            throw new InvalidOperationException("The awaited coroutine outcome is unknown or no longer retained.");
        }
    }
}
