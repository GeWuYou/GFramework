using GFramework.Core.Abstractions.command;
using GFramework.Core.command;
using NUnit.Framework;

namespace GFramework.Core.Tests.command;

/// <summary>
///     CommandBus类的单元测试
///     测试内容包括：
///     - Send方法执行命令
///     - Send方法处理null命令
///     - Send方法（带返回值）返回值
///     - Send方法（带返回值）处理null命令
///     - SendAsync方法执行异步命令
///     - SendAsync方法处理null异步命令
///     - SendAsync方法（带返回值）返回值
///     - SendAsync方法（带返回值）处理null异步命令
/// </summary>
[TestFixture]
public class CommandBusTests
{
    [SetUp]
    public void SetUp()
    {
        _commandBus = new CommandBus();
    }

    private CommandBus _commandBus = null!;

    /// <summary>
    ///     测试Send方法执行命令
    /// </summary>
    [Test]
    public void Send_Should_Execute_Command()
    {
        var input = new TestCommandInput { Value = 42 };
        var command = new TestCommand(input);

        Assert.DoesNotThrow(() => _commandBus.Send(command));
        Assert.That(command.Executed, Is.True);
        Assert.That(command.ExecutedValue, Is.EqualTo(42));
    }

    /// <summary>
    ///     测试Send方法处理null命令时抛出ArgumentNullException异常
    /// </summary>
    [Test]
    public void Send_WithNullCommand_Should_ThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _commandBus.Send(null!));
    }

    /// <summary>
    ///     测试Send方法（带返回值）正确返回值
    /// </summary>
    [Test]
    public void Send_WithResult_Should_Return_Value()
    {
        var input = new TestCommandInput { Value = 100 };
        var command = new TestCommandWithResult(input);

        var result = _commandBus.Send(command);

        Assert.That(command.Executed, Is.True);
        Assert.That(result, Is.EqualTo(200));
    }

    /// <summary>
    ///     测试Send方法（带返回值）处理null命令时抛出ArgumentNullException异常
    /// </summary>
    [Test]
    public void Send_WithResult_AndNullCommand_Should_ThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _commandBus.Send<int>(null!));
    }

    /// <summary>
    ///     测试SendAsync方法执行异步命令
    /// </summary>
    [Test]
    public async Task SendAsync_Should_Execute_AsyncCommand()
    {
        var input = new TestCommandInput { Value = 42 };
        var command = new TestAsyncCommand(input);

        await _commandBus.SendAsync(command);

        Assert.That(command.Executed, Is.True);
        Assert.That(command.ExecutedValue, Is.EqualTo(42));
    }

    /// <summary>
    ///     测试SendAsync方法处理null异步命令时抛出ArgumentNullException异常
    /// </summary>
    [Test]
    public void SendAsync_WithNullCommand_Should_ThrowArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(async () => await _commandBus.SendAsync(null!));
    }

    /// <summary>
    ///     测试SendAsync方法（带返回值）正确返回值
    /// </summary>
    [Test]
    public async Task SendAsync_WithResult_Should_Return_Value()
    {
        var input = new TestCommandInput { Value = 100 };
        var command = new TestAsyncCommandWithResult(input);

        var result = await _commandBus.SendAsync(command);

        Assert.That(command.Executed, Is.True);
        Assert.That(result, Is.EqualTo(200));
    }

    /// <summary>
    ///     测试SendAsync方法（带返回值）处理null异步命令时抛出ArgumentNullException异常
    /// </summary>
    [Test]
    public void SendAsync_WithResult_AndNullCommand_Should_ThrowArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(async () => await _commandBus.SendAsync<int>(null!));
    }
}

/// <summary>
///     测试用命令输入类，实现ICommandInput接口
/// </summary>
public sealed class TestCommandInput : ICommandInput
{
    /// <summary>
    ///     获取或设置值
    /// </summary>
    public int Value { get; init; }
}

/// <summary>
///     测试用命令类，继承AbstractCommand
/// </summary>
public sealed class TestCommand : AbstractCommand<TestCommandInput>
{
    /// <summary>
    ///     构造函数
    /// </summary>
    /// <param name="input">命令输入</param>
    public TestCommand(TestCommandInput input) : base(input)
    {
    }

    /// <summary>
    ///     获取命令是否已执行
    /// </summary>
    public bool Executed { get; private set; }

    /// <summary>
    ///     获取执行的值
    /// </summary>
    public int ExecutedValue { get; private set; }

    /// <summary>
    ///     执行命令的重写方法
    /// </summary>
    /// <param name="input">命令输入</param>
    protected override void OnExecute(TestCommandInput input)
    {
        Executed = true;
        ExecutedValue = 42;
    }
}

/// <summary>
///     测试用带返回值的命令类，继承AbstractCommand
/// </summary>
public sealed class TestCommandWithResult : AbstractCommand<TestCommandInput, int>
{
    /// <summary>
    ///     构造函数
    /// </summary>
    /// <param name="input">命令输入</param>
    public TestCommandWithResult(TestCommandInput input) : base(input)
    {
    }

    /// <summary>
    ///     获取命令是否已执行
    /// </summary>
    public bool Executed { get; private set; }

    /// <summary>
    ///     执行命令并返回结果的重写方法
    /// </summary>
    /// <param name="input">命令输入</param>
    /// <returns>执行结果</returns>
    protected override int OnExecute(TestCommandInput input)
    {
        Executed = true;
        return input.Value * 2;
    }
}

/// <summary>
///     测试用异步命令类，继承AbstractAsyncCommand
/// </summary>
public sealed class TestAsyncCommand : AbstractAsyncCommand<TestCommandInput>
{
    /// <summary>
    ///     构造函数
    /// </summary>
    /// <param name="input">命令输入</param>
    public TestAsyncCommand(TestCommandInput input) : base(input)
    {
    }

    /// <summary>
    ///     获取命令是否已执行
    /// </summary>
    public bool Executed { get; private set; }

    /// <summary>
    ///     获取执行的值
    /// </summary>
    public int ExecutedValue { get; private set; }

    /// <summary>
    ///     执行异步命令的重写方法
    /// </summary>
    /// <param name="input">命令输入</param>
    /// <returns>表示异步操作的任务</returns>
    protected override Task OnExecuteAsync(TestCommandInput input)
    {
        Executed = true;
        ExecutedValue = 42;
        return Task.CompletedTask;
    }
}

/// <summary>
///     测试用带返回值的异步命令类，继承AbstractAsyncCommand
/// </summary>
public sealed class TestAsyncCommandWithResult : AbstractAsyncCommand<TestCommandInput, int>
{
    /// <summary>
    ///     构造函数
    /// </summary>
    /// <param name="input">命令输入</param>
    public TestAsyncCommandWithResult(TestCommandInput input) : base(input)
    {
    }

    /// <summary>
    ///     获取命令是否已执行
    /// </summary>
    public bool Executed { get; private set; }

    /// <summary>
    ///     执行异步命令并返回结果的重写方法
    /// </summary>
    /// <param name="input">命令输入</param>
    /// <returns>执行结果的异步任务</returns>
    protected override Task<int> OnExecuteAsync(TestCommandInput input)
    {
        Executed = true;
        return Task.FromResult(input.Value * 2);
    }
}