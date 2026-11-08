// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Threading;
using GFramework.Core.Abstractions.Pause;
using GFramework.Core.Pause;
using GFramework.Game.Abstractions.Enums;
using GFramework.Game.Abstractions.Scene;
using GFramework.Game.Runtime;
using GFramework.Game.Scene;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GFramework.Game.Tests.Runtime;

/// <summary>Exercises the official Game composition path with a host-owned standard logging factory.</summary>
[TestFixture]
public sealed class GameLoggingCompositionTests
{
    /// <summary>Default Game pause registration forwards the host's typed logger without changing its public constructor.</summary>
    [Test]
    public async Task AddGamePause_ForwardsHostLoggingToDefaultManager()
    {
        var output = new CapturingProvider();
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace).AddProvider(output));
        services.AddGamePause();
        services.AddGamePause();
        var provider = services.BuildServiceProvider();
        await using (provider.ConfigureAwait(false))
        {
            var pause = provider.GetRequiredService<IPauseStackManager>();
            var token = pause.Push("host-logging-probe");
            Assert.That(output.Records.Exists(record =>
                string.Equals(record.Category, typeof(PauseStackManager).FullName, StringComparison.Ordinal) &&
                record.Message.Contains("host-logging-probe", StringComparison.Ordinal)), Is.True);
            Assert.That(services.Count(descriptor => descriptor.ServiceType == typeof(IPauseStackManager)), Is.EqualTo(1));
            pause.Pop(token);
        }
    }

    /// <summary>The official scene factory forwards logging into the private pipeline instead of discarding continued failures.</summary>
    [Test]
    public async Task AddGameScenes_ForwardsHostLoggingToTransitionPipeline()
    {
        var output = new CapturingProvider();
        var failure = new InvalidOperationException("continued scene extension failure");
        var target = new Mock<ISceneBehavior>();
        target.SetupGet(value => value.Key).Returns("target");
        var factory = new Mock<ISceneFactory>();
        factory.Setup(value => value.Create("target")).Returns(target.Object);
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace).AddProvider(output));
        services.AddGameScenes(factory.Object, new Mock<ISceneRoot>().Object, new InlineGameDispatcher());
        var provider = services.BuildServiceProvider();
        await using (provider.ConfigureAwait(false))
        {
            var router = (SceneRouter)provider.GetRequiredService<ISceneRouter>();
            var handler = new Mock<ISceneTransitionHandler>();
            handler.SetupGet(value => value.Phases).Returns(SceneTransitionPhases.BeforeChange);
            handler.Setup(value => value.ShouldHandle(It.IsAny<SceneTransitionEvent>(), It.IsAny<SceneTransitionPhases>())).Returns(value: true);
            handler.Setup(value => value.HandleAsync(It.IsAny<SceneTransitionEvent>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException(failure));
            router.RegisterHandler(handler.Object, new SceneTransitionHandlerOptions { ContinueOnError = true });
            await router.PushAsync("target").ConfigureAwait(false);
            Assert.That(output.Records.Exists(record => ReferenceEquals(record.Error, failure)), Is.True);
        }
    }

    /// <summary>Records actual Microsoft logging dispatch without console or native engine dependencies.</summary>
    private sealed class CapturingProvider : ILoggerProvider
    {
        /// <summary>Records category, formatted text and attached exception identity.</summary>
        public List<(string Category, string Message, Exception? Error)> Records { get; } = [];
        /// <inheritdoc />
        public ILogger CreateLogger(string categoryName) => new CaptureLogger(this, categoryName);
        /// <inheritdoc />
        public void Dispose()
        {
        }

        /// <summary>Stores the formatted message emitted by the configured host factory.</summary>
        private sealed class CaptureLogger(CapturingProvider owner, string category) : ILogger
        {
            /// <inheritdoc />
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            /// <inheritdoc />
            public bool IsEnabled(LogLevel logLevel) => true;
            /// <inheritdoc />
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter) => owner.Records.Add((category, formatter(state, exception), exception));
        }
    }
}
