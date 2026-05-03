// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Query;

namespace GFramework.Core.Tests.Query;

/// <summary>
///     字符串类型测试异步查询类V4，继承AbstractAsyncQuery
/// </summary>
public sealed class TestAsyncStringQueryV4 : AbstractAsyncQuery<TestAsyncQueryInputV2, string>
{
    /// <summary>
    ///     初始化TestAsyncStringQueryV4的新实例
    /// </summary>
    /// <param name="input">查询输入参数</param>
    public TestAsyncStringQueryV4(TestAsyncQueryInputV2 input) : base(input)
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
    /// <returns>格式化的字符串结果</returns>
    protected override Task<string> OnDoAsync(TestAsyncQueryInputV2 input)
    {
        Executed = true;
        return Task.FromResult($"Value: {input.Value * 2}");
    }
}
