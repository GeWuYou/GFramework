// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.StateManagement;

/// <summary>
///     定义状态选择器接口，用于从整棵状态树中投影出局部状态视图。
///     该抽象适用于复用复杂选择逻辑，避免在 UI 或 Controller 中重复编写投影代码。
/// </summary>
/// <typeparam name="TState">源状态类型。</typeparam>
/// <typeparam name="TSelected">投影后的局部状态类型。</typeparam>
public interface IStateSelector<in TState, out TSelected>
{
    /// <summary>
    ///     从给定状态中选择目标片段。
    /// </summary>
    /// <param name="state">当前完整状态。</param>
    /// <returns>投影后的局部状态。</returns>
    TSelected Select(TState state);
}