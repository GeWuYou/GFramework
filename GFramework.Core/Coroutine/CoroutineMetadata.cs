// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Coroutine;

namespace GFramework.Core.Coroutine;

/// <summary>
///     存储协程元数据信息的内部类，包含协程的状态、枚举器、标签等信息
/// </summary>
internal class CoroutineMetadata
{
    /// <summary>
    ///     协程所属调度器的执行阶段。
    ///     该值用于诊断等待语义是否与当前宿主阶段匹配。
    /// </summary>
    public CoroutineExecutionStage ExecutionStage;

    /// <summary>
    ///     协程的分组标识符，用于批量管理协程
    /// </summary>
    public string? Group;

    /// <summary>
    ///     协程的优先级
    /// </summary>
    public CoroutinePriority Priority;

    /// <summary>
    ///     协程在调度器中的槽位索引
    /// </summary>
    public int SlotIndex;

    /// <summary>
    ///     协程开始执行的时间戳（毫秒）
    /// </summary>
    public double StartTime;

    /// <summary>
    ///     协程当前的执行状态
    /// </summary>
    public CoroutineState State;

    /// <summary>
    ///     协程的标签标识符，用于协程的分类和查找
    /// </summary>
    public string? Tag;

    /// <summary>
    ///     判断协程是否处于活跃状态（运行中、暂停或挂起）
    /// </summary>
    public bool IsActive =>
        State is CoroutineState.Running
            or CoroutineState.Paused;
}