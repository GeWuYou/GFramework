// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.StateManagement;

/// <summary>
///     表示一次 Store 分发流程中的上下文数据。
///     中间件和 Store 实现通过该对象共享当前 action、分发时间以及归约结果。
/// </summary>
/// <typeparam name="TState">状态树的根状态类型。</typeparam>
public sealed class StoreDispatchContext<TState>
{
    /// <summary>
    ///     初始化一个新的分发上下文。
    /// </summary>
    /// <param name="action">当前分发的 action。</param>
    /// <param name="previousState">分发前的状态快照。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="action"/> 为 <see langword="null"/> 时抛出。</exception>
    public StoreDispatchContext(object action, TState previousState)
    {
        Action = action ?? throw new ArgumentNullException(nameof(action));
        PreviousState = previousState;
        NextState = previousState;
        DispatchedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    ///     获取当前分发的 action 实例。
    /// </summary>
    public object Action { get; }

    /// <summary>
    ///     获取当前分发的 action 运行时类型。
    /// </summary>
    public Type ActionType => Action.GetType();

    /// <summary>
    ///     获取分发前的状态快照。
    /// </summary>
    public TState PreviousState { get; }

    /// <summary>
    ///     获取或设置归约后的下一状态。
    ///     Store 会在 reducer 执行完成后使用该值更新内部状态。
    /// </summary>
    public TState NextState { get; set; }

    /// <summary>
    ///     获取或设置本次分发是否导致状态发生变化。
    ///     中间件可读取该值进行日志和诊断，但通常应由 Store 负责最终判定。
    /// </summary>
    public bool HasStateChanged { get; set; }

    /// <summary>
    ///     获取本次分发创建时的时间戳。
    /// </summary>
    public DateTimeOffset DispatchedAt { get; }
}