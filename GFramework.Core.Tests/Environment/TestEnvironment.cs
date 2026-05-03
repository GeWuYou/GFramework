// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Environment;

namespace GFramework.Core.Tests.Environment;

/// <summary>
///     为环境相关测试提供可写注册入口的测试环境实现。
/// </summary>
public sealed class TestEnvironment : EnvironmentBase
{
    /// <summary>
    ///     获取测试环境名称。
    /// </summary>
    public override string Name { get; } = "TestEnvironment";

    /// <summary>
    ///     将测试数据注册到基础环境存储中，便于测试通过显式测试辅助入口准备上下文。
    /// </summary>
    /// <param name="key">要注册的环境键。</param>
    /// <param name="value">要注册的环境值。</param>
    public void RegisterForTest(string key, object value)
    {
        base.Register(key, value);
    }

    /// <summary>
    ///     初始化测试环境。
    /// </summary>
    /// <remarks>
    ///     当前测试环境没有额外初始化逻辑，但仍保留重写以匹配 <see cref="EnvironmentBase"/> 契约。
    /// </remarks>
    public override void Initialize()
    {
    }
}
