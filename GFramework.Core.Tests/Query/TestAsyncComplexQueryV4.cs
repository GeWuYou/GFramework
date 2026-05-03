// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Query;

namespace GFramework.Core.Tests.Query;

/// <summary>
///     复杂对象类型测试异步查询类V4，继承AbstractAsyncQuery
/// </summary>
public sealed class TestAsyncComplexQueryV4 : AbstractAsyncQuery<TestAsyncQueryInputV2, TestAsyncQueryResultV2>
{
    /// <summary>
    ///     初始化TestAsyncComplexQueryV4的新实例
    /// </summary>
    /// <param name="input">查询输入参数</param>
    public TestAsyncComplexQueryV4(TestAsyncQueryInputV2 input) : base(input)
    {
    }

    /// <summary>
    ///     获取查询是否已执行
    /// </summary>
    public bool Executed { get; private set; }

    /// <summary>
    ///     执行异步查询操作的具体实现
    /// </summary>
    /// <param name="input">查询输入参数</param>
    /// <returns>复杂对象查询结果</returns>
    protected override Task<TestAsyncQueryResultV2> OnDoAsync(TestAsyncQueryInputV2 input)
    {
        Executed = true;
        var result = new TestAsyncQueryResultV2
        {
            Value = input.Value * 2,
            TripleValue = input.Value * 3
        };
        return Task.FromResult(result);
    }
}
