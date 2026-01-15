using GFramework.Core.Abstractions.query;
using GFramework.Core.query;
using NUnit.Framework;

namespace GFramework.Core.Tests.query;

[TestFixture]
public class QueryBusTests
{
    [SetUp]
    public void SetUp()
    {
        _queryBus = new QueryBus();
    }

    private QueryBus _queryBus = null!;

    [Test]
    public void Send_Should_Return_Query_Result()
    {
        var input = new TestQueryInput { Value = 10 };
        var query = new TestQuery(input);

        var result = _queryBus.Send(query);

        Assert.That(result, Is.EqualTo(20));
    }

    [Test]
    public void Send_WithNullQuery_Should_ThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _queryBus.Send<int>(null!));
    }

    [Test]
    public void Send_WithStringResult_Should_Return_String()
    {
        var input = new TestQueryInput { Value = 5 };
        var query = new TestStringQuery(input);

        var result = _queryBus.Send(query);

        Assert.That(result, Is.EqualTo("Result: 10"));
    }
}

public sealed class TestQueryInput : IQueryInput
{
    public int Value { get; init; }
}

public sealed class TestQuery : AbstractQuery<TestQueryInput, int>
{
    public TestQuery(TestQueryInput input) : base(input)
    {
    }

    protected override int OnDo(TestQueryInput input)
    {
        return input.Value * 2;
    }
}

public sealed class TestStringQuery : AbstractQuery<TestQueryInput, string>
{
    public TestStringQuery(TestQueryInput input) : base(input)
    {
    }

    protected override string OnDo(TestQueryInput input)
    {
        return $"Result: {input.Value * 2}";
    }
}