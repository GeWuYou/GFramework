// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Coroutine;

namespace GFramework.Core.Coroutine.Instructions;

/// <summary>
///     将协程移动到指定宿主阶段，并保持该阶段直到再次显式切换。
/// </summary>
/// <param name="stage">下一次继续执行所使用的阶段。</param>
/// <remarks>
///     调度器验证宿主支持该阶段；恢复发生在目标阶段的下一次推进中。
///     Deferred 表示延迟调用阶段，不表示渲染完成。
/// </remarks>
public sealed class SwitchTo(CoroutineExecutionStage stage) : IYieldInstruction
{
    /// <summary>
    ///     获取切换目标；调度器负责解释该值并移动协程。
    /// </summary>
    public CoroutineExecutionStage Stage { get; } = stage;

    /// <inheritdoc />
    public bool IsDone => true;

    /// <inheritdoc />
    public void Update(double deltaTime)
    {
        // 阶段队列迁移由调度器完成；该指令不自行推进宿主。
    }
}
