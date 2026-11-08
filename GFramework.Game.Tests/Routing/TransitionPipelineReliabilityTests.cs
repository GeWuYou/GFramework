// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Threading;
using GFramework.Game.Abstractions.Scene;
using GFramework.Game.Scene;

namespace GFramework.Game.Tests.Routing;

/// <summary>验证 Scene 过渡管线的超时、取消及宿主上下文传播。</summary>
[TestFixture]
public class TransitionPipelineReliabilityTests
{
    /// <summary>即使调用方没有提供可取消令牌，阶段处理器也会收到配置的超时令牌。</summary>
    [Test]
    public async Task StageHandler_WithDefaultOuterToken_ReceivesTimeoutCancellation()
    {
        var execute = CreateStageExecution(
        [
            token =>
            {
                Assert.That(token.CanBeCanceled, Is.True);
                return Task.Delay(TimeSpan.FromSeconds(10), token);
            },
        ], timeoutMs: 25);

        await Assert.CatchAsync<OperationCanceledException>(() => execute().WaitAsync(TimeSpan.FromSeconds(2)));
    }

    /// <summary>Around 中间件同样接收独立超时，而不是只在调用方令牌可取消时才启用超时。</summary>
    [Test]
    public async Task AroundHandler_WithDefaultOuterToken_ReceivesTimeoutCancellation()
    {
        var execute = CreateAroundExecution(token =>
        {
            Assert.That(token.CanBeCanceled, Is.True);
            return Task.Delay(TimeSpan.FromSeconds(10), token);
        }, timeoutMs: 25);

        await Assert.CatchAsync<OperationCanceledException>(() => execute().WaitAsync(TimeSpan.FromSeconds(2)));
    }

    /// <summary>ContinueOnError 不会将调用方取消转成成功完成。</summary>
    [Test]
    public async Task AroundHandler_WithContinueOnError_DoesNotSwallowCallerCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync().ConfigureAwait(false);
        var execute = CreateAroundExecution(token => Task.FromCanceled(token),
            outerToken: cancellation.Token, continueOnError: true);

