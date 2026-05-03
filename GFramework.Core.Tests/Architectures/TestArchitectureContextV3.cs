// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     为 <see cref="ArchitectureServicesTests" /> 提供最小实现的架构上下文测试桩。
/// </summary>
/// <remarks>
///     共享的容器解析、事件总线协作与 legacy CQRS 失败契约由 <see cref="TestArchitectureContextBase" /> 提供，
///     当前类型只补充 <see cref="ArchitectureServicesTests" /> 需要的上下文实例标识。
/// </remarks>
public class TestArchitectureContextV3 : TestArchitectureContextBase
{
    /// <summary>
    ///     获取或初始化用于区分测试上下文实例的标识。
    /// </summary>
    public int Id { get; init; }
}
