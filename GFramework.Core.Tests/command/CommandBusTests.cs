using GFramework.Core.Abstractions.command;
using GFramework.Core.command;
using NUnit.Framework;

namespace GFramework.Core.Tests.command;

[TestFixture]
public class CommandBusTests
{
    [SetUp]
    public void SetUp()
    {
        _commandBus = new CommandBus();
    }

    private CommandBus _commandBus = null!;

    [Test]
    public void Send_Should_Execute_Command()
    {
        var input = new TestCommandInput { Value = 42 };
        var command = new TestCommand(input);

        Assert.DoesNotThrow(() => _commandBus.Send(command));
        Assert.That(command.Executed, Is.True);
        Assert.That(command.ExecutedValue, Is.EqualTo(42));
    }

    [Test]
    public void Send_WithNullCommand_Should_ThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _commandBus.Send(null!));
    }

    [Test]
    public void Send_WithResult_Should_Return_Value()
    {
        var input = new TestCommandInput { Value = 100 };
        var command = new TestCommandWithResult(input);

        var result = _commandBus.Send(command);

        Assert.That(command.Executed, Is.True);
        Assert.That(result, Is.EqualTo(200));
    }

    [Test]
    public void Send_WithResult_AndNullCommand_Should_ThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _commandBus.Send<int>(null!));
    }
}

public sealed class TestCommandInput : ICommandInput
{
    public int Value { get; init; }
}

public sealed class TestCommand : AbstractCommand<TestCommandInput>
{
    public TestCommand(TestCommandInput input) : base(input)
    {
    }

    public bool Executed { get; private set; }
    public int ExecutedValue { get; private set; }

    protected override void OnExecute(TestCommandInput input)
    {
        Executed = true;
        ExecutedValue = 42;
    }
}

public sealed class TestCommandWithResult : AbstractCommand<TestCommandInput, int>
{
    public TestCommandWithResult(TestCommandInput input) : base(input)
    {
    }

    public bool Executed { get; private set; }

    protected override int OnExecute(TestCommandInput input)
    {
        Executed = true;
        return input.Value * 2;
    }
}