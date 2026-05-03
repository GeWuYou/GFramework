// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Bases;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     表示未声明优先级、依赖默认排序值的混合测试系统。
/// </summary>
public class MixedTestSystemWithoutPriority : AbstractSystem, IMixedTestSystem
{
    /// <summary>
    ///     保持空初始化，以便测试仅覆盖优先级排序行为。
    /// </summary>
    protected override void OnInit()
    {
    }
}
