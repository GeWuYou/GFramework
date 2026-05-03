// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Query;

namespace GFramework.Core.Tests.Query;

/// <summary>
///     为 <see cref="AsyncQueryExecutorTests" /> 提供布尔结果的测试异步查询。
/// </summary>
public sealed class TestAsyncBooleanQuery : AbstractAsyncQuery<TestAsyncQueryInput, bool>
{
    /// <summary>
    ///     初始化 <see cref="TestAsyncBooleanQuery" /> 的新实例。
    /// </summary>
    /// <param name="input">查询输入参数。</param>
    public TestAsyncBooleanQuery(TestAsyncQueryInput input) : base(input)
    {
    }

    /// <summary>
    ///     执行异步查询并根据输入值是否大于零返回布尔结果。
    /// </summary>
    /// <param name="input">查询输入参数。</param>
    /// <returns>当输入值大于 0 时返回 <see langword="true" />；否则返回 <see langword="false" />。</returns>
    protected override Task<bool> OnDoAsync(TestAsyncQueryInput input)
    {
        return Task.FromResult(input.Value > 0);
    }
}
