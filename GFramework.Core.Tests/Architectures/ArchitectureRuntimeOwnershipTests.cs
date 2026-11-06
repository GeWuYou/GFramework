// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading;
using System.Threading.Tasks;
using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Enums;
using GFramework.Core.Abstractions.Rule;
using GFramework.Core.Architectures;
using Microsoft.Extensions.Logging;
using GFramework.Core.Systems;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace GFramework.Core.Tests.Architectures;

/// <summary>验证唯一 provider、实例隔离、自动失败清理及并发销毁的可观察资源责任。</summary>
[TestFixture]
public sealed class ArchitectureRuntimeOwnershipTests
{
    /// <summary>类型构造与 Ready 后读取必须复用同一个正式 Singleton。</summary>
    [Test]
    public async Task Singleton_Should_Preserve_Identity_Across_Activation_And_Ready()
    {
        var constructions = 0;
        var architecture = new OwnedArchitecture((owner, services) =>
        {
            services.AddSingleton(_ =>
            {
                constructions++;
                return new OwnedDependency();
            });
            owner.RegisterSystem<DependencySystem>();
        });
        await architecture.InitializeAsync().ConfigureAwait(false);
        var dependency = architecture.Context.GetService<OwnedDependency>();
        Assert.Multiple(() =>
        {
            Assert.That(constructions, Is.EqualTo(1));
            Assert.That(architecture.Context.GetSystem<DependencySystem>().Dependency, Is.SameAs(dependency));
        });
        await architecture.DestroyAsync().ConfigureAwait(false);
        Assert.That(dependency.DisposeCalls, Is.EqualTo(1));
    }

    /// <summary>上下文不得将 Transient 缓存为单例；Scoped 不得从根上下文解析。</summary>
    [Test]
    public async Task Context_Should_Honor_Standard_Service_Lifetimes()
    {
        var architecture = new OwnedArchitecture((_, services) =>
        {
            services.AddTransient<TransientMarker>();
            services.AddScoped<ScopedMarker>();
        });
        await architecture.InitializeAsync().ConfigureAwait(false);
        Assert.That(architecture.Context.GetService<TransientMarker>(),
            Is.Not.SameAs(architecture.Context.GetService<TransientMarker>()));
        Assert.Throws<InvalidOperationException>(() => architecture.Context.GetService<ScopedMarker>());
        await architecture.DestroyAsync().ConfigureAwait(false);
    }

    /// <summary>两个相同架构类型同时运行，销毁其中一个不能污染另一个的上下文与日志。</summary>
    [Test]
    public async Task Two_Architectures_Should_Isolate_Context_And_Log_Factory()
    {
        using var factoryA = LoggerFactory.Create(static _ => { });
        using var factoryB = LoggerFactory.Create(static _ => { });
        var a = new OwnedArchitecture((_, services) => services.AddSingleton(new Marker("A")), factoryA);
        var b = new OwnedArchitecture((_, services) => services.AddSingleton(new Marker("B")), factoryB);
        await a.InitializeAsync().ConfigureAwait(false);
        await b.InitializeAsync().ConfigureAwait(false);
        Assert.Multiple(() =>
        {
            Assert.That(a.Context.GetService<Marker>().Name, Is.EqualTo("A"));
            Assert.That(b.Context.GetService<Marker>().Name, Is.EqualTo("B"));
            Assert.That(a.Context.GetService<ILoggerFactory>(), Is.SameAs(factoryA));
            Assert.That(b.Context.GetService<ILoggerFactory>(), Is.SameAs(factoryB));
        });
        var oldContext = a.Context;
        await a.DestroyAsync().ConfigureAwait(false);
        Assert.Throws<ObjectDisposedException>(() => oldContext.GetService<Marker>());
        Assert.That(b.Context.GetService<Marker>().Name, Is.EqualTo("B"));
        await b.DestroyAsync().ConfigureAwait(false);
    }

    /// <summary>外部组件参与业务停止，但框架不接管它的 Dispose。</summary>
    [Test]
    public async Task Borrowed_Component_Should_Stop_Without_Transferring_Resource_Ownership()
    {
        var component = new BorrowedSystem();
        var architecture = new OwnedArchitecture((owner, _) => owner.RegisterSystem(component));
        await architecture.InitializeAsync().ConfigureAwait(false);
        await architecture.DestroyAsync().ConfigureAwait(false);
        Assert.Multiple(() =>
        {
            Assert.That(component.StopCalls, Is.EqualTo(1));
            Assert.That(component.DisposeCalls, Is.Zero);
        });
        component.Dispose();
    }

