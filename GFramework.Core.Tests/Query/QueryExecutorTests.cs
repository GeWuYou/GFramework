// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Query;

namespace GFramework.Core.Tests.Query;

/// <summary>
///     查询总线测试类，用于测试QueryBus的功能和异常处理
/// </summary>
[TestFixture]
public class QueryExecutorTests
{
    /// <summary>
    ///     测试设置方法，在每个测试方法执行前初始化查询总线实例
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        _queryExecutor = new QueryExecutor();
    }

    private QueryExecutor _queryExecutor = null!;

    /// <summary>
    ///     测试Send方法是否能正确返回查询结果
    ///     验证当传入有效查询对象时，能够得到预期的计算结果
    /// </summary>
    [Test]
    public void Send_Should_Return_Query_Result()
    {
        var input = new TestQueryInput { Value = 10 };
        var query = new TestQuery(input);

        var result = _queryExecutor.Send(query);

        Assert.That(result, Is.EqualTo(20));
    }

    /// <summary>
    ///     测试Send方法在传入空查询对象时是否会抛出ArgumentNullException异常
    ///     验证参数验证功能的正确性
    /// </summary>
    [Test]
    public void Send_WithNullQuery_Should_ThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _queryExecutor.Send<int>(null!));
    }

    /// <summary>
    ///     测试Send方法是否能正确返回字符串类型的查询结果
    ///     验证不同返回类型的支持情况
    /// </summary>
    [Test]
    public void Send_WithStringResult_Should_Return_String()
    {
        var input = new TestQueryInput { Value = 5 };
        var query = new TestStringQuery(input);

        var result = _queryExecutor.Send(query);

        Assert.That(result, Is.EqualTo("Result: 10"));
    }
}
