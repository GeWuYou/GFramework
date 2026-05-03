// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Tests.Extensions;

/// <summary>
///     为 <see cref="ObjectExtensionsTests" /> 提供类型匹配断言所需的简单测试对象。
/// </summary>
public class TestClass
{
    /// <summary>
    ///     获取或设置用于数值分支断言的测试值。
    /// </summary>
    public int Value { get; set; }

    /// <summary>
    ///     获取或设置用于字符串结果断言的测试名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
