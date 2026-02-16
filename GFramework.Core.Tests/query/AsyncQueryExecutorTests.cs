using GFramework.Core.Abstractions.cqrs.query;
using GFramework.Core.query;
using NUnit.Framework;

namespace GFramework.Core.Tests.query;

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
        Assert.ThrowsAsync<ArgumentNullException>(async () => await _asyncQueryExecutor.SendAsync<int>(null!));
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
        Assert.That(result.DoubleValue, Is.EqualTo(300));
    }

    /// <summary>
    ///     测试SendAsync方法是否能正确处理抛出异常的查询
    /// </summary>
    [Test]
    public void SendAsync_Should_Propagate_Exception_From_Query()
    {
        var input = new TestAsyncQueryInput { Value = 0 };
        var query = new TestAsyncQueryWithException(input);

        Assert.ThrowsAsync<InvalidOperationException>(async () => await _asyncQueryExecutor.SendAsync(query));
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

/// <summary>
///     测试用异步查询输入类，实现IQueryInput接口
/// </summary>
public sealed class TestAsyncQueryInput : IQueryInput
{
    /// <summary>
    ///     获取或设置查询值
    /// </summary>
    public int Value { get; init; }
}

/// <summary>
///     整数类型测试异步查询类，继承AbstractAsyncQuery
///     实现具体的异步查询逻辑并返回整数结果
/// </summary>
public sealed class TestAsyncQuery : AbstractAsyncQuery<TestAsyncQueryInput, int>
{
    /// <summary>
    ///     初始化TestAsyncQuery的新实例
    /// </summary>
    /// <param name="input">查询输入参数</param>
    public TestAsyncQuery(TestAsyncQueryInput input) : base(input)
    {
    }

    /// <summary>
    ///     执行异步查询操作的具体实现
    /// </summary>
    /// <param name="input">查询输入参数</param>
    /// <returns>查询结果，将输入值乘以2</returns>
    protected override Task<int> OnDoAsync(TestAsyncQueryInput input)
    {
        return Task.FromResult(input.Value * 2);
    }
}

/// <summary>
///     字符串类型测试异步查询类，继承AbstractAsyncQuery
///     实现具体的异步查询逻辑并返回字符串结果
/// </summary>
public sealed class TestAsyncStringQuery : AbstractAsyncQuery<TestAsyncQueryInput, string>
{
    /// <summary>
    ///     初始化TestAsyncStringQuery的新实例
    /// </summary>
    /// <param name="input">查询输入参数</param>
    public TestAsyncStringQuery(TestAsyncQueryInput input) : base(input)
    {
    }

    /// <summary>
    ///     执行异步查询操作的具体实现
    /// </summary>
    /// <param name="input">查询输入参数</param>
    /// <returns>格式化的字符串结果</returns>
    protected override Task<string> OnDoAsync(TestAsyncQueryInput input)
    {
        return Task.FromResult($"Result: {input.Value * 2}");
    }
}

/// <summary>
///     布尔类型测试异步查询类，继承AbstractAsyncQuery
///     实现具体的异步查询逻辑并返回布尔结果
/// </summary>
public sealed class TestAsyncBooleanQuery : AbstractAsyncQuery<TestAsyncQueryInput, bool>
{
    /// <summary>
    ///     初始化TestAsyncBooleanQuery的新实例
    /// </summary>
    /// <param name="input">查询输入参数</param>
    public TestAsyncBooleanQuery(TestAsyncQueryInput input) : base(input)
    {
    }

    /// <summary>
    ///     执行异步查询操作的具体实现
    /// </summary>
    /// <param name="input">查询输入参数</param>
    /// <returns>如果值大于0返回true，否则返回false</returns>
    protected override Task<bool> OnDoAsync(TestAsyncQueryInput input)
    {
        return Task.FromResult(input.Value > 0);
    }
}

/// <summary>
///     复杂对象类型测试异步查询类，继承AbstractAsyncQuery
///     实现具体的异步查询逻辑并返回复杂对象结果
/// </summary>
public sealed class TestAsyncComplexQuery : AbstractAsyncQuery<TestAsyncQueryInput, TestAsyncQueryResult>
{
    /// <summary>
    ///     初始化TestAsyncComplexQuery的新实例
    /// </summary>
    /// <param name="input">查询输入参数</param>
    public TestAsyncComplexQuery(TestAsyncQueryInput input) : base(input)
    {
    }

    /// <summary>
    ///     执行异步查询操作的具体实现
    /// </summary>
    /// <param name="input">查询输入参数</param>
    /// <returns>复杂对象查询结果</returns>
    protected override Task<TestAsyncQueryResult> OnDoAsync(TestAsyncQueryInput input)
    {
        var result = new TestAsyncQueryResult
        {
            Value = input.Value * 2,
            DoubleValue = input.Value * 3
        };
        return Task.FromResult(result);
    }
}

/// <summary>
///     测试用异步查询类（抛出异常）
/// </summary>
public sealed class TestAsyncQueryWithException : AbstractAsyncQuery<TestAsyncQueryInput, int>
{
    /// <summary>
    ///     初始化TestAsyncQueryWithException的新实例
    /// </summary>
    /// <param name="input">查询输入参数</param>
    public TestAsyncQueryWithException(TestAsyncQueryInput input) : base(input)
    {
    }

    /// <summary>
    ///     执行异步查询操作并抛出异常
    /// </summary>
    /// <param name="input">查询输入参数</param>
    /// <exception cref="InvalidOperationException">总是抛出异常</exception>
    protected override Task<int> OnDoAsync(TestAsyncQueryInput input)
    {
        throw new InvalidOperationException("Test exception");
    }
}

/// <summary>
///     测试用复杂查询结果类
/// </summary>
public sealed class TestAsyncQueryResult
{
    /// <summary>
    ///     获取或设置值
    /// </summary>
    public int Value { get; init; }

    /// <summary>
    ///     获取或设置双倍值
    /// </summary>
    public int DoubleValue { get; init; }
}