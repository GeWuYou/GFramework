// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.StateManagement;

/// <summary>
///     定义 Store 分发中间件接口。
///     中间件用于在 action 分发前后插入日志、诊断、审计或拦截逻辑，
///     同时保持核心 Store 实现专注于状态归约与订阅通知。
/// </summary>
/// <typeparam name="TState">状态树的根状态类型。</typeparam>
public interface IStoreMiddleware<TState>
{
    /// <summary>
    ///     执行一次分发管线节点。
    ///     实现通常应调用 <paramref name="next"/> 继续后续处理；若选择短路，
    ///     需要自行保证上下文状态对调用方仍然是可解释的。
    /// </summary>
    /// <param name="context">当前分发上下文。</param>
    /// <param name="next">继续执行后续中间件或 reducer 的委托。</param>
    void Invoke(StoreDispatchContext<TState> context, Action next);
}