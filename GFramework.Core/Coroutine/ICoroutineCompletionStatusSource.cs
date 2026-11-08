// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Coroutine;

namespace GFramework.Core.Coroutine;

/// <summary>Preserves a known task outcome when its original exception type also represents cooperative cancellation.</summary>
internal interface ICoroutineCompletionStatusSource
{
    /// <summary>The underlying operation's explicit status; Unknown leaves classification to the coroutine exception contract.</summary>
    CoroutineCompletionStatus CompletionStatus
    {
        get;
    }
}
