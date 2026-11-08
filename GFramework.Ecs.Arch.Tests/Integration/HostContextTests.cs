// Copyright (c) 2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Arch.Core;
using Arch.System;
using GFramework.Core.Abstractions.Lifecycle;
using GFramework.Core.Architectures;
using GFramework.Ecs.Arch.Abstractions;
using GFramework.Ecs.Arch.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace GFramework.Ecs.Arch.Tests.Integration;

/// <summary>Async composition must not activate native systems on an arbitrary pipeline continuation thread.</summary>
[TestFixture]
public sealed class HostContextTests
{
    /// <summary>Activation, start, update and failed-start cleanup all use the supplied host context.</summary>
    [TestCase(false)]
    [TestCase(true)]
    public async Task AsyncCompositionUsesTheHostContext(bool failStart)
    {
        using var context = new HostContext();
        var trace = new Trace();
        await using var architecture = new HostArchitecture(context, trace, failStart);
        if (failStart)
            await Assert.ThrowsAsync<ArithmeticException>(() => architecture.InitializeAsync()).ConfigureAwait(false);
        else
        {
            await architecture.InitializeAsync().ConfigureAwait(false);
            var runtime = architecture.Services.GetRequiredService<IArchRuntime>();
            await context.InvokeAsync(() =>
            {
                runtime.Update(1);
                Assert.That(runtime.GetStatistics()!.CompletedFrames, Is.EqualTo(1));
            }).ConfigureAwait(false);
        }
        await architecture.DestroyAsync().ConfigureAwait(false);
        Assert.That(trace.CreatedOn, Is.EqualTo(context.ThreadId));
        Assert.That(trace.StartedOn, Is.EqualTo(context.ThreadId));
        Assert.That(trace.StoppedOn, Is.EqualTo(context.ThreadId));
        Assert.That(trace.Disposals, Is.EqualTo(1));
    }

    /// <summary>A real owner with an async predecessor followed by native ECS activation.</summary>
    private sealed class HostArchitecture(SynchronizationContext context, Trace trace, bool failStart) : Architecture
    {
        /// <inheritdoc />
        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddComponent<AsyncPredecessor>(order: -100);
            services.AddArch(options => options.EnableStatistics = true, hostContext: context);
            services.AddSingleton<ISystem<float>>(provider =>
                new NativeSystem(provider.GetRequiredService<World>(), trace, failStart));
        }
    }

    /// <summary>Forces the pipeline through an asynchronous continuation before the ECS participant.</summary>
    private sealed class AsyncPredecessor : IAsyncInitializable
    {
        /// <inheritdoc />
        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    /// <summary>Records callback threads without moving native access into the test runner.</summary>
    private sealed class Trace
    {
        /// <summary>Constructor thread.</summary>
        public int CreatedOn
        {
            get; set;
        }
        /// <summary>Initialization thread.</summary>
        public int StartedOn
        {
            get; set;
        }
        /// <summary>Business stop thread.</summary>
        public int StoppedOn
        {
            get; set;
        }
        /// <summary>Final disposal count.</summary>
        public int Disposals
        {
            get; set;
        }
    }

    /// <summary>A native system with an optional partial initialization failure.</summary>
    private sealed class NativeSystem : BaseSystem<World, float>, IDestroyable
    {
        private readonly Trace _trace;
        private readonly bool _failStart;

        /// <summary>Records the thread that DI uses to activate the system.</summary>
        public NativeSystem(World world, Trace trace, bool failStart) : base(world)
        {
            _trace = trace;
            _failStart = failStart;
            trace.CreatedOn = Environment.CurrentManagedThreadId;
        }
        /// <inheritdoc />
        public override void Initialize()
        {
            _trace.StartedOn = Environment.CurrentManagedThreadId;
            if (_failStart)
                throw new ArithmeticException("Partial native initialization.");
        }
        /// <inheritdoc />
        public override void Update(in float t)
        {
        }
        /// <inheritdoc />
        public void Destroy() => _trace.StoppedOn = Environment.CurrentManagedThreadId;
        /// <inheritdoc />
        public override void Dispose() => _trace.Disposals++;
    }

    /// <summary>A bounded dedicated-thread host dispatcher that survives until all cleanup has joined.</summary>
    private sealed class HostContext : SynchronizationContext, IDisposable
    {
        private readonly BlockingCollection<(SendOrPostCallback Callback, object? State)> _queue = [];
        private readonly Thread _thread;

        /// <summary>Starts the owning thread; queued work cannot execute on the test runner.</summary>
        public HostContext()
        {
            _thread = new Thread(() =>
            {
                SetSynchronizationContext(this);
                foreach (var (callback, state) in _queue.GetConsumingEnumerable())
                    callback(state);
            });
            _thread.Start();
        }

        /// <summary>The owning host thread identity.</summary>
        public int ThreadId => _thread.ManagedThreadId;
        /// <inheritdoc />
        public override void Post(SendOrPostCallback d, object? state) => _queue.Add((d, state));

        /// <summary>Joins an observed action used to drive a host frame.</summary>
        public Task InvokeAsync(Action action)
        {
            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Post(_ =>
            {
                try
                {
                    action();
                    completion.SetResult();
                }
                catch (Exception error) { completion.SetException(error); }
            }, null);
            return completion.Task;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _queue.CompleteAdding();
            _thread.Join();
            _queue.Dispose();
        }
    }
}