    /// <summary>宿主启动失败后先自动停止组件、释放根资源，再返回失败，之后不重复清理。</summary>
    [Test]
    public async Task Failed_Startup_Should_Cleanup_Before_Returning()
    {
        var dependency = new OwnedDependency();
        var component = new BorrowedSystem();
        var architecture = new OwnedArchitecture((owner, services) =>
        {
            services.AddSingleton(_ => dependency);
            owner.RegisterSystem(component);
        }) { Started = () => throw new InvalidOperationException("host failed") };
        architecture.Started = () =>
        {
            _ = architecture.Context.GetService<OwnedDependency>();
            throw new InvalidOperationException("host failed");
        };
        Assert.ThrowsAsync<InvalidOperationException>(() => architecture.InitializeAsync());
        Assert.That(architecture.CurrentPhase, Is.EqualTo(ArchitecturePhase.FailedInitialization));
        Assert.That(component.StopCalls, Is.EqualTo(1));
        Assert.That(dependency.DisposeCalls, Is.EqualTo(1));
        Assert.ThrowsAsync<InvalidOperationException>(() => architecture.WaitUntilReadyAsync());
        Assert.Throws<InvalidOperationException>(() => architecture.InitializeAsync());
        await architecture.DestroyAsync().ConfigureAwait(false);
        Assert.That(component.StopCalls, Is.EqualTo(1));
        Assert.That(dependency.DisposeCalls, Is.EqualTo(1));
    }

    /// <summary>并发销毁必须等待同一次异步释放，停止开始后外部旧 Context 立即失效。</summary>
    [Test]
    public async Task Concurrent_Destroy_Should_Wait_For_One_Async_Cleanup()
    {
        var resource = new BlockingAsyncResource();
        var architecture = new OwnedArchitecture((_, services) => services.AddSingleton(_ => resource));
        await architecture.InitializeAsync().ConfigureAwait(false);
        _ = architecture.Context.GetService<BlockingAsyncResource>();
        var first = architecture.DestroyAsync().AsTask();
        await resource.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        var second = architecture.DestroyAsync().AsTask();
        Assert.That(second.IsCompleted, Is.False);
        Assert.Throws<ObjectDisposedException>(() => architecture.Context.GetService<BlockingAsyncResource>());
        resource.Release.TrySetResult();
        await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        Assert.That(resource.DisposeCalls, Is.EqualTo(1));
    }

    /// <summary>取消异步阶段之后资源先释放，Ready 等待者也必须以取消完成。</summary>
    [Test]
    public async Task Canceled_Stage_Should_Cleanup_And_Complete_Ready_Waiter()
    {
        var resource = new OwnedDependency();
        var hook = new CancelableHook();
        var architecture = new OwnedArchitecture((owner, services) =>
        {
            services.AddSingleton(_ => resource);
            owner.RegisterLifecycleHook(hook);
        });
        using var cancellation = new CancellationTokenSource();
        var startup = architecture.InitializeAsync(cancellation.Token);
        await hook.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        _ = architecture.Context.GetService<OwnedDependency>();
        cancellation.Cancel();
        Assert.That(async () => await startup.ConfigureAwait(false), Throws.InstanceOf<OperationCanceledException>());
        Assert.That(async () => await architecture.WaitUntilReadyAsync().ConfigureAwait(false), Throws.InstanceOf<OperationCanceledException>());
        Assert.That(resource.DisposeCalls, Is.EqualTo(1));
        await architecture.DestroyAsync().ConfigureAwait(false);
    }

    /// <summary>取消回调抛错不能跳过根 provider 清理；错误在清理完成后报告。</summary>
    [Test]
    public async Task Throwing_Cancellation_Callback_Should_Not_Skip_Destruction()
    {
        var resource = new OwnedDependency();
        var hook = new CancelableHook { ThrowOnCancellation = true };
        var architecture = new OwnedArchitecture((owner, services) =>
        {
            services.AddSingleton(_ => resource);
            owner.RegisterLifecycleHook(hook);
        });
        var startup = architecture.InitializeAsync();
        await hook.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        _ = architecture.Context.GetService<OwnedDependency>();
        Assert.That(async () => await architecture.DestroyAsync().ConfigureAwait(false), Throws.InstanceOf<AggregateException>());
        Assert.That(async () => await startup.ConfigureAwait(false), Throws.InstanceOf<OperationCanceledException>());
        Assert.That(resource.DisposeCalls, Is.EqualTo(1));
        Assert.That(architecture.CurrentPhase, Is.EqualTo(ArchitecturePhase.Destroyed));
    }

