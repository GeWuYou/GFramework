// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.StateManagement;

/// <summary>
///     定义状态归约器接口。
///     Reducer 应保持纯函数风格：根据当前状态和 action 计算下一状态，
///     不直接产生副作用，也不依赖外部可变环境。
/// </summary>
/// <typeparam name="TState">状态树的根状态类型。</typeparam>
/// <typeparam name="TAction">当前 reducer 处理的 action 类型。</typeparam>
public interface IReducer<TState, in TAction>
{
    /// <summary>
    ///     根据当前状态和 action 计算下一状态。
    /// </summary>
    /// <param name="currentState">当前状态快照。</param>
    /// <param name="action">触发本次归约的 action。</param>
    /// <returns>归约后的下一状态。</returns>
    TState Reduce(TState currentState, TAction action);
}