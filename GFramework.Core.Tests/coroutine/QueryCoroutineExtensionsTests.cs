using GFramework.Core.Abstractions.architecture;
using GFramework.Core.Abstractions.coroutine;
using GFramework.Core.Abstractions.query;
using GFramework.Core.Abstractions.rule;
using GFramework.Core.coroutine.extensions;
using Moq;
using NUnit.Framework;

namespace GFramework.Core.Tests.coroutine;

/// <summary>
///     QueryCoroutineExtensions的单元测试类
///     测试内容包括：
///     - SendQueryCoroutine扩展方法
/// </summary>
[TestFixture]
public class QueryCoroutineExtensionsTests
{
    /// <summary>
    ///     测试用的简单查询类
    /// </summary>
    private class TestQuery : IQuery<string>
    {
        private IArchitectureContext? _context;
        public string QueryData { get; set; } = string.Empty;
        public void SetContext(IArchitectureContext context) => _context = context;
        public IArchitectureContext GetContext() => _context!;
        public string Do() => QueryData;
    }

    /// <summary>
    ///     上下文感知基类的模拟实现
    /// </summary>
    private class TestContextAware : IContextAware
    {
        public Mock<IArchitectureContext> _mockContext = new Mock<IArchitectureContext>();

        public IArchitectureContext GetContext()
        {
            return _mockContext.Object;
        }

        public void SetContext(IArchitectureContext context)
        {
        }
    }

    /// <summary>
    ///     验证SendQueryCoroutine应该能正常执行查询并返回结果
    /// </summary>
    [Test]
    public void SendQueryCoroutine_Should_Execute_Query_And_Return_Result()
    {
        var query = new TestQuery { QueryData = "TestQueryData" };
        string? receivedResult = null;
        var resultReceived = false;
        var contextAware = new TestContextAware();

        // 设置上下文发送查询的模拟行为
        contextAware._mockContext
            .Setup(ctx => ctx.SendQuery<string>(It.IsAny<TestQuery>()))
            .Returns("TestResult");

        var coroutine = contextAware.SendQueryCoroutine<TestQuery, string>(query, result =>
        {
            receivedResult = result;
            resultReceived = true;
        });

        // 迭代协程直到完成
        var moved = coroutine.MoveNext();

        // SendQueryCoroutine立即执行并返回，所以MoveNext应该返回false
        Assert.That(moved, Is.False);
        Assert.That(resultReceived, Is.True);
        Assert.That(receivedResult, Is.EqualTo("TestResult"));
    }

    /// <summary>
    ///     验证SendQueryCoroutine应该处理不同类型的查询和结果
    /// </summary>
    [Test]
    public void SendQueryCoroutine_Should_Handle_Different_Query_And_Result_Types()
    {
        // 使用整数查询和布尔结果
        var query = new IntQuery { Value = 42 };
        bool? receivedResult = null;
        var resultReceived = false;
        var contextAware = new TestContextAware();

        // 设置上下文发送查询的模拟行为
        contextAware._mockContext
            .Setup(ctx => ctx.SendQuery<bool>(It.IsAny<IntQuery>()))
            .Returns(true);

        var coroutine = contextAware.SendQueryCoroutine<IntQuery, bool>(query, result =>
        {
            receivedResult = result;
            resultReceived = true;
        });

        // 迭代协程直到完成
        var moved = coroutine.MoveNext();

        Assert.That(moved, Is.False);
        Assert.That(resultReceived, Is.True);
        Assert.That(receivedResult, Is.True);
    }

    /// <summary>
    ///     验证SendQueryCoroutine在null回调时应该抛出异常
    /// </summary>
    [Test]
    public void SendQueryCoroutine_Should_Throw_When_Null_Result_Callback()
    {
        var query = new TestQuery { QueryData = "TestQueryData" };
        var contextAware = new TestContextAware();

        // 设置上下文发送查询的模拟行为
        contextAware._mockContext
            .Setup(ctx => ctx.SendQuery<string>(It.IsAny<TestQuery>()))
            .Returns("TestResult");

        // 使用null作为结果回调，应该抛出NullReferenceException
        var coroutine = contextAware.SendQueryCoroutine<TestQuery, string>(query, null!);

        // 迭代协程时应该抛出异常
        Assert.That(() => coroutine.MoveNext(), Throws.TypeOf<NullReferenceException>());
    }

