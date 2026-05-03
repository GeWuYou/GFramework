// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Tests.Query;

/// <summary>
///     测试用复杂查询结果类V2
/// </summary>
public sealed class TestAsyncQueryResultV2
{
    /// <summary>
    ///     获取值；该值只能在对象初始化阶段设置。
    /// </summary>
    public int Value { get; init; }

    /// <summary>
    ///     获取三倍值；该值只能在对象初始化阶段设置。
    /// </summary>
    public int TripleValue { get; init; }
}
