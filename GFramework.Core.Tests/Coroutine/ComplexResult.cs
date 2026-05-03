// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Tests.Coroutine;

/// <summary>
///     为 <see cref="ComplexQuery" /> 提供复杂对象结果的测试载体。
/// </summary>
internal class ComplexResult
{
    /// <summary>
    ///     获取或设置处理后的名称。
    /// </summary>
    public string ProcessedName { get; set; } = string.Empty;

    /// <summary>
    ///     获取或设置整数集合的求和结果。
    /// </summary>
    public int Sum { get; set; }

    /// <summary>
    ///     获取或设置整数集合中的元素数量。
    /// </summary>
    public int Count { get; set; }
}
