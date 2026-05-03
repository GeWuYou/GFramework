// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Command;
using GFramework.Core.Abstractions.Rule;
using GFramework.Core.Architectures;
using GFramework.Core.Command;
using GFramework.Core.Environment;
using GFramework.Core.Events;
using GFramework.Core.Ioc;
using GFramework.Core.Query;
using GFramework.Cqrs.Abstractions.Cqrs.Command;

namespace GFramework.Core.Tests.Command;

/// <summary>
///     AbstractAsyncCommand类的单元测试
///     测试内容包括：
///     - 异步命令无返回值版本的基础实现
///     - 异步命令有返回值版本的基础实现
///     - ExecuteAsync方法调用
///     - ExecuteAsync方法的异常处理
///     - 上下文感知功能（SetContext, GetContext）
///     - 日志功能（Logger属性）
///     - 子类继承行为验证（两个版本）
///     - 命令执行前日志记录
///     - 命令执行后日志记录
///     - 错误情况下的日志记录
///     - 无返回值版本的行为
///     - 有返回值版本的行为
/// </summary>
[TestFixture]
public class AbstractAsyncCommandTests
{
    [SetUp]
    public void SetUp()
    {
        _container = new MicrosoftDiContainer();
        _container.RegisterPlurality(new EventBus());
        _container.RegisterPlurality(new CommandExecutor());
        _container.RegisterPlurality(new QueryExecutor());
        _container.RegisterPlurality(new DefaultEnvironment());
        _container.RegisterPlurality(new AsyncQueryExecutor());
        _context = new ArchitectureContext(_container);
    }

    private ArchitectureContext _context = null!;
    private MicrosoftDiContainer _container = null!;

    /// <summary>
    ///     测试异步命令无返回值版本的基础实现
    /// </summary>
    [Test]
    public async Task AbstractAsyncCommand_Should_Implement_IAsyncCommand_Interface()
    {
        var input = new TestCommandInputV2();
        var command = new TestAsyncCommandV3(input);

        Assert.That(command, Is.InstanceOf<IAsyncCommand>());
    }

    /// <summary>
    ///     测试异步命令有返回值版本的基础实现
    /// </summary>
    [Test]
    public async Task AbstractAsyncCommand_WithResult_Should_Implement_IAsyncCommand_Interface()
    {
        var input = new TestCommandInputV2();
        var command = new TestAsyncCommandWithResultV3(input);

        Assert.That(command, Is.InstanceOf<IAsyncCommand<int>>());
    }

    /// <summary>
    ///     测试ExecuteAsync方法调用
    /// </summary>
    [Test]
    public async Task ExecuteAsync_Should_Invoke_OnExecuteAsync_Method()
    {
        var input = new TestCommandInputV2 { Value = 42 };
        var command = new TestAsyncCommandV3(input);
        var asyncCommand = (IAsyncCommand)command;

        await asyncCommand.ExecuteAsync();

        Assert.That(command.Executed, Is.True);
        Assert.That(command.ExecutedValue, Is.EqualTo(42));
    }

    /// <summary>
    ///     测试ExecuteAsync方法（带返回值）调用
    /// </summary>
    [Test]
    public async Task ExecuteAsync_WithResult_Should_Invoke_OnExecuteAsync_Method_And_Return_Result()
    {
        var input = new TestCommandInputV2 { Value = 100 };
        var command = new TestAsyncCommandWithResultV3(input);
        var asyncCommand = (IAsyncCommand<int>)command;

        var result = await asyncCommand.ExecuteAsync();

        Assert.That(command.Executed, Is.True);
        Assert.That(result, Is.EqualTo(200));
    }

    /// <summary>
    ///     测试ExecuteAsync方法的异常处理
    /// </summary>
    [Test]
    public void ExecuteAsync_Should_Propagate_Exception_From_OnExecuteAsync()
    {
        var input = new TestCommandInputV2();
        var command = new TestAsyncCommandWithExceptionV3(input);
        var asyncCommand = (IAsyncCommand)command;

        Assert.ThrowsAsync<InvalidOperationException>(() => asyncCommand.ExecuteAsync());
    }

