// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading;
using System.Threading.Tasks;
using GFramework.Core.Ioc;
using Microsoft.Extensions.DependencyInjection;

namespace GFramework.Core.Tests.Ioc;

/// <summary>
///     验证容器只释放它拥有的资源、异步清理可等待，以及标准 DI 构建时的作用域约束。
/// </summary>
[TestFixture]
public sealed class MicrosoftDiContainerOwnershipTests
{
    /// <summary>
    ///     调用者传入的实例即使经所有接口别名解析，也不会转成 provider 拥有的资源。
    /// </summary>
    [Test]
    public async Task ExternalPluralityAliases_Should_Not_Transfer_DisposalOwnership()
    {
        var resource = new ExternalResource();
        var container = new MicrosoftDiContainer();
        container.RegisterPlurality(resource);
        container.Freeze();

        Assert.Multiple(() =>
        {
            Assert.That(container.GetRequired<ExternalResource>(), Is.SameAs(resource));
            Assert.That(container.GetRequired<IPrimaryResource>(), Is.SameAs(resource));
            Assert.That(container.GetRequired<ISecondaryResource>(), Is.SameAs(resource));
            Assert.That(container.GetRequired<IDisposable>(), Is.SameAs(resource));
            Assert.That(container.GetRequired<IAsyncDisposable>(), Is.SameAs(resource));
        });

        await container.DisposeAsync().ConfigureAwait(false);
        Assert.That(resource.DisposeCount, Is.Zero);
        await resource.DisposeAsync().ConfigureAwait(false);
        Assert.That(resource.DisposeCount, Is.EqualTo(1));
    }

    /// <summary>
    ///     不创建会被 Microsoft DI 工厂别名重复捕获的拥有型资源注册。
    /// </summary>
    [Test]
    public void GenericPlurality_Should_Reject_DisposableTypes_BeforeAddingDescriptors()
    {
        using var container = new MicrosoftDiContainer();

        Assert.Multiple(() =>
        {
            var exception = Assert.Throws<InvalidOperationException>(() =>
                container.RegisterPlurality<ExternalResource>());
            Assert.That(exception!.Message, Does.Contain("canonical owning service"));
            Assert.That(container.HasRegistration(typeof(ExternalResource)), Is.False);
            Assert.Throws<InvalidOperationException>(() => container.RegisterPlurality<AsyncResource>());
            Assert.That(container.HasRegistration(typeof(AsyncResource)), Is.False);
        });
    }

    /// <summary>
    ///     无资源拥有责任的泛型别名继续解析同一个标准 DI 单例。
    /// </summary>
    [Test]
    public async Task NonDisposablePlurality_Should_Share_Instance_AcrossAliases()
    {
        using var container = new MicrosoftDiContainer();
        container.RegisterPlurality<SharedService>();
        container.Freeze();

        Assert.Multiple(() =>
        {
            Assert.That(container.GetRequired<IPrimaryResource>(), Is.SameAs(container.GetRequired<SharedService>()));
            Assert.That(container.GetRequired<ISecondaryResource>(), Is.SameAs(container.GetRequired<SharedService>()));
        });
    }

    /// <summary>
    ///     异步释放会等待 async-only 根资源，其他调用共享等待而不是提前返回。
    /// </summary>
    [Test]
    public async Task ConcurrentDisposeAsync_Should_WaitFor_One_OwnedResourceCleanup()
    {
        var container = new MicrosoftDiContainer();
        container.RegisterSingleton<AsyncResource, AsyncResource>();
        container.Freeze();
        var resource = container.GetRequired<AsyncResource>();

        var first = container.DisposeAsync().AsTask();
        await resource.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        var second = container.DisposeAsync().AsTask();
        try
        {
            Assert.Multiple(() =>
            {
                Assert.That(first.IsCompleted, Is.False);
                Assert.That(second.IsCompleted, Is.False);
                Assert.That(second, Is.SameAs(first));
                Assert.Throws<ObjectDisposedException>(() => container.GetRequired<AsyncResource>());
            });
        }
        finally
        {
            resource.Release.TrySetResult(null);
        }

        await Task.WhenAll(first, second).ConfigureAwait(false);
        await container.DisposeAsync().ConfigureAwait(false);
        Assert.That(resource.DisposeCount, Is.EqualTo(1));
    }

    /// <summary>
    ///     同步调用在已有异步清理进行时等待同一操作。
    /// </summary>
    [Test]
    public async Task Dispose_During_AsyncCleanup_Should_Wait_ForCompletion()
    {
        var container = new MicrosoftDiContainer();
        container.RegisterSingleton<AsyncResource, AsyncResource>();
        container.Freeze();
        var resource = container.GetRequired<AsyncResource>();

        var cleanup = container.DisposeAsync().AsTask();
        await resource.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        var entered = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var syncCleanup = Task.Run(() =>
        {
            entered.TrySetResult(null);
            container.Dispose();
        });
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

        try
        {
            Assert.That(syncCleanup.IsCompleted, Is.False);
        }
        finally
        {
            resource.Release.TrySetResult(null);
        }

        await Task.WhenAll(cleanup, syncCleanup).ConfigureAwait(false);
        Assert.That(resource.DisposeCount, Is.EqualTo(1));
    }

