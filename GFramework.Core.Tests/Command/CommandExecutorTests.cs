// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Command;
using GFramework.Core.Tests.Architectures;

namespace GFramework.Core.Tests.Command;

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
public class CommandExecutorTests
{
    [SetUp]
    public void SetUp()
    {
        _commandExecutor = new CommandExecutor();
    }

    private CommandExecutor _commandExecutor = null!;

    /// <summary>
    ///     测试Send方法执行命令
    /// </summary>
    [Test]
    public void Send_Should_Execute_Command()
    {
        var input = new TestCommandInput { Value = 42 };
        var command = new TestCommand(input);

        Assert.DoesNotThrow(() => _commandExecutor.Send(command));
        Assert.That(command.Executed, Is.True);
        Assert.That(command.ExecutedValue, Is.EqualTo(42));
    }

    /// <summary>
    ///     测试Send方法处理null命令时抛出ArgumentNullException异常
    /// </summary>
    [Test]
    public void Send_WithNullCommand_Should_ThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _commandExecutor.Send(null!));
    }

    /// <summary>
    ///     测试Send方法（带返回值）正确返回值
    /// </summary>
    [Test]
    public void Send_WithResult_Should_Return_Value()
    {
        var input = new TestCommandInput { Value = 100 };
        var command = new TestCommandWithResult(input);

        var result = _commandExecutor.Send(command);

        Assert.That(command.Executed, Is.True);
        Assert.That(result, Is.EqualTo(200));
    }

    /// <summary>
    ///     测试Send方法（带返回值）处理null命令时抛出ArgumentNullException异常
    /// </summary>
    [Test]
    public void Send_WithResult_AndNullCommand_Should_ThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _commandExecutor.Send<int>(null!));
    }

    /// <summary>
    ///     验证 legacy 同步命令桥接会在线程池上等待 runtime，
    ///     避免直接继承调用方当前的同步上下文。
    /// </summary>
    [Test]
    public void Send_Should_Bridge_Through_Runtime_Without_Reusing_Caller_SynchronizationContext()
    {
        var runtime = new RecordingCqrsRuntime();
        var executor = new CommandExecutor(runtime);
        var command = new ContextAwareLegacyCommand();
        var expectedContext = new TestArchitectureContextBaseStub();
        ((GFramework.Core.Abstractions.Rule.IContextAware)command).SetContext(expectedContext);
        var originalContext = SynchronizationContext.Current;

        try
        {
            SynchronizationContext.SetSynchronizationContext(new TestLegacySynchronizationContext());

            executor.Send(command);

            Assert.Multiple(() =>
            {
                Assert.That(runtime.LastRequest, Is.TypeOf<GFramework.Core.Cqrs.LegacyCommandDispatchRequest>());
                Assert.That(runtime.ObservedSynchronizationContextType, Is.Null);
            });
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(originalContext);
        }
    }

    /// <summary>
    ///     验证 legacy 带返回值命令桥接也会保留上下文注入与返回值语义。
    /// </summary>
    [Test]
    public void Send_WithResult_Should_Bridge_Through_Runtime_And_Preserve_Context()
    {
        var runtime = new RecordingCqrsRuntime(static _ => 123);
        var executor = new CommandExecutor(runtime);
        var command = new ContextAwareLegacyCommandWithResult(123);
        var expectedContext = new TestArchitectureContextBaseStub();
        ((GFramework.Core.Abstractions.Rule.IContextAware)command).SetContext(expectedContext);

        var result = executor.Send(command);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(123));
            Assert.That(runtime.LastRequest, Is.TypeOf<GFramework.Core.Cqrs.LegacyCommandResultDispatchRequest>());
            Assert.That(command.ObservedContext, Is.SameAs(expectedContext));
        });
    }

    /// <summary>
    ///     测试SendAsync方法执行异步命令
    /// </summary>
    [Test]
    public async Task SendAsync_Should_Execute_AsyncCommand()
    {
        var input = new TestCommandInput { Value = 42 };
        var command = new TestAsyncCommand(input);

        await _commandExecutor.SendAsync(command);

        Assert.That(command.Executed, Is.True);
        Assert.That(command.ExecutedValue, Is.EqualTo(42));
    }

    /// <summary>
    ///     测试SendAsync方法处理null异步命令时抛出ArgumentNullException异常
    /// </summary>
    [Test]
    public void SendAsync_WithNullCommand_Should_ThrowArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(() => _commandExecutor.SendAsync(null!));
    }

    /// <summary>
    ///     测试SendAsync方法（带返回值）正确返回值
    /// </summary>
    [Test]
    public async Task SendAsync_WithResult_Should_Return_Value()
    {
        var input = new TestCommandInput { Value = 100 };
        var command = new TestAsyncCommandWithResult(input);

        var result = await _commandExecutor.SendAsync(command);

        Assert.That(command.Executed, Is.True);
        Assert.That(result, Is.EqualTo(200));
    }

    /// <summary>
    ///     测试SendAsync方法（带返回值）处理null异步命令时抛出ArgumentNullException异常
    /// </summary>
    [Test]
    public void SendAsync_WithResult_AndNullCommand_Should_ThrowArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(() => _commandExecutor.SendAsync<int>(null!));
    }

    /// <summary>
    ///     为同步 bridge 测试提供最小架构上下文替身。
    /// </summary>
    private sealed class TestArchitectureContextBaseStub : TestArchitectureContextBase
    {
    }
}
