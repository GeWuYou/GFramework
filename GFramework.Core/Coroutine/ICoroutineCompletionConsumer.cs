// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Coroutine;

/// <summary>
///     消费已经观察到完成的异步等待结果，避免完成发生在 Update 与 IsDone 之间时丢失失败或取消。
/// </summary>
/// <remarks>调度器只在 IsDone 返回 true 后调用，不重复推进等待指令或触发业务回调。</remarks>
internal interface ICoroutineCompletionConsumer
{
    /// <summary>
    ///     在驱动线程消费终结结果，并传播原始失败或取消；调用时底层操作必须已经完成。
    /// </summary>
    void ConsumeCompletion();
}
