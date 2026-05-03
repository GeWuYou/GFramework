// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Query;

namespace GFramework.Core.Tests.Query;

/// <summary>
///     为 <see cref="QueryExecutorTests" /> 提供字符串结果的测试同步查询。
/// </summary>
public sealed class TestStringQuery : AbstractQuery<TestQueryInput, string>
{
    /// <summary>
    ///     初始化 <see cref="TestStringQuery" /> 的新实例。
    /// </summary>
    /// <param name="input">查询输入参数。</param>
    public TestStringQuery(TestQueryInput input) : base(input)
    {
    }

    /// <summary>
    ///     执行同步查询并返回格式化后的字符串结果。
    /// </summary>
    /// <param name="input">查询输入参数。</param>
    /// <returns>包含双倍值的格式化字符串。</returns>
    protected override string OnDo(TestQueryInput input)
    {
        return $"Result: {input.Value * 2}";
    }
}
