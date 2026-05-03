// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Bases;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     表示优先级为 30 的测试系统。
/// </summary>
public class PriorityTestSystemC : AbstractSystem, IPriorityTestSystem, IPrioritized
{
    /// <summary>
    ///     获取当前测试系统的排序优先级。
    /// </summary>
    public int Priority => 30;

    /// <summary>
    ///     保持空初始化，以便测试仅覆盖优先级排序行为。
    /// </summary>
    protected override void OnInit()
    {
    }
}
