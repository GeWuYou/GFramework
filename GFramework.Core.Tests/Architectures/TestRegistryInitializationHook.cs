// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using GFramework.Core.Abstractions.Enums;
using GFramework.Core.Architectures;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     为 <see cref="RegistryInitializationHookBaseTests" /> 提供的注册表初始化钩子测试替身。
/// </summary>
/// <remarks>
///     使用给定配置集合和目标阶段创建测试钩子。
/// </remarks>
/// <param name="configs">测试期间要注册到目标注册表的配置值。</param>
/// <param name="targetPhase">触发注册行为的架构阶段。</param>
public class TestRegistryInitializationHook(
    IEnumerable<string> configs,
    ArchitecturePhase targetPhase = ArchitecturePhase.AfterSystemInit) : RegistryInitializationHookBase<TestRegistry, string>(configs, targetPhase)
{

    /// <summary>
    ///     将当前配置值写入测试注册表。
    /// </summary>
    /// <param name="registry">要接收配置值的测试注册表。</param>
    /// <param name="config">当前遍历到的配置值。</param>
    protected override void RegisterConfig(TestRegistry registry, string config)
    {
        registry.Register(config);
    }
}
