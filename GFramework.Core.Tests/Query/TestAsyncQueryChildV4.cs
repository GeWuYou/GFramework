// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Query;

namespace GFramework.Core.Tests.Query;

/// <summary>
///     测试用异步查询子类V4，继承AbstractAsyncQuery
/// </summary>
public sealed class TestAsyncQueryChildV4 : AbstractAsyncQuery<TestAsyncQueryInputV2, int>
{
    /// <summary>
    ///     初始化TestAsyncQueryChildV4的新实例
    /// </summary>
    /// <param name="input">查询输入参数</param>
    public TestAsyncQueryChildV4(TestAsyncQueryInputV2 input) : base(input)
    {
    }

    /// <summary>
    ///     获取查询是否已执行
    /// </summary>
    public bool Executed { get; private set; }

    /// <summary>
    ///     执行异步查询操作的具体实现（子类实现，乘以3）
    /// </summary>
    /// <param name="input">查询输入参数</param>
    /// <returns>查询结果，将输入值乘以3</returns>
    protected override Task<int> OnDoAsync(TestAsyncQueryInputV2 input)
    {
        Executed = true;
        return Task.FromResult(input.Value * 3);
    }
}