    /// <summary>清理回调等待自身销毁会明确拒绝，不得与共享等待任务形成死锁。</summary>
    [Test]
    public async Task Reentrant_Destroy_Should_Reject_The_Same_Logical_Operation()
    {
        var component = new BorrowedSystem();
        var architecture = new OwnedArchitecture((owner, _) => owner.RegisterSystem(component));
        component.Stop = () => Assert.Throws<InvalidOperationException>(() =>
        {
            _ = architecture.DestroyAsync();
        });
        await architecture.InitializeAsync().ConfigureAwait(false);
        await architecture.DestroyAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        Assert.That(component.StopCalls, Is.EqualTo(1));
    }

    /// <summary>同步入口不得执行异步钩子，失败后仍使实例终止并禁止重试。</summary>
    [Test]
    public async Task Sync_Startup_Should_Reject_Async_Hooks()
    {
        var architecture = new OwnedArchitecture((owner, _) => owner.RegisterLifecycleHook(new CancelableHook()));
        Assert.Throws<InvalidOperationException>(() => architecture.Initialize());
        Assert.That(architecture.CurrentPhase, Is.EqualTo(ArchitecturePhase.FailedInitialization));
        await architecture.DestroyAsync().ConfigureAwait(false);
    }

    /// <summary>异步 Ready 钩子完成前不能把阶段已进入误报为运行时完全就绪。</summary>
    [Test]
    public async Task Readiness_Should_Wait_For_Ready_Phase_Hooks()
    {
        var hook = new ReadyBarrierHook();
        var architecture = new OwnedArchitecture((owner, _) => owner.RegisterLifecycleHook(hook));
        var startup = architecture.InitializeAsync();
        await hook.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        Assert.That(architecture.CurrentPhase, Is.EqualTo(ArchitecturePhase.Ready));
        Assert.That(architecture.IsReady, Is.False);
        Assert.That(architecture.WaitUntilReadyAsync().IsCompleted, Is.False);
        hook.Release.TrySetResult();
        await startup.ConfigureAwait(false);
        Assert.That(architecture.IsReady, Is.True);
        await architecture.DestroyAsync().ConfigureAwait(false);
    }

    /// <summary>测试用架构只在配置阶段声明依赖，按实际宿主钩子触发成功或故障。</summary>
    private sealed class OwnedArchitecture : Architecture
    {
        private readonly Action<OwnedArchitecture, IServiceCollection> _configure;

        /// <summary>创建使用独立日志和服务集合的架构。</summary>
        /// <param name="configure">配置描述回调。</param>
        /// <param name="factory">实例日志工厂。</param>
        public OwnedArchitecture(
            Action<OwnedArchitecture, IServiceCollection> configure,
            ILoggerFactory? factory = null)
            : base(configuration: CreateConfiguration(factory))
        {
            _configure = configure;
        }

        /// <summary>Ready 前的同步宿主行为。</summary>
        public Action? Started { get; set; }

        /// <summary>仅记录服务描述。</summary>
        /// <param name="services">当前正式集合。</param>
        protected override void ConfigureServices(IServiceCollection services) => _configure(this, services);

        /// <summary>注入测试所需的宿主行为。</summary>
        protected override void OnStarted() => Started?.Invoke();

        /// <summary>构建本测试独立的实例日志配置。</summary>
        /// <param name="factory">显式工厂。</param>
        /// <returns>配置。</returns>
        private static ArchitectureConfiguration CreateConfiguration(ILoggerFactory? factory)
        {
            var result = new ArchitectureConfiguration();
            if (factory != null)
                result.LoggerProperties.LoggerFactory = factory;
            return result;
        }
    }

    /// <summary>由 provider 创建和释放的计数依赖。</summary>
    private sealed class OwnedDependency : IDisposable
    {
        /// <summary>已观察到的资源释放次数。</summary>
        public int DisposeCalls { get; private set; }

        /// <summary>只记录资源责任，不使用外部资源。</summary>
        public void Dispose() => DisposeCalls++;
    }

    /// <summary>证明构造注入与上下文解析的实例身份。</summary>
    /// <param name="dependency">正式 provider 解析的依赖。</param>
    private sealed class DependencySystem(OwnedDependency dependency) : AbstractSystem
    {
        /// <summary>构造时得到的依赖。</summary>
        public OwnedDependency Dependency { get; } = dependency;

