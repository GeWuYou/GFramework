// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Query;

namespace GFramework.Core.Tests.Query;

/// <summary>
///     为 <see cref="AsyncQueryExecutorTests" /> 提供固定抛出异常的测试异步查询。
/// </summary>
public sealed class TestAsyncQueryWithException : AbstractAsyncQuery<TestAsyncQueryInput, int>
{
    /// <summary>
    ///     初始化 <see cref="TestAsyncQueryWithException" /> 的新实例。
    /// </summary>
    /// <param name="input">查询输入参数。</param>
    public TestAsyncQueryWithException(TestAsyncQueryInput input) : base(input)
    {
    }

    /// <summary>
    ///     执行异步查询并始终抛出测试异常。
    /// </summary>
    /// <param name="input">查询输入参数。</param>
    /// <returns>此方法不会返回结果。</returns>
    /// <exception cref="InvalidOperationException">始终抛出，模拟查询执行失败。</exception>
    protected override Task<int> OnDoAsync(TestAsyncQueryInput input)
    {
        throw new InvalidOperationException("Test exception");
    }
}
