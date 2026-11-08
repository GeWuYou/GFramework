// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Threading;
using GFramework.Game.Abstractions.Enums;
using GFramework.Game.Abstractions.Scene;
using GFramework.Game.Runtime;
using GFramework.Game.Scene;
using Microsoft.Extensions.Logging;

namespace GFramework.Game.Tests.Scene;

/// <summary>Preserves cancellation through middleware and verifies router-owned transition diagnostics.</summary>
[TestFixture]
public sealed class SceneRouterReviewTests
{
    /// <summary>Dispose aliases retain the one pending shutdown task instead of allocating independent async wrappers.</summary>
    [Test]
    public async Task DisposeAliases_ShareCanonicalStopTask()
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var target = CreateScene();
        target.Setup(value => value.OnUnloadAsync(It.IsAny<CancellationToken>())).Returns(new ValueTask(release.Task));
        var router = CreateRouter(target.Object);
        await router.PushAsync("target");
        var stopping = router.StopAsync();
        try
        {
            Assert.That(stopping.IsCompleted, Is.False);
            Assert.That(router.DisposeAsync().AsTask(), Is.SameAs(stopping));
            Assert.That(router.DisposeAsync().AsTask(), Is.SameAs(stopping));
        }
        finally
        {
            release.TrySetResult();
        }
        await stopping.WaitAsync(TimeSpan.FromSeconds(5));
    }

    /// <summary>Cleanup that succeeds must not wrap navigation cancellation into an error ignored by around middleware.</summary>
    /// <param name="cancelDuringLoad">Whether loading or entering the target requests cancellation.</param>
    [TestCase(true)]
    [TestCase(false)]
    public async Task TargetCancellation_ThroughNext_RemainsCancellation(bool cancelDuringLoad)
    {
        var target = CreateScene();
        var cancellation = new OperationCanceledException("target cancelled");
        if (cancelDuringLoad)
        {
            target.Setup(value => value.OnLoadAsync(It.IsAny<ISceneEnterParam?>(), It.IsAny<CancellationToken>()))
                .Returns(() => ValueTask.FromException(cancellation));
        }
        else
        {
            target.Setup(value => value.OnEnterAsync(It.IsAny<CancellationToken>()))
                .Returns(() => ValueTask.FromException(cancellation));
        }
        var router = CreateRouter(target.Object);
        var around = new Mock<ISceneAroundTransitionHandler>();
        around.Setup(value => value.ShouldHandle(It.IsAny<SceneTransitionEvent>())).Returns(value: true);
        around.Setup(value => value.HandleAsync(It.IsAny<SceneTransitionEvent>(), It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns((SceneTransitionEvent _, Func<Task> next, CancellationToken _) => next());
        router.RegisterAroundHandler(around.Object, new SceneTransitionHandlerOptions { ContinueOnError = true });
        await Assert.CatchAsync<OperationCanceledException>(() => router.PushAsync("target", cancellationToken: global::System.Threading.CancellationToken.None));
        around.Verify(value => value.HandleAsync(It.IsAny<SceneTransitionEvent>(), It.IsAny<Func<Task>>(),
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(router.Current, Is.Null);
        target.Verify(value => value.OnUnloadAsync(It.IsAny<CancellationToken>()), Times.Once);
        await router.StopAsync();
    }

    /// <summary>A continued stage failure remains visible through the logger supplied to the public router.</summary>
    [Test]
    public async Task ContinuedTransitionFailure_ReachesConfiguredRouterLogger()
    {
        var failure = new InvalidOperationException("transition failed");
        var logger = new CapturingLogger();
        var target = CreateScene();
        var router = CreateRouter(target.Object, logger);
        var handler = new Mock<ISceneTransitionHandler>();
        handler.SetupGet(value => value.Phases).Returns(SceneTransitionPhases.BeforeChange);
        handler.Setup(value => value.ShouldHandle(It.IsAny<SceneTransitionEvent>(), It.IsAny<SceneTransitionPhases>())).Returns(value: true);
        handler.Setup(value => value.HandleAsync(It.IsAny<SceneTransitionEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromException(failure));
        router.RegisterHandler(handler.Object, new SceneTransitionHandlerOptions { ContinueOnError = true });
        Assert.That(await router.PushAsync("target"), Is.EqualTo(SceneOperationResult.Applied));
        Assert.That(logger.Errors.Exists(value => ReferenceEquals(value, failure)), Is.True);
        await router.StopAsync();
    }

    /// <summary>Creates one successful target without any native host dependency.</summary>
    private static Mock<ISceneBehavior> CreateScene()
    {
        var target = new Mock<ISceneBehavior>();
        target.SetupGet(value => value.Key).Returns("target");
        target.Setup(value => value.OnLoadAsync(It.IsAny<ISceneEnterParam?>(), It.IsAny<CancellationToken>())).Returns(ValueTask.CompletedTask);
        target.Setup(value => value.OnEnterAsync(It.IsAny<CancellationToken>())).Returns(ValueTask.CompletedTask);
        target.Setup(value => value.OnExitAsync(It.IsAny<CancellationToken>())).Returns(ValueTask.CompletedTask);
        target.Setup(value => value.OnUnloadAsync(It.IsAny<CancellationToken>())).Returns(ValueTask.CompletedTask);
        return target;
    }

    /// <summary>Builds the production router, optionally injecting the logger whose output is observed.</summary>
    private static SceneRouter CreateRouter(ISceneBehavior target, ILogger<SceneRouter>? logger = null)
    {
        var factory = new Mock<ISceneFactory>();
        factory.Setup(value => value.Create("target")).Returns(target);
        var root = new Mock<ISceneRoot>();
        return logger is null
            ? new SceneRouter(factory.Object, root.Object, new InlineGameDispatcher())
            : new SceneRouter(factory.Object, root.Object, new InlineGameDispatcher(), logger);
    }

    /// <summary>Captures exception identity rather than relying on console output.</summary>
    private sealed class CapturingLogger : ILogger<SceneRouter>
    {
        /// <summary>Exceptions attached to Error-or-higher records.</summary>
        public List<Exception?> Errors { get; } = [];
        /// <inheritdoc />
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel) => true;
        /// <inheritdoc />
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel >= LogLevel.Error)
            {
                Errors.Add(exception);
            }
        }
    }
}