        /// <summary>该测试只关注激活与所有权。</summary>
        protected override void OnInit() { }

        /// <summary>业务停止不直接释放其 DI 拥有的依赖。</summary>
        protected override void OnDestroy() { }
    }

    /// <summary>外部组件以业务停止和 Dispose 两个计数区分责任。</summary>
    private sealed class BorrowedSystem : AbstractSystem, IDisposable
    {
        /// <summary>业务停止次数。</summary>
        public int StopCalls { get; private set; }

        /// <summary>外部资源释放次数。</summary>
        public int DisposeCalls { get; private set; }

        /// <summary>可选停止动作。</summary>
        public Action? Stop { get; set; }

        /// <summary>无异步工作。</summary>
        protected override void OnInit() { }

        /// <summary>业务停止与外部 Dispose 保持分离。</summary>
        protected override void OnDestroy()
        {
            StopCalls++;
            Stop?.Invoke();
        }

        /// <summary>由外部拥有者调用。</summary>
        public void Dispose() => DisposeCalls++;
    }

    /// <summary>记录实例隔离。</summary>
    /// <param name="Name">实例名称。</param>
    private sealed record Marker(string Name);

    /// <summary>Transient 的实例标记。</summary>
    private sealed class TransientMarker;

    /// <summary>Scoped 的实例标记。</summary>
    private sealed class ScopedMarker;

    /// <summary>显式 barrier 使并发测试不依赖任意延时。</summary>
    private sealed class BlockingAsyncResource : IAsyncDisposable
    {
        /// <summary>释放已经开始。</summary>
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>允许资源完成释放。</summary>
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>释放调用次数。</summary>
        public int DisposeCalls { get; private set; }

        /// <summary>等待测试释放 barrier。</summary>
        /// <returns>已完整释放。</returns>
        public async ValueTask DisposeAsync()
        {
            DisposeCalls++;
            Entered.TrySetResult();
            await Release.Task.ConfigureAwait(false);
        }
    }

    /// <summary>可取消的真实阶段等待，其他阶段无异步工作。</summary>
    private sealed class CancelableHook : IAsyncArchitectureLifecycleHook
    {
        /// <summary>Utility 前阶段已经进入。</summary>
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>模拟不遵守约定的用户取消回调。</summary>
        public bool ThrowOnCancellation { get; init; }

        /// <summary>同步模式不可使用该钩子。</summary>
        /// <param name="phase">阶段。</param>
        /// <param name="architecture">实例。</param>
        public void OnPhase(ArchitecturePhase phase, IArchitecture architecture) =>
            throw new InvalidOperationException("Use asynchronous initialization.");

        /// <summary>仅在 Utility 之前等待取消；失败/销毁通知不再次等待。</summary>
        /// <param name="phase">阶段。</param>
        /// <param name="architecture">实例。</param>
        /// <param name="cancellationToken">本次启动取消令牌。</param>
        /// <returns>阶段已完成。</returns>
        public async ValueTask OnPhaseAsync(
            ArchitecturePhase phase, IArchitecture architecture, CancellationToken cancellationToken = default)
        {
            if (phase != ArchitecturePhase.BeforeUtilityInit)
                return;
            using var callback = cancellationToken.Register(() =>
            {
                if (ThrowOnCancellation)
                    throw new InvalidOperationException("cancel callback failed");
            });
            Entered.TrySetResult();
            await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
        }
    }
    /// <summary>以 barrier 验证 Ready 阶段具有真实异步完成边界。</summary>
    private sealed class ReadyBarrierHook : IAsyncArchitectureLifecycleHook
    {
        /// <summary>Ready 回调已开始。</summary>
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>允许 Ready 回调结束。</summary>
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>此场景要求异步入口。</summary>
        /// <param name="phase">阶段。</param>
        /// <param name="architecture">当前架构。</param>
        public void OnPhase(ArchitecturePhase phase, IArchitecture architecture) =>
            throw new InvalidOperationException("Use InitializeAsync.");

        /// <summary>Ready 阶段等待显式完成，其余阶段立即完成。</summary>
        /// <param name="phase">阶段。</param>
        /// <param name="architecture">当前架构。</param>
        /// <param name="cancellationToken">启动取消令牌。</param>
        /// <returns>阶段完成。</returns>
        public async ValueTask OnPhaseAsync(
            ArchitecturePhase phase, IArchitecture architecture, CancellationToken cancellationToken = default)
        {
            if (phase != ArchitecturePhase.Ready)
                return;
            Entered.TrySetResult();
            await Release.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
    }

}
