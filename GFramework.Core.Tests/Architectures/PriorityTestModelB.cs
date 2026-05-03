// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Bases;
using GFramework.Core.Model;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     表示优先级为 20 的测试模型。
/// </summary>
public class PriorityTestModelB : AbstractModel, IPriorityTestModel, IPrioritized
{
    /// <summary>
    ///     获取当前测试模型的排序优先级。
    /// </summary>
    public int Priority => 20;

    /// <summary>
    ///     保持空初始化，以便测试仅覆盖优先级排序行为。
    /// </summary>
    protected override void OnInit()
    {
    }
}
