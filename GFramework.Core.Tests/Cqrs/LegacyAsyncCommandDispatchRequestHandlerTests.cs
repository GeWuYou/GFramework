// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Command;
using GFramework.Core.Abstractions.Rule;
using GFramework.Core.Cqrs;
using GFramework.Core.Rule;
using GFramework.Core.Tests.Architectures;

namespace GFramework.Core.Tests.Cqrs;

/// <summary>
///     验证 legacy 异步无返回值命令 bridge handler 的取消语义。
/// </summary>
[TestFixture]
public class LegacyAsyncCommandDispatchRequestHandlerTests
{
    /// <summary>
    ///     验证当取消令牌在执行前已触发时，handler 不会启动底层 legacy 命令。
    /// </summary>
    [Test]
    public void Handle_Should_Throw_Without_Executing_Command_When_Cancellation_Is_Already_Requested()
    {
        var handler = new LegacyAsyncCommandDispatchRequestHandler();
        var command = new ProbeAsyncCommand(Task.CompletedTask);
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        Assert.ThrowsAsync<OperationCanceledException>(
            async () => await handler.Handle(
                    new LegacyAsyncCommandDispatchRequest(command),
                    cancellationTokenSource.Token)
                .AsTask()
                .ConfigureAwait(false));
        Assert.That(command.ExecutionCount, Is.Zero);
    }

    /// <summary>
    ///     验证当底层 legacy 命令正在运行时，handler 会通过 <c>WaitAsync</c> 及时向调用方暴露取消。
    /// </summary>
    [Test]
    public async Task Handle_Should_Observe_Cancellation_While_Command_Is_Running()
    {
        var completionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var handler = new LegacyAsyncCommandDispatchRequestHandler();
        var command = new ProbeAsyncCommand(completionSource.Task);
        using var cancellationTokenSource = new CancellationTokenSource();
        ((IContextAware)handler).SetContext(new TestArchitectureContextBaseStub());

        var handleTask = handler.Handle(
                new LegacyAsyncCommandDispatchRequest(command),
                cancellationTokenSource.Token)
            .AsTask();

        cancellationTokenSource.Cancel();

        Assert.That(
            async () => await handleTask.ConfigureAwait(false),
            Throws.InstanceOf<OperationCanceledException>());
        Assert.That(command.ExecutionCount, Is.EqualTo(1));
    }

    /// <summary>
    ///     为 handler 取消测试提供可控完成时机的异步命令替身。
    /// </summary>
    private sealed class ProbeAsyncCommand(Task executionTask) : ContextAwareBase, IAsyncCommand
    {
        /// <summary>
        ///     获取底层命令逻辑的触发次数。
        /// </summary>
        public int ExecutionCount { get; private set; }

        /// <inheritdoc />
        public Task ExecuteAsync()
        {
            ExecutionCount++;
            return executionTask;
        }
    }

    /// <summary>
    ///     为 handler 取消测试提供最小架构上下文替身。
    /// </summary>
    private sealed class TestArchitectureContextBaseStub : TestArchitectureContextBase
    {
    }
}
