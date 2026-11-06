// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     为 <see cref="GameContextTests" /> 提供最小可用的架构上下文测试桩。
/// </summary>
/// <remarks>
///     共享的容器解析与事件总线协作由 <see cref="TestArchitectureContextBase" /> 提供，
///     当前类型仅作为默认测试上下文命名入口，供现有测试与派生替身继续复用。
/// </remarks>
public class TestArchitectureContext : TestArchitectureContextBase
{
}
