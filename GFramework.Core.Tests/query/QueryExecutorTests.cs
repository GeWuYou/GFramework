using GFramework.Core.Abstractions.cqrs.query;
using GFramework.Core.query;
using NUnit.Framework;

namespace GFramework.Core.Tests.query;

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

/// <summary>
///     测试用查询输入类，实现IQueryInput接口
///     用于传递查询所需的参数信息
/// </summary>
public sealed class TestQueryInput : IQueryInput
{
    /// <summary>
    ///     获取或设置查询值
    /// </summary>
    public int Value { get; init; }
}

/// <summary>
///     整数类型测试查询类，继承自AbstractQuery
///     实现具体的查询逻辑并返回整数结果
/// </summary>
public sealed class TestQuery : AbstractQuery<TestQueryInput, int>
{
    /// <summary>
    ///     初始化TestQuery的新实例
    /// </summary>
    /// <param name="input">查询输入参数</param>
    public TestQuery(TestQueryInput input) : base(input)
    {
    }

    /// <summary>
    ///     执行查询操作的具体实现
    /// </summary>
    /// <param name="input">查询输入参数</param>
    /// <returns>查询结果，将输入值乘以2</returns>
    protected override int OnDo(TestQueryInput input)
    {
        return input.Value * 2;
    }
}

/// <summary>
///     字符串类型测试查询类，继承自AbstractQuery
///     实现具体的查询逻辑并返回字符串结果
/// </summary>
public sealed class TestStringQuery : AbstractQuery<TestQueryInput, string>
{
    /// <summary>
    ///     初始化TestStringQuery的新实例
    /// </summary>
    /// <param name="input">查询输入参数</param>
    public TestStringQuery(TestQueryInput input) : base(input)
    {
    }

    /// <summary>
    ///     执行查询操作的具体实现
    /// </summary>
    /// <param name="input">查询输入参数</param>
    /// <returns>格式化的字符串结果</returns>
    protected override string OnDo(TestQueryInput input)
    {
        return $"Result: {input.Value * 2}";
    }
}