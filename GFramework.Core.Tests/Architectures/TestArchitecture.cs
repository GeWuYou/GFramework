// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Architectures;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     提供给 <see cref="GameContextTests" /> 的最小架构测试桩。
/// </summary>
public class TestArchitecture : Architecture
{
    /// <summary>
    ///     保持空初始化流程，便于测试仅验证 <see cref="GameContext" /> 的上下文绑定行为。
    /// </summary>
    protected override void OnInitialize()
    {
    }
}
