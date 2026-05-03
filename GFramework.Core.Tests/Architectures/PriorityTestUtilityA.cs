// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Bases;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     表示优先级为 10 的测试工具。
/// </summary>
public class PriorityTestUtilityA : IPriorityTestUtility, IPrioritized
{
    /// <summary>
    ///     获取当前测试工具的排序优先级。
    /// </summary>
    public int Priority => 10;
}
