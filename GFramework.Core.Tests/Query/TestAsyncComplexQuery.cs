// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Query;

namespace GFramework.Core.Tests.Query;

/// <summary>
///     为 <see cref="AsyncQueryExecutorTests" /> 提供复杂对象结果的测试异步查询。
/// </summary>
public sealed class TestAsyncComplexQuery : AbstractAsyncQuery<TestAsyncQueryInput, TestAsyncQueryResult>
{
    /// <summary>
    ///     初始化 <see cref="TestAsyncComplexQuery" /> 的新实例。
    /// </summary>
    /// <param name="input">查询输入参数。</param>
    public TestAsyncComplexQuery(TestAsyncQueryInput input) : base(input)
    {
    }

    /// <summary>
    ///     执行异步查询并构造组合结果对象。
    /// </summary>
    /// <param name="input">查询输入参数。</param>
    /// <returns>包含双倍值和三倍值的测试结果对象。</returns>
    protected override Task<TestAsyncQueryResult> OnDoAsync(TestAsyncQueryInput input)
    {
        var result = new TestAsyncQueryResult
        {
            Value = input.Value * 2,
            TripleValue = input.Value * 3
        };

        return Task.FromResult(result);
    }
}