    /// <summary>
    ///     测试上下文感知功能 - SetContext方法
    /// </summary>
    [Test]
    public void SetContext_Should_Set_Context_Property()
    {
        var input = new TestCommandInputV2();
        var command = new TestAsyncCommandV3(input);
        var contextAware = (IContextAware)command;

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
        var input = new TestCommandInputV2();
        var command = new TestAsyncCommandV3(input);
        var contextAware = (IContextAware)command;

        contextAware.SetContext(_context);

        var context = contextAware.GetContext();
        Assert.That(context, Is.Not.Null);
        Assert.That(context, Is.SameAs(_context));
    }

    /// <summary>
    ///     测试子类继承行为验证 - 无返回值版本
    /// </summary>
    [Test]
    public async Task Child_Class_Should_Inherit_And_Override_OnExecuteAsync_Method()
    {
        var input = new TestCommandInputV2 { Value = 100 };
        var command = new TestAsyncCommandChildV3(input);
        var asyncCommand = (IAsyncCommand)command;

        await asyncCommand.ExecuteAsync();

        Assert.That(command.Executed, Is.True);
        Assert.That(command.ExecutedValue, Is.EqualTo(200));
    }

    /// <summary>
    ///     测试子类继承行为验证 - 有返回值版本
    /// </summary>
    [Test]
    public async Task Child_Class_WithResult_Should_Inherit_And_Override_OnExecuteAsync_Method()
    {
        var input = new TestCommandInputV2 { Value = 50 };
        var command = new TestAsyncCommandWithResultChildV3(input);
        var asyncCommand = (IAsyncCommand<int>)command;

        var result = await asyncCommand.ExecuteAsync();

        Assert.That(command.Executed, Is.True);
        Assert.That(result, Is.EqualTo(150));
    }

    /// <summary>
    ///     测试异步命令执行生命周期完整性
    /// </summary>
    [Test]
    public async Task AsyncCommand_Should_Complete_Execution_Lifecycle()
    {
        var input = new TestCommandInputV2 { Value = 42 };
        var command = new TestAsyncCommandV3(input);
        var asyncCommand = (IAsyncCommand)command;

        Assert.That(command.Executed, Is.False, "Command should not be executed before ExecuteAsync");

        await asyncCommand.ExecuteAsync();

        Assert.That(command.Executed, Is.True, "Command should be executed after ExecuteAsync");
        Assert.That(command.ExecutedValue, Is.EqualTo(42), "Command should have correct executed value");
    }

    /// <summary>
    ///     测试异步命令多次执行
    /// </summary>
    [Test]
    public async Task AsyncCommand_Should_Be_Executable_Multiple_Times()
    {
        var input = new TestCommandInputV2 { Value = 10 };
        var command = new TestAsyncCommandV3(input);
        var asyncCommand = (IAsyncCommand)command;

        await asyncCommand.ExecuteAsync();
        Assert.That(command.ExecutedValue, Is.EqualTo(10), "First execution should have value 10");

        await asyncCommand.ExecuteAsync();
        Assert.That(command.ExecutedValue, Is.EqualTo(10), "Second execution should have value 10");
    }

    /// <summary>
    ///     测试异步命令（带返回值）的返回值类型
    /// </summary>
    [Test]
    public async Task AsyncCommand_WithResult_Should_Return_Correct_Type()
    {
        var input = new TestCommandInputV2 { Value = 100 };
        var command = new TestAsyncCommandWithResultV3(input);
        var asyncCommand = (IAsyncCommand<int>)command;

        var result = await asyncCommand.ExecuteAsync();

        Assert.That(result, Is.InstanceOf<int>());
        Assert.That(result, Is.EqualTo(200));
    }
}
