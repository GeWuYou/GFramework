// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Threading;
using System.Collections.Concurrent;
using GFramework.Game.Runtime;
using GFramework.Game.Abstractions.Setting;
using GFramework.Game.Setting;
using GFramework.Game.Setting.Events;

namespace GFramework.Game.Tests.Setting;

/// <summary>Exercises settings coordination through explicit dependencies and observable notifications.</summary>
[TestFixture]
public sealed class SettingsSystemTests
{
    /// <summary>Independent application failures publish their result while later applicators still run.</summary>
    [Test]
    public async Task ApplyAllAsync_AttemptsEveryApplicator_AndPublishesResults()
    {
        var successful = new Mock<IResetApplyAbleSettings>();
        successful.Setup(value => value.ApplyAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var failing = new Mock<IResetApplyAbleSettings>();
        failing.Setup(value => value.ApplyAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromException(new InvalidOperationException("apply failed")));
        var model = new Mock<ISettingsModel>();
        model.Setup(value => value.AllApplicators()).Returns([successful.Object, failing.Object]);
        var events = new List<object>();
        var system = new SettingsSystem(model.Object, events.Add);

        await Assert.CatchAsync<AggregateException>(() => system.ApplyAllAsync());

        successful.Verify(value => value.ApplyAsync(It.IsAny<CancellationToken>()), Times.Once);
        failing.Verify(value => value.ApplyAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(events.OfType<SettingsApplyingEvent<ISettingsSection>>().Count(), Is.EqualTo(2));
        Assert.That(events.OfType<SettingsAppliedEvent<ISettingsSection>>().Count(value => value.Success), Is.EqualTo(1));
        Assert.That(events.OfType<SettingsAppliedEvent<ISettingsSection>>().Count(value => !value.Success), Is.EqualTo(1));
    }

    /// <summary>Persistence receives the caller token rather than a separate context-based lifetime.</summary>
    [Test]
    public async Task SaveAllAsync_ForwardsTokenToModel()
    {
        using var cancellation = new CancellationTokenSource();
        var model = new Mock<ISettingsModel>();
        model.Setup(value => value.SaveAllAsync(cancellation.Token)).Returns(Task.CompletedTask);
        var system = new SettingsSystem(model.Object);

        await system.SaveAllAsync(cancellation.Token);

        model.Verify(value => value.SaveAllAsync(cancellation.Token), Times.Once);
    }

    /// <summary>Reset completes before applying the changed data.</summary>
    [Test]
    public async Task ResetAllAsync_ResetsBeforeApplication()
    {
        var calls = new List<string>();
        var applicator = new Mock<IResetApplyAbleSettings>();
        applicator.Setup(value => value.ApplyAsync(It.IsAny<CancellationToken>()))
            .Returns(() => { calls.Add("apply"); return Task.CompletedTask; });
        var model = new Mock<ISettingsModel>();
        model.Setup(value => value.ResetAll()).Callback(() => calls.Add("reset"));
        model.Setup(value => value.AllApplicators()).Returns([applicator.Object]);

        await new SettingsSystem(model.Object).ResetAllAsync();

        Assert.That(calls, Is.EqualTo(["reset", "apply"]));
    }

    /// <summary>A missing targeted applicator produces no unrelated application.</summary>
    [Test]
    public async Task ApplyAsync_WithoutMatchingApplicator_DoesNothing()
    {
        var model = new Mock<ISettingsModel>(MockBehavior.Strict);
        model.Setup(value => value.GetApplicator<TestSettings>()).Returns((TestSettings?)null);

        await new SettingsSystem(model.Object).ApplyAsync<TestSettings>();

        model.VerifyAll();
    }

    /// <summary>Cancellation never becomes an ordinary failed-application notification.</summary>
    [Test]
    public async Task ApplyAllAsync_WhenAlreadyCanceled_DoesNotReadModel()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var model = new Mock<ISettingsModel>(MockBehavior.Strict);
        var system = new SettingsSystem(model.Object);

        await Assert.CatchAsync<OperationCanceledException>(() => system.ApplyAllAsync(cancellation.Token));
    }

    /// <summary>Every applicator, including those after an asynchronous one, returns to the explicit host thread.</summary>
    [Test]
    public async Task ApplyAllAsync_UsesHostDispatcherForEveryApplicator()
    {
        using var host = new DedicatedHostContext();
        var first = new Mock<IResetApplyAbleSettings>();
        first.Setup(value => value.ApplyAsync(It.IsAny<CancellationToken>())).Returns(async () =>
        {
            Assert.That(Environment.CurrentManagedThreadId, Is.EqualTo(host.ThreadId));
            await Task.Yield();
            Assert.That(Environment.CurrentManagedThreadId, Is.EqualTo(host.ThreadId));
        });
        var second = new Mock<IResetApplyAbleSettings>();
        second.Setup(value => value.ApplyAsync(It.IsAny<CancellationToken>())).Returns(() =>
        {
            Assert.That(Environment.CurrentManagedThreadId, Is.EqualTo(host.ThreadId));
            return Task.CompletedTask;
        });
        var model = new Mock<ISettingsModel>();
        model.Setup(value => value.AllApplicators()).Returns([first.Object, second.Object]);
        var system = new SettingsSystem(model.Object, dispatcher: new SynchronizationContextGameDispatcher(host));

        await system.ApplyAllAsync();

        first.Verify(value => value.ApplyAsync(It.IsAny<CancellationToken>()), Times.Once);
        second.Verify(value => value.ApplyAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Emulates an engine-owned callback thread while keeping the test independent of an engine package.</summary>
    private sealed class DedicatedHostContext : SynchronizationContext, IDisposable
    {
        private readonly BlockingCollection<Action> _callbacks = [];
        private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly Thread _thread;

        /// <summary>Starts a bounded test callback pump.</summary>
        public DedicatedHostContext()
        {
            _thread = new Thread(Run) { IsBackground = true };
            _thread.Start();
            _ready.Task.GetAwaiter().GetResult();
        }

        /// <summary>Gets the thread which owns all simulated native objects.</summary>
        public int ThreadId => _thread.ManagedThreadId;

        /// <inheritdoc />
        public override void Post(SendOrPostCallback callback, object? state) => _callbacks.Add(() => callback(state));

        /// <summary>Installs the host context before executing queued work.</summary>
        private void Run()
        {
            SetSynchronizationContext(this);
            _ready.TrySetResult();
            foreach (var callback in _callbacks.GetConsumingEnumerable())
                callback();
        }

        /// <summary>Joins the owned host thread after all observed operations have completed.</summary>
        public void Dispose()
        {
            _callbacks.CompleteAdding();
            if (!_thread.Join(TimeSpan.FromSeconds(5)))
                throw new InvalidOperationException("Test host did not stop.");
            _callbacks.Dispose();
        }
    }

    /// <summary>Concrete target type for the generic lookup regression.</summary>
    private sealed class TestSettings : IResetApplyAbleSettings
    {
        /// <inheritdoc />
        public ISettingsData Data => throw new NotSupportedException();
        /// <inheritdoc />
        public Type DataType => typeof(TestSettings);
        /// <inheritdoc />
        public Task ApplyAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        /// <inheritdoc />
        public void Reset()
        {
        }
    }
}
