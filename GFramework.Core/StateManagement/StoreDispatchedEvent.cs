// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.StateManagement;

namespace GFramework.Core.StateManagement;

/// <summary>
///     表示一条由 Store 分发桥接到 EventBus 的事件。
///     该事件用于让旧模块在不直接依赖 Store API 的情况下观察 action 分发结果。
/// </summary>
/// <typeparam name="TState">状态树的根状态类型。</typeparam>
public sealed class StoreDispatchedEvent<TState>
{
    /// <summary>
    ///     初始化一个新的 Store 分发桥接事件。
    /// </summary>
    /// <param name="dispatchRecord">本次分发记录。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="dispatchRecord"/> 为 <see langword="null"/> 时抛出。</exception>
    public StoreDispatchedEvent(StoreDispatchRecord<TState> dispatchRecord)
    {
        DispatchRecord = dispatchRecord ?? throw new ArgumentNullException(nameof(dispatchRecord));
    }

    /// <summary>
    ///     获取本次桥接对应的 Store 分发记录。
    /// </summary>
    public StoreDispatchRecord<TState> DispatchRecord { get; }
}