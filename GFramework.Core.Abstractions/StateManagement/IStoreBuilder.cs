// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.StateManagement;

/// <summary>
///     定义 Store 构建器接口，用于在创建 Store 之前完成 reducer、中间件和比较器配置。
///     该抽象适用于模块化注册、依赖注入装配和测试工厂，避免调用方必须依赖具体 Store 类型进行配置。
/// </summary>
/// <typeparam name="TState">状态树的根状态类型。</typeparam>
public interface IStoreBuilder<TState>
{
    /// <summary>
    ///     配置用于判断状态是否真正变化的比较器。
    /// </summary>
    /// <param name="comparer">状态比较器。</param>
    /// <returns>当前构建器实例。</returns>
    IStoreBuilder<TState> WithComparer(IEqualityComparer<TState> comparer);

    /// <summary>
    ///     配置历史缓冲区容量。
    ///     传入 0 表示禁用历史记录；大于 0 时会保留最近若干个状态快照，用于撤销、重做和时间旅行调试。
    /// </summary>
    /// <param name="historyCapacity">历史缓冲区容量。</param>
    /// <returns>当前构建器实例。</returns>
    IStoreBuilder<TState> WithHistoryCapacity(int historyCapacity);

    /// <summary>
    ///     配置 reducer 的 action 匹配策略。
    ///     默认使用 <see cref="StoreActionMatchingMode.ExactTypeOnly"/>，仅在需要复用基类或接口 action 层次时再启用多态匹配。
    /// </summary>
    /// <param name="actionMatchingMode">要使用的匹配策略。</param>
    /// <returns>当前构建器实例。</returns>
    IStoreBuilder<TState> WithActionMatching(StoreActionMatchingMode actionMatchingMode);

    /// <summary>
    ///     添加一个强类型 reducer。
    /// </summary>
    /// <typeparam name="TAction">当前 reducer 处理的 action 类型。</typeparam>
    /// <param name="reducer">要添加的 reducer。</param>
    /// <returns>当前构建器实例。</returns>
    IStoreBuilder<TState> AddReducer<TAction>(IReducer<TState, TAction> reducer);

    /// <summary>
    ///     使用委托快速添加一个 reducer。
    /// </summary>
    /// <typeparam name="TAction">当前 reducer 处理的 action 类型。</typeparam>
    /// <param name="reducer">执行归约的委托。</param>
    /// <returns>当前构建器实例。</returns>
    IStoreBuilder<TState> AddReducer<TAction>(Func<TState, TAction, TState> reducer);

    /// <summary>
    ///     添加一个 Store 中间件。
    /// </summary>
    /// <param name="middleware">要添加的中间件。</param>
    /// <returns>当前构建器实例。</returns>
    IStoreBuilder<TState> UseMiddleware(IStoreMiddleware<TState> middleware);

    /// <summary>
    ///     基于给定初始状态创建一个新的 Store。
    /// </summary>
    /// <param name="initialState">Store 的初始状态。</param>
    /// <returns>已应用当前构建器配置的 Store 实例。</returns>
    IStore<TState> Build(TState initialState);
}