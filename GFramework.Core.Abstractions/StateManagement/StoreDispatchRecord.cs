// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.StateManagement;

/// <summary>
///     记录最近一次 Store 分发的结果。
///     该结构为调试和诊断提供稳定的只读视图，避免调用方直接依赖 Store 的内部状态。
/// </summary>
/// <typeparam name="TState">状态树的根状态类型。</typeparam>
public sealed class StoreDispatchRecord<TState>
{
    /// <summary>
    ///     初始化一条分发记录。
    /// </summary>
    /// <param name="action">本次分发的 action。</param>
    /// <param name="previousState">分发前状态。</param>
    /// <param name="nextState">分发后状态。</param>
    /// <param name="hasStateChanged">是否发生了有效状态变化。</param>
    /// <param name="dispatchedAt">分发时间。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="action"/> 为 <see langword="null"/> 时抛出。</exception>
    public StoreDispatchRecord(
        object action,
        TState previousState,
        TState nextState,
        bool hasStateChanged,
        DateTimeOffset dispatchedAt)
    {
        Action = action ?? throw new ArgumentNullException(nameof(action));
        PreviousState = previousState;
        NextState = nextState;
        HasStateChanged = hasStateChanged;
        DispatchedAt = dispatchedAt;
    }

    /// <summary>
    ///     获取本次分发的 action 实例。
    /// </summary>
    public object Action { get; }

    /// <summary>
    ///     获取本次分发的 action 运行时类型。
    /// </summary>
    public Type ActionType => Action.GetType();

    /// <summary>
    ///     获取分发前状态。
    /// </summary>
    public TState PreviousState { get; }

    /// <summary>
    ///     获取分发后状态。
    /// </summary>
    public TState NextState { get; }

    /// <summary>
    ///     获取本次分发是否产生了有效状态变化。
    /// </summary>
    public bool HasStateChanged { get; }

    /// <summary>
    ///     获取分发时间。
    /// </summary>
    public DateTimeOffset DispatchedAt { get; }
}