    /// <summary>
    ///     验证SendQueryCoroutine应该在查询执行期间调用结果处理回调
    /// </summary>
    [Test]
    public void SendQueryCoroutine_Should_Call_Result_Callback_During_Execution()
    {
        var query = new TestQuery { QueryData = "TestQueryData" };
        string? receivedResult = null;
        var callCount = 0;
        var contextAware = new TestContextAware();

        // 设置上下文发送查询的模拟行为
        contextAware._mockContext
            .Setup(ctx => ctx.SendQuery<string>(It.IsAny<TestQuery>()))
            .Returns("ProcessedResult");

        var coroutine = contextAware.SendQueryCoroutine<TestQuery, string>(query, result =>
        {
            receivedResult = result;
            callCount++;
        });

        // 协程应立即执行查询并调用回调
        coroutine.MoveNext();

        Assert.That(callCount, Is.EqualTo(1));
        Assert.That(receivedResult, Is.EqualTo("ProcessedResult"));
    }

    /// <summary>
    ///     验证SendQueryCoroutine应该返回IEnumerator<IYieldInstruction>
    /// </summary>
    [Test]
    public void SendQueryCoroutine_Should_Return_IEnumerator_Of_YieldInstruction()
    {
        var query = new TestQuery { QueryData = "TestQueryData" };
        var contextAware = new TestContextAware();

        // 设置上下文发送查询的模拟行为
        contextAware._mockContext
            .Setup(ctx => ctx.SendQuery<string>(It.IsAny<TestQuery>()))
            .Returns("TestResult");

        var coroutine = contextAware.SendQueryCoroutine<TestQuery, string>(query, result => { });

        Assert.That(coroutine, Is.InstanceOf<IEnumerator<IYieldInstruction>>());
    }

    /// <summary>
    ///     验证SendQueryCoroutine应该在查询抛出异常时处理异常
    /// </summary>
    [Test]
    public void SendQueryCoroutine_Should_Handle_Query_Exception()
    {
        var query = new TestQuery { QueryData = "TestQueryData" };
        string? receivedResult = null;
        var contextAware = new TestContextAware();
        var expectedException = new InvalidOperationException("Query execution failed");

        // 设置上下文发送查询的模拟行为，让它抛出异常
        contextAware._mockContext
            .Setup(ctx => ctx.SendQuery<string>(It.IsAny<TestQuery>()))
            .Throws(expectedException);

        // 由于SendQueryCoroutine会直接执行查询，这可能导致异常
        Assert.Throws<InvalidOperationException>(() =>
        {
            var coroutine =
                contextAware.SendQueryCoroutine<TestQuery, string>(query, result => { receivedResult = result; });

            // 尝试移动协程，这应该会执行查询并抛出异常
            coroutine.MoveNext();
        });
    }

    /// <summary>
    ///     验证SendQueryCoroutine应该正确传递查询参数
    /// </summary>
    [Test]
    public void SendQueryCoroutine_Should_Pass_Query_Parameters_Correctly()
    {
        var query = new TestQuery { QueryData = "PassedQueryData" };
        string? receivedResult = null;
        var contextAware = new TestContextAware();
        TestQuery? capturedQuery = null;

        // 设置上下文发送查询的模拟行为，并捕获传入的查询参数
        contextAware._mockContext
            .Setup(ctx => ctx.SendQuery<string>(It.IsAny<TestQuery>()))
            .Returns((IQuery<string> q) =>
            {
                capturedQuery = (TestQuery)q;
                return $"Processed_{capturedQuery.QueryData}";
            });

        var coroutine =
            contextAware.SendQueryCoroutine<TestQuery, string>(query, result => { receivedResult = result; });

        coroutine.MoveNext();

        Assert.That(capturedQuery, Is.Not.Null);
        Assert.That(capturedQuery!.QueryData, Is.EqualTo("PassedQueryData"));
        Assert.That(receivedResult, Is.EqualTo("Processed_PassedQueryData"));
    }

