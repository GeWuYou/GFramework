// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Rule;
using GFramework.Cqrs.Abstractions.Cqrs;
using GFramework.Cqrs.Abstractions.Cqrs.Command;
using GFramework.Cqrs.Cqrs.Command;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     验证 CQRS handler 基类在脱离 dispatcher 使用时会显式失败，并在注入上下文后保持可观察行为。
/// </summary>
[TestFixture]
internal sealed class AbstractCqrsHandlerContextTests
{
    /// <summary>
    ///     验证新的轻量 handler 基类不会再偷偷回退到全局 GameContext。
    /// </summary>
    [Test]
    public void GetContext_Should_Throw_When_Handler_Has_Not_Been_Initialized_By_Runtime()
    {
        var handler = new TestCommandHandler();

        var exception = Assert.Throws<InvalidOperationException>(() => ((IContextAware)handler).GetContext());

        Assert.That(
            exception!.Message,
            Does.Contain("has not been initialized").IgnoreCase);
    }

    /// <summary>
    ///     验证 runtime 注入上下文后，派生 handler 可以继续访问 Context 并收到 OnContextReady 回调。
    /// </summary>
    [Test]
    public async Task Handle_Should_Observe_Injected_Context_And_OnContextReady_Callback()
    {
        var handler = new TestCommandHandler();
        var context = new Mock<IArchitectureContext>(MockBehavior.Strict).Object;

        ((IContextAware)handler).SetContext(context);
        await handler.Handle(new TestCommand(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(handler.OnContextReadyCallCount, Is.EqualTo(1));
            Assert.That(handler.LastObservedContext, Is.SameAs(context));
        });
    }

    /// <summary>
    ///     用于验证上下文注入行为的最小 CQRS 命令。
    /// </summary>
    private sealed record TestCommand : ICommand<Unit>;

    /// <summary>
    ///     暴露基类上下文访问与初始化回调的测试处理器。
    /// </summary>
    private sealed class TestCommandHandler : AbstractCommandHandler<TestCommand>
    {
        public int OnContextReadyCallCount { get; private set; }

        public IArchitectureContext? LastObservedContext { get; private set; }

        protected override void OnContextReady()
        {
            OnContextReadyCallCount++;
        }

        public override ValueTask<Unit> Handle(TestCommand command, CancellationToken cancellationToken)
        {
            LastObservedContext = Context;
            return ValueTask.FromResult(Unit.Value);
        }
    }
}
