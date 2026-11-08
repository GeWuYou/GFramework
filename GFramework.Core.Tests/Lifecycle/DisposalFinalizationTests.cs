// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using GFramework.Core.Architectures;
using GFramework.Core.Ioc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace GFramework.Core.Tests.Lifecycle;

/// <summary>
///     验证只有成功的规范清理过程可以抑制派生终结器，失败和同链重入不能提前抑制。
/// </summary>
[TestFixture]
[NonParallelizable]
public sealed class DisposalFinalizationTests
{
    /// <summary>架构清理成功抑制终结器，清理失败则保留它；未释放实例作为同轮 GC 正控。</summary>
    /// <param name="failCleanup">正式 provider 拥有的资源是否清理失败。</param>
    [TestCase(false)]
    [TestCase(true)]
    public async Task Architecture_Finalization_Should_Follow_Canonical_Cleanup_Result(bool failCleanup)
    {
        var finalized = 0;
        var cleaned = 0;
        var controlFinalized = 0;
        var controlCleaned = 0;
        var reference = await CreateArchitectureAsync(
            () => Interlocked.Increment(ref finalized), () => Interlocked.Increment(ref cleaned),
            dispose: true, failCleanup).ConfigureAwait(false);
        var control = await CreateArchitectureAsync(
            () => Interlocked.Increment(ref controlFinalized), () => Interlocked.Increment(ref controlCleaned),
            dispose: false, failCleanup: false).ConfigureAwait(false);

        await CollectFinalizersUntilUnreachableAsync(reference, control).ConfigureAwait(false);

        Assert.That(reference.IsAlive, Is.False, "The disposed probe must actually become unreachable.");
        Assert.That(control.IsAlive, Is.False, "The non-disposed control must actually become unreachable.");
        Assert.That(Volatile.Read(ref controlFinalized), Is.EqualTo(1));
        Assert.That(Volatile.Read(ref controlCleaned), Is.Zero);
        Assert.That(Volatile.Read(ref cleaned), Is.EqualTo(1));
        Assert.That(Volatile.Read(ref finalized), Is.EqualTo(failCleanup ? 1 : 0));
    }

    /// <summary>同步和异步容器入口遵循同一个成功清理屏障，失败时保留派生终结器。</summary>
    /// <param name="asyncDispose">是否使用异步入口。</param>
    /// <param name="failCleanup">provider 拥有的资源是否清理失败。</param>
    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public async Task Container_Finalization_Should_Follow_Canonical_Cleanup_Result(bool asyncDispose, bool failCleanup)
    {
        var finalized = 0;
        var cleaned = 0;
        var controlFinalized = 0;
        var controlCleaned = 0;
        var reference = asyncDispose
            ? await CreateContainerAsync(() => Interlocked.Increment(ref finalized),
                () => Interlocked.Increment(ref cleaned), failCleanup).ConfigureAwait(false)
            : CreateContainerSync(() => Interlocked.Increment(ref finalized),
                () => Interlocked.Increment(ref cleaned), dispose: true, failCleanup);
        var control = CreateContainerSync(() => Interlocked.Increment(ref controlFinalized),
            () => Interlocked.Increment(ref controlCleaned), dispose: false, failCleanup: false);

        await CollectFinalizersUntilUnreachableAsync(reference, control).ConfigureAwait(false);

        Assert.That(reference.IsAlive, Is.False, "The disposed probe must actually become unreachable.");
        Assert.That(control.IsAlive, Is.False, "The non-disposed control must actually become unreachable.");
        Assert.That(Volatile.Read(ref controlFinalized), Is.EqualTo(1));
        Assert.That(Volatile.Read(ref controlCleaned), Is.Zero);
        Assert.That(Volatile.Read(ref cleaned), Is.EqualTo(1));
        Assert.That(Volatile.Read(ref finalized), Is.EqualTo(failCleanup ? 1 : 0));
    }

    /// <summary>同链同步 Dispose 的 no-op 不得在仍进行中的异步清理最终失败前抑制终结器。</summary>
    [Test]
    public async Task Reentrant_Sync_Dispose_Should_Not_Suppress_Finalization_Before_Async_Failure()
    {
        var finalized = 0;
        var reference = await CreateReentrantContainerAsync(() => Interlocked.Increment(ref finalized))
            .ConfigureAwait(false);

        await CollectFinalizersUntilUnreachableAsync(reference).ConfigureAwait(false);

        Assert.That(reference.IsAlive, Is.False, "The failed cleanup probe must actually become unreachable.");
        Assert.That(Volatile.Read(ref finalized), Is.EqualTo(1));
    }