    /// <summary>
    ///     失败的资源清理同样只执行一次，所有后续调用观察同一失败结果。
    /// </summary>
    [Test]
    public async Task DisposeAsync_Failure_Should_Be_Shared_Without_RepeatingCleanup()
    {
        var container = new MicrosoftDiContainer();
        container.RegisterSingleton<FailingResource, FailingResource>();
        container.Freeze();
        var resource = container.GetRequired<FailingResource>();

        var first = container.DisposeAsync().AsTask();
        var second = container.DisposeAsync().AsTask();
        Assert.That(second, Is.SameAs(first));
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await first.ConfigureAwait(false));
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await second.ConfigureAwait(false));
        Assert.That(resource.DisposeCount, Is.EqualTo(1));
    }

    /// <summary>
    ///     重复冻结不会替换根 provider 并重新创建它拥有的单例。
    /// </summary>
    [Test]
    public void RepeatedFreeze_Should_Preserve_ResolvedSingletonIdentity()
    {
        using var container = new MicrosoftDiContainer();
        container.RegisterSingleton<SharedService, SharedService>();
        container.Freeze();
        var original = container.GetRequired<SharedService>();

        container.Freeze();

        Assert.That(container.GetRequired<SharedService>(), Is.SameAs(original));
    }

    /// <summary>
    ///     冻结时验证 Singleton 对 Scoped 的捕获，不推迟到运行时才暴露错误。
    /// </summary>
    [Test]
    public void Freeze_Should_Reject_Singleton_CapturingScopedDependency()
    {
        using var container = new MicrosoftDiContainer();
        container.RegisterScoped<ScopedDependency, ScopedDependency>();
        container.RegisterSingleton<CaptiveService, CaptiveService>();

        Assert.Throws<AggregateException>(container.Freeze);
    }

    /// <summary>
    ///     注册作用域对象后，只有显式 scope 可以解析它。
    /// </summary>
    [Test]
    public void RootResolution_Should_Reject_ScopedDependency()
    {
        using var container = new MicrosoftDiContainer();
        container.RegisterScoped<ScopedDependency, ScopedDependency>();
        container.Freeze();

        Assert.Throws<InvalidOperationException>(() => container.GetRequired<ScopedDependency>());
        using var scope = container.CreateScope();
        Assert.That(scope.ServiceProvider.GetRequiredService<ScopedDependency>(), Is.Not.Null);
    }

    private interface IPrimaryResource;

    private interface ISecondaryResource;

    private sealed class ExternalResource : IPrimaryResource, ISecondaryResource, IDisposable, IAsyncDisposable
    {
        /// <summary>
        ///     已执行的资源释放次数。
        /// </summary>
        public int DisposeCount { get; private set; }

        /// <summary>
        ///     记录同步资源释放。
        /// </summary>
        public void Dispose()
        {
            DisposeCount++;
        }

        /// <summary>
        ///     记录异步资源释放。
        /// </summary>
        /// <returns>已完成的释放操作。</returns>
        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class SharedService : IPrimaryResource, ISecondaryResource
    {
        /// <summary>
        ///     提供可由标准 DI 激活的构造函数。
        /// </summary>
        public SharedService()
        {
        }
    }

    private sealed class AsyncResource : IAsyncDisposable
    {
        private int _disposeCount;

        /// <summary>
        ///     提供可由标准 DI 激活的构造函数。
        /// </summary>
        public AsyncResource()
        {
        }

        /// <summary>
        ///     资源已进入清理阶段的信号。
        /// </summary>
        public TaskCompletionSource<object?> Entered { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>
        ///     允许资源完成清理的信号。
        /// </summary>
        public TaskCompletionSource<object?> Release { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>
        ///     实际执行清理的次数。
        /// </summary>
        public int DisposeCount => Volatile.Read(ref _disposeCount);

        /// <summary>
        ///     等待测试明确允许资源完成释放。
        /// </summary>
        /// <returns>释放操作。</returns>
        public async ValueTask DisposeAsync()
        {
            Interlocked.Increment(ref _disposeCount);
            Entered.TrySetResult(null);
            await Release.Task.ConfigureAwait(false);
        }
    }

    private sealed class FailingResource : IAsyncDisposable
    {
        /// <summary>
        ///     提供可由标准 DI 激活的构造函数。
        /// </summary>
        public FailingResource()
        {
        }

        /// <summary>
        ///     实际执行资源清理的次数。
        /// </summary>
        public int DisposeCount { get; private set; }

        /// <summary>
        ///     生成需要向每个清理等待者传递的失败。
        /// </summary>
        /// <returns>失败的资源清理操作。</returns>
        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            return ValueTask.FromException(new InvalidOperationException("Resource cleanup failed."));
        }
    }

    private sealed class ScopedDependency
    {
        /// <summary>
        ///     提供可由标准 DI 激活的构造函数。
        /// </summary>
        public ScopedDependency()
        {
        }
    }

    private sealed class CaptiveService
    {
        /// <summary>
        ///     构造不能被 Singleton 捕获的作用域依赖。
        /// </summary>
        /// <param name="dependency">应由独立 scope 解析的依赖。</param>
        public CaptiveService(ScopedDependency dependency)
        {
            _ = dependency;
        }
    }
}
