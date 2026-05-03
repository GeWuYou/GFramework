// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Bases;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     表示显式声明负优先级的混合测试系统。
/// </summary>
public class MixedTestSystemNegativePriority : AbstractSystem, IMixedTestSystem, IPrioritized
{
    /// <summary>
    ///     获取当前测试系统的排序优先级。
    /// </summary>
    public int Priority => -10;

    /// <summary>
    ///     保持空初始化，以便测试仅覆盖优先级排序行为。
    /// </summary>
    protected override void OnInit()
    {
    }
}
