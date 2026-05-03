// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.StateManagement;

/// <summary>
///     表示一条由 Store 状态变更桥接到 EventBus 的事件。
///     该事件会复用 Store 对订阅通知的折叠语义，因此在批处理中只会发布最终状态。
/// </summary>
/// <typeparam name="TState">状态树的根状态类型。</typeparam>
public sealed class StoreStateChangedEvent<TState>
{
    /// <summary>
    ///     初始化一个新的 Store 状态变更桥接事件。
    /// </summary>
    /// <param name="state">最新状态快照。</param>
    /// <param name="changedAt">状态变更时间。</param>
    public StoreStateChangedEvent(TState state, DateTimeOffset changedAt)
    {
        State = state;
        ChangedAt = changedAt;
    }

    /// <summary>
    ///     获取最新状态快照。
    /// </summary>
    public TState State { get; }

    /// <summary>
    ///     获取该状态对外广播的时间。
    /// </summary>
    public DateTimeOffset ChangedAt { get; }
}