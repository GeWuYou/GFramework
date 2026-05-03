// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Query;

namespace GFramework.Core.Tests.Query;

/// <summary>
///     测试用异步查询类（抛出异常）
/// </summary>
public sealed class TestAsyncQueryWithExceptionV4 : AbstractAsyncQuery<TestAsyncQueryInputV2, int>
{
    /// <summary>
    ///     初始化TestAsyncQueryWithExceptionV4的新实例
    /// </summary>
    /// <param name="input">查询输入参数</param>
    public TestAsyncQueryWithExceptionV4(TestAsyncQueryInputV2 input) : base(input)
    {
    }

    /// <summary>
    ///     执行异步查询操作并抛出异常
    /// </summary>
    /// <param name="input">查询输入参数</param>
    /// <returns>返回一个不会正常完成的 <see cref="Task{TResult}" />，因为该方法始终抛出异常。</returns>
    /// <exception cref="InvalidOperationException">总是抛出异常</exception>
    protected override Task<int> OnDoAsync(TestAsyncQueryInputV2 input)
    {
        throw new InvalidOperationException("Test exception");
    }
}
