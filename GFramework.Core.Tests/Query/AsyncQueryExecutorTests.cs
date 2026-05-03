// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Query;

namespace GFramework.Core.Tests.Query;

/// <summary>
///     AsyncQueryBus类的单元测试
///     测试内容包括：
///     - SendAsync方法 - 正常查询发送
///     - SendAsync方法 - 空查询异常
///     - 异步查询结果正确性
///     - 不同返回类型的异步查询支持
///     - 异步查询的异常处理
///     - 异步查询的上下文传递
/// </summary>
[TestFixture]
public class AsyncQueryExecutorTests
{
    [SetUp]
    public void SetUp()
    {
        _asyncQueryExecutor = new AsyncQueryExecutor();
    }

    private AsyncQueryExecutor _asyncQueryExecutor = null!;

    /// <summary>
    ///     测试SendAsync方法正确返回查询结果
    /// </summary>
    [Test]
    public async Task SendAsync_Should_Return_Query_Result()
    {
        var input = new TestAsyncQueryInput { Value = 10 };
        var query = new TestAsyncQuery(input);

        var result = await _asyncQueryExecutor.SendAsync(query);

        Assert.That(result, Is.EqualTo(20));
    }

    /// <summary>
    ///     测试SendAsync方法在传入空查询对象时是否会抛出ArgumentNullException异常
    /// </summary>
    [Test]
    public void SendAsync_WithNullQuery_Should_ThrowArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(() => _asyncQueryExecutor.SendAsync<int>(null!));
    }

    /// <summary>
    ///     测试SendAsync方法是否能正确返回字符串类型的查询结果
    /// </summary>
    [Test]
    public async Task SendAsync_WithStringResult_Should_Return_String()
    {
        var input = new TestAsyncQueryInput { Value = 5 };
        var query = new TestAsyncStringQuery(input);

        var result = await _asyncQueryExecutor.SendAsync(query);

        Assert.That(result, Is.EqualTo("Result: 10"));
    }

    /// <summary>
    ///     测试SendAsync方法是否能正确返回布尔类型的查询结果
    /// </summary>
    [Test]
    public async Task SendAsync_WithBooleanResult_Should_Return_Boolean()
    {
        var input = new TestAsyncQueryInput { Value = 42 };
        var query = new TestAsyncBooleanQuery(input);

        var result = await _asyncQueryExecutor.SendAsync(query);

        Assert.That(result, Is.True);
    }

    /// <summary>
    ///     测试SendAsync方法是否能正确处理复杂对象的查询结果
    /// </summary>
    [Test]
    public async Task SendAsync_WithComplexObjectResult_Should_Return_ComplexObject()
    {
        var input = new TestAsyncQueryInput { Value = 100 };
        var query = new TestAsyncComplexQuery(input);

        var result = await _asyncQueryExecutor.SendAsync(query);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Value, Is.EqualTo(200));
        Assert.That(result.TripleValue, Is.EqualTo(300));
    }

    /// <summary>
    ///     测试SendAsync方法是否能正确处理抛出异常的查询
    /// </summary>
    [Test]
    public void SendAsync_Should_Propagate_Exception_From_Query()
    {
        var input = new TestAsyncQueryInput { Value = 0 };
        var query = new TestAsyncQueryWithException(input);

        Assert.ThrowsAsync<InvalidOperationException>(() => _asyncQueryExecutor.SendAsync(query));
    }

    /// <summary>
    ///     测试SendAsync方法多次调用
    /// </summary>
    [Test]
    public async Task SendAsync_Should_Be_Callable_Multiple_Times()
    {
        var input = new TestAsyncQueryInput { Value = 10 };
        var query = new TestAsyncQuery(input);

        var result1 = await _asyncQueryExecutor.SendAsync(query);
        var result2 = await _asyncQueryExecutor.SendAsync(query);

        Assert.That(result1, Is.EqualTo(20));
        Assert.That(result2, Is.EqualTo(20));
    }

    /// <summary>
    ///     测试SendAsync方法在不同查询之间保持独立性
    /// </summary>
    [Test]
    public async Task SendAsync_Should_Maintain_Independence_Between_Different_Queries()
    {
        var input1 = new TestAsyncQueryInput { Value = 10 };
        var input2 = new TestAsyncQueryInput { Value = 20 };
        var query1 = new TestAsyncQuery(input1);
        var query2 = new TestAsyncQuery(input2);

        var result1 = await _asyncQueryExecutor.SendAsync(query1);
        var result2 = await _asyncQueryExecutor.SendAsync(query2);

        Assert.That(result1, Is.EqualTo(20));
        Assert.That(result2, Is.EqualTo(40));
    }
}
