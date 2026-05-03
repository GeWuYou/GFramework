// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using GFramework.Core.Abstractions.Utility;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     为 <see cref="RegistryInitializationHookBaseTests" /> 记录注册结果的测试注册表。
/// </summary>
public class TestRegistry : IUtility
{
    private readonly List<string> _registeredConfigs = [];

    /// <summary>
    ///     获取已注册配置值的只读视图，避免将测试内部使用的列表实现暴露给调用方。
    /// </summary>
    public IReadOnlyList<string> RegisteredConfigs => _registeredConfigs;

    /// <summary>
    ///     记录一次配置注册。
    /// </summary>
    /// <param name="config">要追加到测试结果中的配置值。</param>
    public void Register(string config)
    {
        _registeredConfigs.Add(config);
    }
}
