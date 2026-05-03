// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.StateManagement;

/// <summary>
///     表示一条 Store 历史快照记录。
///     该记录用于撤销/重做和调试面板查看历史状态，不会暴露 Store 的内部可变结构。
/// </summary>
/// <typeparam name="TState">状态树的根状态类型。</typeparam>
public sealed class StoreHistoryEntry<TState>
{
    /// <summary>
    ///     初始化一条历史记录。
    /// </summary>
    /// <param name="state">该历史点对应的状态快照。</param>
    /// <param name="recordedAt">该历史点被记录的时间。</param>
    /// <param name="action">触发该状态的 action；若为初始状态或已清空历史后的锚点，则为 <see langword="null"/>。</param>
    public StoreHistoryEntry(TState state, DateTimeOffset recordedAt, object? action = null)
    {
        State = state;
        RecordedAt = recordedAt;
        Action = action;
    }

    /// <summary>
    ///     获取该历史点对应的状态快照。
    /// </summary>
    public TState State { get; }

    /// <summary>
    ///     获取该历史点被记录的时间。
    /// </summary>
    public DateTimeOffset RecordedAt { get; }

    /// <summary>
    ///     获取触发该历史点的 action 实例。
    ///     对于初始状态或调用 <c>ClearHistory()</c> 后的新锚点，该值为 <see langword="null"/>。
    /// </summary>
    public object? Action { get; }

    /// <summary>
    ///     获取触发该历史点的 action 运行时类型。
    ///     若该历史点没有关联 action，则返回 <see langword="null"/>。
    /// </summary>
    public Type? ActionType => Action?.GetType();
}