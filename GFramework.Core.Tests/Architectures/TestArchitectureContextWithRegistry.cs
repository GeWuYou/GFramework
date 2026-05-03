// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Architectures;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     为 <see cref="RegistryInitializationHookBaseTests" /> 在架构上下文中暴露 <see cref="TestRegistry" /> 的测试替身。
/// </summary>
public class TestArchitectureContextWithRegistry : TestArchitectureContext
{
    private readonly TestRegistry _registry;

    /// <summary>
    ///     使用给定测试注册表创建上下文测试替身。
    /// </summary>
    /// <param name="registry">需要通过 <see cref="GetUtility{TUtility}" /> 返回的测试注册表。</param>
    public TestArchitectureContextWithRegistry(TestRegistry registry)
    {
        _registry = registry;
    }

    /// <summary>
    ///     在请求 <see cref="TestRegistry" /> 时返回测试注册表，其余类型回退到基类实现。
    /// </summary>
    /// <typeparam name="TUtility">请求的工具类型。</typeparam>
    /// <returns>匹配时返回测试注册表，否则返回基类结果。</returns>
    public override TUtility GetUtility<TUtility>()
    {
        if (typeof(TUtility) == typeof(TestRegistry))
        {
            return (TUtility)(object)_registry;
        }

        return base.GetUtility<TUtility>();
    }
}
