// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Tests.Query;

/// <summary>
///     表示 <see cref="AsyncQueryExecutorTests" /> 使用的复杂测试查询结果。
/// </summary>
public sealed class TestAsyncQueryResult
{
    /// <summary>
    ///     获取主结果值；该值只能在对象初始化阶段设置。
    /// </summary>
    public int Value { get; init; }

    /// <summary>
    ///     获取派生的三倍结果值；该值只能在对象初始化阶段设置。
    /// </summary>
    public int TripleValue { get; init; }
}