    // Keep probes out of the assertion stack: an alive JIT local must not masquerade as successful suppression.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<WeakReference> CreateArchitectureAsync(Action onFinalized, Action onCleaned,
        bool dispose, bool failCleanup)
    {
        var architecture = new FinalizableArchitecture(onFinalized, onCleaned, failCleanup);
        await architecture.InitializeAsync().ConfigureAwait(false);
        _ = architecture.Context.GetService<CleanupResource>();
        if (dispose)
        {
            if (failCleanup)
            {
                await Assert.ThrowsAsync<AggregateException>(async () =>
                    await architecture.DisposeAsync().ConfigureAwait(false)).ConfigureAwait(false);
            }
            else
            {
                await architecture.DisposeAsync().ConfigureAwait(false);
            }
        }
        return new WeakReference(architecture);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateContainerSync(Action onFinalized, Action onCleaned, bool dispose, bool failCleanup)
    {
        var container = BuildContainerProbe(onFinalized, onCleaned, failCleanup);
        if (dispose)
        {
            if (failCleanup)
            {
                Assert.Throws<InvalidOperationException>(container.Dispose);
            }
            else
            {
                container.Dispose();
            }
        }
        return new WeakReference(container);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<WeakReference> CreateContainerAsync(Action onFinalized, Action onCleaned, bool failCleanup)
    {
        var container = BuildContainerProbe(onFinalized, onCleaned, failCleanup);
        if (failCleanup)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await container.DisposeAsync().ConfigureAwait(false)).ConfigureAwait(false);
        }
        else
        {
            await container.DisposeAsync().ConfigureAwait(false);
        }
        return new WeakReference(container);
    }

    private static FinalizableContainer BuildContainerProbe(Action onFinalized, Action onCleaned, bool failCleanup)
    {
        var container = new FinalizableContainer(onFinalized);
        container.GetServicesUnsafe.AddSingleton(_ => new CleanupResource(onCleaned, failCleanup));
        container.Freeze();
        _ = container.GetRequired<CleanupResource>();
        return container;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<WeakReference> CreateReentrantContainerAsync(Action onFinalized)
    {
        var container = new FinalizableContainer(onFinalized);
        var resource = new ReentrantAsyncResource(container.Dispose);
        container.GetServicesUnsafe.AddSingleton(_ => resource);
        container.Freeze();
        _ = container.GetRequired<ReentrantAsyncResource>();
        var cleanup = container.DisposeAsync().AsTask();
        await resource.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        try
        {
            Assert.That(cleanup.IsCompleted, Is.False, "Reentrant Dispose returned while canonical cleanup is still pending.");
        }
        finally
        {
            resource.Release.TrySetResult();
        }
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await cleanup.ConfigureAwait(false)).ConfigureAwait(false);
        return new WeakReference(container);
    }

    /// <summary>
    ///     等待完成任务的生产者退出其 finally 和调用栈，再判断弱引用是否仍被永久持有。
    /// </summary>
    private static async Task CollectFinalizersUntilUnreachableAsync(params WeakReference[] references)
    {
        var elapsed = Stopwatch.StartNew();
        do
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            if (!Array.Exists(references, static reference => reference.IsAlive))
            {
                return;
            }

            // Canonical completion can wake this assertion before the producer's finally drops its owner reference.
            // Yield rather than repeatedly collecting on that same still-live stack; no strong Target is read here.
            await Task.Delay(TimeSpan.FromMilliseconds(10), CancellationToken.None).ConfigureAwait(false);
        }
        while (elapsed.Elapsed < TimeSpan.FromSeconds(5));
    }

    /// <summary>只观察终结器，不在终结线程启动架构清理或访问外部资源。</summary>
    /// <param name="onFinalized">终结器观察。</param>
    /// <param name="onCleaned">资源清理观察。</param>
    /// <param name="failCleanup">资源清理是否失败。</param>
    private sealed class FinalizableArchitecture(Action onFinalized, Action onCleaned, bool failCleanup)
        : Architecture(CreateConfiguration())
    {
        /// <summary>仅通过 factory 注册受 provider 拥有的资源。</summary>
        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(_ => new CleanupResource(onCleaned, failCleanup));
        }

#pragma warning disable MA0055 // A real derived finalizer is needed to test successful suppression and failure retention.
        /// <summary>记录未被成功清理抑制的终结器执行。</summary>
        ~FinalizableArchitecture()
        {
            onFinalized();
        }
#pragma warning restore MA0055

        private static ArchitectureConfiguration CreateConfiguration()
        {
            var configuration = new ArchitectureConfiguration();
            configuration.LoggerProperties.LoggerFactory = NullLoggerFactory.Instance;
            return configuration;
        }
    }

    /// <summary>不创建外部句柄的可终结容器探针。</summary>
    /// <param name="onFinalized">终结器观察。</param>
    private sealed class FinalizableContainer(Action onFinalized) : MicrosoftDiContainer
    {
#pragma warning disable MA0055 // A real derived finalizer verifies canonical cleanup rather than merely checking source calls.
        /// <summary>只记录终结器执行，不从终结线程调用容器 API。</summary>
        ~FinalizableContainer()
        {
            onFinalized();
        }
#pragma warning restore MA0055
    }

    /// <summary>记录 provider 的实际清理责任，并可模拟失败。</summary>
    /// <param name="onCleaned">清理观察。</param>
    /// <param name="failCleanup">是否抛出预期清理故障。</param>
    private sealed class CleanupResource(Action onCleaned, bool failCleanup) : IDisposable
    {
        /// <inheritdoc />
        public void Dispose()
        {
            onCleaned();
            if (failCleanup)
            {
                throw new InvalidOperationException("Expected owned resource cleanup failure.");
            }
        }
    }

    /// <summary>先同链重入，再等待显式释放并失败，稳定暴露过早抑制终结器的问题。</summary>
    private sealed class ReentrantAsyncResource(Action reenter) : IAsyncDisposable
    {
        private Action? _reenter = reenter;

        /// <summary>重入调用已返回、规范清理仍等待的边界。</summary>
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>由测试释放规范清理的异步等待。</summary>
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            _reenter!();
            _reenter = null;
            Entered.TrySetResult();
            await Release.Task.ConfigureAwait(false);
            throw new InvalidOperationException("Expected failure after same-chain reentrant Dispose.");
        }
    }
}
