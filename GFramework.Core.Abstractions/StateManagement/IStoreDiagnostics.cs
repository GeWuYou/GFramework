// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.StateManagement;

/// <summary>
///     暴露 Store 的诊断信息。
///     该接口用于调试、监控和后续时间旅行能力的扩展，不参与状态写入流程。
/// </summary>
/// <typeparam name="TState">状态树的根状态类型。</typeparam>
public interface IStoreDiagnostics<TState>
{
    /// <summary>
    ///     获取当前已注册的订阅者数量。
    /// </summary>
    int SubscriberCount { get; }

    /// <summary>
    ///     获取最近一次分发的 action 类型。
    ///     即使该次分发未引起状态变化，该值也会更新。
    /// </summary>
    Type? LastActionType { get; }

    /// <summary>
    ///     获取最近一次真正改变状态的时间戳。
    ///     若尚未发生状态变化，则返回 <see langword="null"/>。
    /// </summary>
    DateTimeOffset? LastStateChangedAt { get; }

    /// <summary>
    ///     获取最近一次分发记录。
    /// </summary>
    StoreDispatchRecord<TState>? LastDispatchRecord { get; }

    /// <summary>
    ///     获取当前 Store 使用的 action 匹配策略。
    /// </summary>
    StoreActionMatchingMode ActionMatchingMode { get; }

    /// <summary>
    ///     获取历史缓冲区容量。
    ///     返回 0 表示当前 Store 未启用历史记录能力。
    /// </summary>
    int HistoryCapacity { get; }

    /// <summary>
    ///     获取当前可见历史记录数量。
    ///     当历史记录启用时，该值至少为 1，因为当前状态会作为历史锚点存在。
    /// </summary>
    int HistoryCount { get; }

    /// <summary>
    ///     获取当前状态在历史缓冲区中的索引。
    ///     当未启用历史记录时返回 -1。
    /// </summary>
    int HistoryIndex { get; }

    /// <summary>
    ///     获取当前是否处于批处理阶段。
    ///     该值为 <see langword="true"/> 时，状态变更通知会延迟到最外层批处理结束后再统一发送。
    /// </summary>
    bool IsBatching { get; }

    /// <summary>
    ///     获取当前历史快照列表的只读快照。
    ///     该方法会返回一份独立快照，供调试工具渲染时间旅行面板，而不暴露 Store 的内部可变集合。
    /// </summary>
    /// <returns>当前历史快照列表；若未启用历史记录或当前没有历史，则返回空数组。</returns>
    IReadOnlyList<StoreHistoryEntry<TState>> GetHistoryEntriesSnapshot();
}