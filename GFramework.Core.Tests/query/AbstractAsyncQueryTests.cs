using GFramework.Core.Abstractions.query;
using GFramework.Core.Abstractions.rule;
using GFramework.Core.architecture;
using GFramework.Core.command;
using GFramework.Core.environment;
using GFramework.Core.events;
using GFramework.Core.ioc;
using GFramework.Core.query;
using NUnit.Framework;

namespace GFramework.Core.Tests.query;

/// <summary>
///     AbstractAsyncQuery类的单元测试
///     测试内容包括：
///     - 异步查询的基础实现
///     - DoAsync方法调用
///     - DoAsync方法的异常处理
///     - 上下文感知功能（SetContext, GetContext）
///     - 日志功能（Logger属性）
///     - 子类继承行为验证
///     - 查询执行前日志记录
///     - 查询执行后日志记录
///     - 返回值类型验证
///     - 错误情况下的日志记录
/// </summary>
[TestFixture]
public class AbstractAsyncQueryTests
{
    [SetUp]
    public void SetUp()
    {
        _container = new IocContainer();
        _container.RegisterPlurality(new EventBus());
        _container.RegisterPlurality(new CommandBus());
        _container.RegisterPlurality(new QueryBus());
        _container.RegisterPlurality(new DefaultEnvironment());
        _container.RegisterPlurality(new AsyncQueryBus());
        _context = new ArchitectureContext(_container);
    }

    private ArchitectureContext _context = null!;
    private IocContainer _container = null!;

    /// <summary>
    ///     测试异步查询的基础实现
    /// </summary>
    [Test]
    public async Task AbstractAsyncQuery_Should_Implement_IAsyncQuery_Interface()
    {
        var input = new TestAsyncQueryInputV2();
        var query = new TestAsyncQueryV4(input);

        Assert.That(query, Is.InstanceOf<IAsyncQuery<int>>());
    }

    /// <summary>
    ///     测试DoAsync方法调用
    /// </summary>
    [Test]
    public async Task DoAsync_Should_Invoke_OnDoAsync_Method()
    {
        var input = new TestAsyncQueryInputV2 { Value = 42 };
        var query = new TestAsyncQueryV4(input);
        var asyncQuery = (IAsyncQuery<int>)query;

        var result = await asyncQuery.DoAsync();

        Assert.That(query.Executed, Is.True);
        Assert.That(result, Is.EqualTo(84));
    }

    /// <summary>
    ///     测试DoAsync方法的异常处理
    /// </summary>
    [Test]
    public void DoAsync_Should_Propagate_Exception_From_OnDoAsync()
    {
        var input = new TestAsyncQueryInputV2();
        var query = new TestAsyncQueryWithExceptionV4(input);
        var asyncQuery = (IAsyncQuery<int>)query;

        Assert.ThrowsAsync<InvalidOperationException>(async () => await asyncQuery.DoAsync());
    }

    /// <summary>
    ///     测试上下文感知功能 - SetContext方法
    /// </summary>
    [Test]
    public void SetContext_Should_Set_Context_Property()
    {
        var input = new TestAsyncQueryInputV2();
        var query = new TestAsyncQueryV4(input);
        var contextAware = (IContextAware)query;

        contextAware.SetContext(_context);

        var context = contextAware.GetContext();
        Assert.That(context, Is.SameAs(_context));
    }

    /// <summary>
    ///     测试上下文感知功能 - GetContext方法
    /// </summary>
    [Test]
    public void GetContext_Should_Return_Context_Property()
    {
        var input = new TestAsyncQueryInputV2();
        var query = new TestAsyncQueryV4(input);
        var contextAware = (IContextAware)query;

        contextAware.SetContext(_context);

        var context = contextAware.GetContext();
        Assert.That(context, Is.Not.Null);
        Assert.That(context, Is.SameAs(_context));
    }

    /// <summary>
    ///     测试子类继承行为验证
    /// </summary>
    [Test]
    public async Task Child_Class_Should_Inherit_And_Override_OnDoAsync_Method()
    {
        var input = new TestAsyncQueryInputV2 { Value = 100 };
        var query = new TestAsyncQueryChildV4(input);
        var asyncQuery = (IAsyncQuery<int>)query;

        var result = await asyncQuery.DoAsync();

        Assert.That(query.Executed, Is.True);
        Assert.That(result, Is.EqualTo(300));
    }

    /// <summary>
    ///     测试异步查询执行生命周期完整性
    /// </summary>
    [Test]
    public async Task AsyncQuery_Should_Complete_Execution_Lifecycle()
    {
        var input = new TestAsyncQueryInputV2 { Value = 42 };
        var query = new TestAsyncQueryV4(input);
        var asyncQuery = (IAsyncQuery<int>)query;

        Assert.That(query.Executed, Is.False, "Query should not be executed before DoAsync");

        var result = await asyncQuery.DoAsync();

        Assert.That(query.Executed, Is.True, "Query should be executed after DoAsync");
        Assert.That(result, Is.EqualTo(84), "Query should have correct result");
    }