    /// <summary>
    ///     验证SendQueryCoroutine应该处理复杂对象查询
    /// </summary>
    [Test]
    public void SendQueryCoroutine_Should_Handle_Complex_Object_Query()
    {
        var query = new ComplexQuery
        {
            Name = "ComplexName",
            Values = new List<int> { 1, 2, 3 },
            Metadata = new Dictionary<string, object> { { "key", "value" } }
        };

        ComplexResult? receivedResult = null;
        var contextAware = new TestContextAware();

        var expectedResult = new ComplexResult
        {
            ProcessedName = "Processed_ComplexName",
            Sum = 6,
            Count = 3
        };

        // 设置上下文发送查询的模拟行为
        contextAware._mockContext
            .Setup(ctx => ctx.SendQuery<ComplexResult>(It.IsAny<ComplexQuery>()))
            .Returns(expectedResult);

        var coroutine =
            contextAware.SendQueryCoroutine<ComplexQuery, ComplexResult>(query, result => { receivedResult = result; });

        coroutine.MoveNext();

        Assert.That(receivedResult, Is.Not.Null);
        Assert.That(receivedResult!.ProcessedName, Is.EqualTo("Processed_ComplexName"));
        Assert.That(receivedResult.Sum, Is.EqualTo(6));
        Assert.That(receivedResult.Count, Is.EqualTo(3));
    }

    /// <summary>
    ///     验证SendQueryCoroutine应该处理空字符串结果
    /// </summary>
    [Test]
    public void SendQueryCoroutine_Should_Handle_Empty_String_Result()
    {
        var query = new TestQuery { QueryData = "TestQueryData" };
        string? receivedResult = null;
        var contextAware = new TestContextAware();

        // 设置上下文发送查询的模拟行为，返回空字符串
        contextAware._mockContext
            .Setup(ctx => ctx.SendQuery<string>(It.IsAny<TestQuery>()))
            .Returns(string.Empty);

        var coroutine =
            contextAware.SendQueryCoroutine<TestQuery, string>(query, result => { receivedResult = result; });

        coroutine.MoveNext();

        Assert.That(receivedResult, Is.EqualTo(string.Empty));
    }

    /// <summary>
    ///     验证SendQueryCoroutine应该处理null结果
    /// </summary>
    [Test]
    public void SendQueryCoroutine_Should_Handle_Null_Result()
    {
        var query = new TestQuery { QueryData = "TestQueryData" };
        string? receivedResult = "initial";
        var contextAware = new TestContextAware();

        // 设置上下文发送查询的模拟行为，返回null
        contextAware._mockContext
            .Setup(ctx => ctx.SendQuery<string>(It.IsAny<TestQuery>()))
            .Returns((string)null!);

        var coroutine =
            contextAware.SendQueryCoroutine<TestQuery, string>(query, result => { receivedResult = result; });

        coroutine.MoveNext();

        Assert.That(receivedResult, Is.Null);
    }
}

/// <summary>
///     用于测试的整数查询类
/// </summary>
internal class IntQuery : IQuery<bool>
{
    private IArchitectureContext? _context;
    public int Value { get; set; }
    public void SetContext(IArchitectureContext context) => _context = context;
    public IArchitectureContext GetContext() => _context!;
    public bool Do() => Value > 0;
}

/// <summary>
///     用于测试的复杂查询类
/// </summary>
internal class ComplexQuery : IQuery<ComplexResult>
{
    private IArchitectureContext? _context;
    public string Name { get; set; } = string.Empty;
    public List<int> Values { get; set; } = new List<int>();
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    public void SetContext(IArchitectureContext context) => _context = context;
    public IArchitectureContext GetContext() => _context!;
    public ComplexResult Do() => new ComplexResult { ProcessedName = Name, Sum = Values.Sum(), Count = Values.Count };
}

/// <summary>
///     用于测试的复杂结果类
/// </summary>
internal class ComplexResult
{
    public string ProcessedName { get; set; } = string.Empty;
    public int Sum { get; set; }
    public int Count { get; set; }
}