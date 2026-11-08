// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using GFramework.Core.Architectures;
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

    // Keep probes out of the assertion stack: an alive JIT local must not masquerade as successful suppression.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<WeakReference> CreateArchitectureAsync(Action onFinalized, Action onCleaned,
        bool dispose, bool failCleanup)
    {
        var architecture = new FinalizableArchitecture(onFinalized, onCleaned, failCleanup);
        await architecture.InitializeAsync().ConfigureAwait(false);
        _ = architecture.Services.GetRequiredService<CleanupResource>();
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

}
