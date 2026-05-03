// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.StateManagement;

/// <summary>
///     定义 Store 在分发 action 时的 reducer 匹配策略。
///     默认使用精确类型匹配，以保持执行结果和顺序的确定性；仅在确有需要时再启用多态匹配。
/// </summary>
public enum StoreActionMatchingMode
{
    /// <summary>
    ///     仅匹配与 action 运行时类型完全相同的 reducer。
    ///     该模式不会命中基类或接口注册，适合作为默认的稳定行为。
    /// </summary>
    ExactTypeOnly = 0,

    /// <summary>
    ///     在精确类型匹配之外，额外匹配可赋值的基类和接口 reducer。
    ///     Store 会保持确定性的执行顺序：精确类型优先，其次是最近的基类，最后是接口注册。
    /// </summary>
    IncludeAssignableTypes = 1
}