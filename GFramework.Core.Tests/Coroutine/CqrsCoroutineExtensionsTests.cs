// Copyright (c) 2026 GeWuYou
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Abstractions.Cqrs;
using GFramework.Core.Abstractions.Rule;
using GFramework.Core.Cqrs.Extensions;

namespace GFramework.Core.Tests.Coroutine;

/// <summary>
///     <see cref="CqrsCoroutineExtensions" /> 的单元测试类。
///     验证新的 CQRS 协程扩展直接走框架内建 CQRS runtime，
///     并确保协程对命令调度异常的传播行为保持稳定。
/// </summary>
[TestFixture]
public class CqrsCoroutineExtensionsTests
{
    /// <summary>
    ///     验证SendCommandCoroutine应该返回IEnumerator<IYieldInstruction>
    /// </summary>
    [Test]
    public void SendCommandCoroutine_Should_Return_IEnumerator_Of_YieldInstruction()
    {
        var command = new TestCommand("Test");
        var contextAware = new TestContextAware();

        contextAware.MockContext
            .Setup(ctx => ctx.SendAsync(command, It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var coroutine = CqrsCoroutineExtensions.SendCommandCoroutine(contextAware, command);

        Assert.That(coroutine, Is.InstanceOf<IEnumerator<IYieldInstruction>>());
    }

    /// <summary>
    ///     验证 SendCommandCoroutine 在底层命令调度失败时会重新抛出原始异常。
    /// </summary>
    [Test]
    public void SendCommandCoroutine_Should_Rethrow_Inner_Exception_When_Command_Fails()
    {
        var command = new TestCommand("Test");
        var contextAware = new TestContextAware();
        var expectedException = new InvalidOperationException("Command failed.");

        contextAware.MockContext
            .Setup(ctx => ctx.SendAsync(command, It.IsAny<CancellationToken>()))
            .Returns(new ValueTask(Task.FromException(expectedException)));

        var coroutine = CqrsCoroutineExtensions.SendCommandCoroutine(contextAware, command);

        Assert.That(coroutine.MoveNext(), Is.True);
        var exception = Assert.Throws<InvalidOperationException>(() => coroutine.MoveNext());
        Assert.That(exception, Is.SameAs(expectedException));
    }

    /// <summary>
    ///     验证 SendCommandCoroutine 在提供错误回调时也会传递解包后的原始异常，
    ///     避免回调路径暴露 <see cref="AggregateException" />。
    /// </summary>
    [Test]
    public void SendCommandCoroutine_Should_Forward_Inner_Exception_To_Error_Handler()
    {
        var command = new TestCommand("Test");
        var contextAware = new TestContextAware();
        var expectedException = new InvalidOperationException("Command failed.");
        Exception? capturedException = null;

        contextAware.MockContext
            .Setup(ctx => ctx.SendAsync(command, It.IsAny<CancellationToken>()))
            .Returns(new ValueTask(Task.FromException(expectedException)));

        var coroutine = CqrsCoroutineExtensions.SendCommandCoroutine(
            contextAware,
            command,
            exception => capturedException = exception);

        Assert.That(coroutine.MoveNext(), Is.True);
        Assert.That(coroutine.MoveNext(), Is.False);
        Assert.That(capturedException, Is.SameAs(expectedException));
    }

    /// <summary>
    ///     验证 SendCommandCoroutine 在底层命令被取消且未提供错误回调时会抛出取消异常。
    /// </summary>
    [Test]
    public void SendCommandCoroutine_Should_Throw_TaskCanceledException_When_Command_Is_Canceled()
    {
        var command = new TestCommand("Test");
        var contextAware = new TestContextAware();
        using var cancellationTokenSource = new CancellationTokenSource();

        cancellationTokenSource.Cancel();
        contextAware.MockContext
            .Setup(ctx => ctx.SendAsync(command, It.IsAny<CancellationToken>()))
            .Returns(new ValueTask(Task.FromCanceled(cancellationTokenSource.Token)));

        var coroutine = CqrsCoroutineExtensions.SendCommandCoroutine(contextAware, command);

        Assert.That(coroutine.MoveNext(), Is.True);
        Assert.Throws<TaskCanceledException>(() => coroutine.MoveNext());
    }

    /// <summary>
    ///     验证 SendCommandCoroutine 在底层命令被取消且提供错误回调时会把取消异常转发给回调。
    /// </summary>
    [Test]
    public void SendCommandCoroutine_Should_Forward_TaskCanceledException_To_Error_Handler_When_Command_Is_Canceled()
    {
        var command = new TestCommand("Test");
        var contextAware = new TestContextAware();
        using var cancellationTokenSource = new CancellationTokenSource();
        Exception? capturedException = null;

        cancellationTokenSource.Cancel();
        contextAware.MockContext
            .Setup(ctx => ctx.SendAsync(command, It.IsAny<CancellationToken>()))
            .Returns(new ValueTask(Task.FromCanceled(cancellationTokenSource.Token)));

        var coroutine = CqrsCoroutineExtensions.SendCommandCoroutine(
            contextAware,
            command,
            exception => capturedException = exception);

        Assert.That(coroutine.MoveNext(), Is.True);
        Assert.That(coroutine.MoveNext(), Is.False);
        Assert.That(capturedException, Is.TypeOf<TaskCanceledException>());
    }

    /// <summary>
    ///     测试用的简单命令类
    /// </summary>
    private sealed record TestCommand(string Data) : IRequest<Unit>;

    /// <summary>
    ///     上下文感知基类的模拟实现
    /// </summary>
    private sealed class TestContextAware : IContextAware
    {
        /// <summary>
        ///     提供可配置的架构上下文 Mock。
        /// </summary>
        public Mock<IArchitectureContext> MockContext { get; } = new();

        /// <summary>
        ///     获取当前架构上下文。
        /// </summary>
        /// <returns>用于 CQRS 调用的架构上下文实例。</returns>
        public IArchitectureContext GetContext()
        {
            return MockContext.Object;
        }

        /// <summary>
        ///     设置架构上下文。
        /// </summary>
        /// <param name="context">要设置的架构上下文。</param>
        public void SetContext(IArchitectureContext context)
        {
        }
    }
}