        await Assert.CatchAsync<OperationCanceledException>(execute).ConfigureAwait(false);
    }

    /// <summary>未配置超时时处理器主动取消不能被 ContinueOnError 误判为可忽略的超时。</summary>
    [Test]
    public async Task StageHandler_WithContinueOnError_DoesNotSwallowIndependentCancellation()
    {
        var execute = CreateStageExecution(
        [
            _ => Task.FromException(new OperationCanceledException("handler canceled independently")),
        ], continueOnError: true);

        await Assert.CatchAsync<OperationCanceledException>(execute).ConfigureAwait(false);
    }

    /// <summary>处理器发生真正异步等待后，后续处理器仍从原宿主同步上下文执行。</summary>
    [Test]
    public async Task StageHandlers_AfterAsynchronousWait_PreserveHostContext()
    {
        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var host = new MarkerSynchronizationContext();
        SynchronizationContext? observed = null;
        var execute = CreateStageExecution(
        [
            _ => pending.Task,
            _ =>
            {
                observed = SynchronizationContext.Current;
                return Task.CompletedTask;
            },
        ]);
        var previous = SynchronizationContext.Current;
        Task operation;
        try
        {
            SynchronizationContext.SetSynchronizationContext(host);
            operation = execute();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }

        pending.SetResult();
        await operation.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.That(observed, Is.SameAs(host));
    }

    /// <summary>中间件主动离开宿主上下文后调用 next，下一层中间件和核心操作仍回到宿主。</summary>
    [TestCase(false)]
    [TestCase(true)]
    public async Task AroundHandlers_AfterConfigureAwaitFalse_DispatchNextAndCoreToHostContext(bool includeNextMiddleware)
    {
        var outerWait = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var host = new MarkerSynchronizationContext();
        SynchronizationContext? outerContinuation = host;
        SynchronizationContext? innerEntry = null;
        SynchronizationContext? coreEntry = null;
        var middleware = new List<Func<Func<Task>, Task>>
        {
            async next =>
            {
                await outerWait.Task.ConfigureAwait(false);
                outerContinuation = SynchronizationContext.Current;
                await next().ConfigureAwait(false);
            },
        };
        if (includeNextMiddleware)
        {
            middleware.Add(next =>
            {
                innerEntry = SynchronizationContext.Current;
                return next();
            });
        }
        var execute = CreateAroundChainExecution(middleware, () =>
        {
            coreEntry = SynchronizationContext.Current;
            return Task.CompletedTask;
        });
        var previous = SynchronizationContext.Current;
        Task operation;
        try
        {
            SynchronizationContext.SetSynchronizationContext(host);
            operation = execute();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }

        outerWait.SetResult();
        await operation.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Multiple(() =>
        {
            Assert.That(outerContinuation, Is.Not.SameAs(host), "外层中间件必须实际离开上下文后再调用 next");
            if (includeNextMiddleware)
                Assert.That(innerEntry, Is.SameAs(host));
            Assert.That(coreEntry, Is.SameAs(host));
        });
    }

    /// <summary>为 Scene 管线创建相同的阶段处理器行为，测试实际公开执行入口。</summary>
    private static Func<Task> CreateStageExecution(IEnumerable<Func<CancellationToken, Task>> callbacks,
        int timeoutMs = 0, bool continueOnError = false)
    {
        var pipeline = new SceneTransitionPipeline();
        var priority = 0;
        foreach (var callback in callbacks)
        {
            var handler = new Mock<ISceneTransitionHandler>();
            handler.SetupGet(value => value.Priority).Returns(priority++);
            handler.SetupGet(value => value.Phases).Returns(SceneTransitionPhases.BeforeChange);
            handler.Setup(value => value.ShouldHandle(It.IsAny<SceneTransitionEvent>(), It.IsAny<SceneTransitionPhases>()))
                .Returns(value: true);
            handler.Setup(value => value.HandleAsync(It.IsAny<SceneTransitionEvent>(), It.IsAny<CancellationToken>()))
                .Returns((SceneTransitionEvent _, CancellationToken token) => callback(token));
            pipeline.RegisterHandler(handler.Object, new SceneTransitionHandlerOptions(timeoutMs, continueOnError));
        }
        return () => pipeline.ExecuteAsync(new SceneTransitionEvent(), SceneTransitionPhases.BeforeChange);

    }

    /// <summary>为 Scene 管线创建可控中间件，隔离取消与 ContinueOnError 的交互。</summary>
    private static Func<Task> CreateAroundExecution(Func<CancellationToken, Task> callback,
        int timeoutMs = 0, bool continueOnError = false, CancellationToken outerToken = default)
    {
        var pipeline = new SceneTransitionPipeline();
        var handler = new Mock<ISceneAroundTransitionHandler>();
        handler.Setup(value => value.ShouldHandle(It.IsAny<SceneTransitionEvent>())).Returns(value: true);
        handler.Setup(value => value.HandleAsync(It.IsAny<SceneTransitionEvent>(), It.IsAny<Func<Task>>(),
                It.IsAny<CancellationToken>()))
            .Returns((SceneTransitionEvent _, Func<Task> _, CancellationToken token) => callback(token));
        pipeline.RegisterAroundHandler(handler.Object, new SceneTransitionHandlerOptions(timeoutMs, continueOnError));
        return () => pipeline.ExecuteAroundAsync(new SceneTransitionEvent(), () => Task.CompletedTask, outerToken);

    }

    /// <summary>创建多层真实中间件链，保留每层调用 next 的位置和异步等待方式。</summary>
    private static Func<Task> CreateAroundChainExecution(IEnumerable<Func<Func<Task>, Task>> callbacks,
        Func<Task> core)
    {
        var scenePipeline = new SceneTransitionPipeline();
        var scenePriority = 0;
        foreach (var callback in callbacks)
        {
            var handler = new Mock<ISceneAroundTransitionHandler>();
            handler.SetupGet(value => value.Priority).Returns(scenePriority++);
            handler.Setup(value => value.ShouldHandle(It.IsAny<SceneTransitionEvent>())).Returns(value: true);
            handler.Setup(value => value.HandleAsync(It.IsAny<SceneTransitionEvent>(), It.IsAny<Func<Task>>(),
                    It.IsAny<CancellationToken>()))
                .Returns((SceneTransitionEvent _, Func<Task> next, CancellationToken _) => callback(next));
            scenePipeline.RegisterAroundHandler(handler.Object, new SceneTransitionHandlerOptions(ContinueOnError: false));
        }
        return () => scenePipeline.ExecuteAroundAsync(new SceneTransitionEvent(), core);
    }

    /// <summary>
    ///     在投递回调时恢复上下文身份，以验证传播规则；这不是 Godot 原生线程验收。
    /// </summary>
    private sealed class MarkerSynchronizationContext : SynchronizationContext
    {
        /// <inheritdoc />
        public override void Post(SendOrPostCallback callback, object? state)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                var previous = Current;
                try
                {
                    SetSynchronizationContext(this);
                    callback(state);
                }
                finally
                {
                    SetSynchronizationContext(previous);
                }
            });
        }
    }
}