    /// <summary>
    ///     测试异步查询多次执行
    /// </summary>
    [Test]
    public async Task AsyncQuery_Should_Be_Executable_Multiple_Times()
    {
        var input = new TestAsyncQueryInputV2 { Value = 10 };
        var query = new TestAsyncQueryV4(input);
        var asyncQuery = (IAsyncQuery<int>)query;

        var result1 = await asyncQuery.DoAsync();
        var result2 = await asyncQuery.DoAsync();

        Assert.That(result1, Is.EqualTo(20), "First execution should have result 20");
        Assert.That(result2, Is.EqualTo(20), "Second execution should have result 20");
    }

    /// <summary>
    ///     测试异步查询的返回值类型
    /// </summary>
    [Test]
    public async Task AsyncQuery_Should_Return_Correct_Type()
    {
        var input = new TestAsyncQueryInputV2 { Value = 100 };
        var query = new TestAsyncQueryV4(input);
        var asyncQuery = (IAsyncQuery<int>)query;

        var result = await asyncQuery.DoAsync();

        Assert.That(result, Is.InstanceOf<int>());
        Assert.That(result, Is.EqualTo(200));
    }

    /// <summary>
    ///     测试异步查询的字符串返回值
    /// </summary>
    [Test]
    public async Task AsyncQuery_WithStringResult_Should_Return_String()
    {
        var input = new TestAsyncQueryInputV2 { Value = 5 };
        var query = new TestAsyncStringQueryV4(input);
        var asyncQuery = (IAsyncQuery<string>)query;

        var result = await asyncQuery.DoAsync();

        Assert.That(result, Is.InstanceOf<string>());
        Assert.That(result, Is.EqualTo("Value: 10"));
    }

    /// <summary>
    ///     测试异步查询的复杂对象返回值
    /// </summary>
    [Test]
    public async Task AsyncQuery_WithComplexResult_Should_Return_ComplexObject()
    {
        var input = new TestAsyncQueryInputV2 { Value = 10 };
        var query = new TestAsyncComplexQueryV4(input);
        var asyncQuery = (IAsyncQuery<TestAsyncQueryResultV2>)query;

        var result = await asyncQuery.DoAsync();

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Value, Is.EqualTo(20));
        Assert.That(result.DoubleValue, Is.EqualTo(30));
    }

    /// <summary>
    ///     测试异步查询在不同实例之间的独立性
    /// </summary>
    [Test]
    public async Task AsyncQuery_Should_Maintain_Independence_Between_Different_Instances()
    {
        var input1 = new TestAsyncQueryInputV2 { Value = 10 };
        var input2 = new TestAsyncQueryInputV2 { Value = 20 };
        var query1 = new TestAsyncQueryV4(input1);
        var query2 = new TestAsyncQueryV4(input2);
        var asyncQuery1 = (IAsyncQuery<int>)query1;
        var asyncQuery2 = (IAsyncQuery<int>)query2;

        var result1 = await asyncQuery1.DoAsync();
        var result2 = await asyncQuery2.DoAsync();

        Assert.That(result1, Is.EqualTo(20));
        Assert.That(result2, Is.EqualTo(40));
    }
}

/// <summary>
///     测试用异步查询输入类V2
/// </summary>
public sealed class TestAsyncQueryInputV2 : IQueryInput
{
    /// <summary>
    ///     获取或设置值
    /// </summary>
    public int Value { get; init; }
}

/// <summary>
///     整数类型测试异步查询类V4，继承AbstractAsyncQuery
/// </summary>
public sealed class TestAsyncQueryV4 : AbstractAsyncQuery<TestAsyncQueryInputV2, int>
{
    /// <summary>
    ///     初始化TestAsyncQueryV4的新实例
    /// </summary>
    /// <param name="input">查询输入参数</param>
    public TestAsyncQueryV4(TestAsyncQueryInputV2 input) : base(input)
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
    /// <returns>查询结果，将输入值乘以2</returns>
    protected override Task<int> OnDoAsync(TestAsyncQueryInputV2 input)
    {
        Executed = true;
        return Task.FromResult(input.Value * 2);
    }
}

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
            DoubleValue = input.Value * 3
        };
        return Task.FromResult(result);
    }
}

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
    /// <exception cref="InvalidOperationException">总是抛出异常</exception>
    protected override Task<int> OnDoAsync(TestAsyncQueryInputV2 input)
    {
        throw new InvalidOperationException("Test exception");
    }
}

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

/// <summary>
///     测试用复杂查询结果类V2
/// </summary>
public sealed class TestAsyncQueryResultV2
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