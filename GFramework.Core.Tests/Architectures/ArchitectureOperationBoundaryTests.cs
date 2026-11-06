// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Enums;
using GFramework.Core.Abstractions.Ioc;
using GFramework.Core.Abstractions.Lifecycle;
using GFramework.Core.Abstractions.Model;
using GFramework.Core.Abstractions.Systems;
using GFramework.Core.Abstractions.Utility;
using GFramework.Core.Architectures;
using GFramework.Core.Rule;
using GFramework.Core.Systems;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     验证异步操作结束边界、取消源责任和多角色组件在标准 provider 上的生命周期。
/// </summary>
[TestFixture]
public sealed class ArchitectureOperationBoundaryTests
{
    private static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    ///     启动回调派生的后台任务在启动完成后可以销毁架构，不受过期的执行上下文标记影响。
    /// </summary>
    /// <returns>启动和后台销毁均完成。</returns>
    [Test]
    public async Task Background_Task_Captured_During_Startup_Should_Destroy_After_Startup_Completes()
    {
        var releaseBackground = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var resource = new OwnedResource();
        Task? background = null;
        var architecture = new BoundaryArchitecture((_, services) => services.AddSingleton(serviceProvider => resource));
        architecture.Started = () =>
        {
            _ = architecture.Context.GetService<OwnedResource>();
            // 捕获真实的启动执行上下文，再用 barrier 确保销毁发生在初始化任务完成之后。
            background = Task.Run(async () =>
            {
                await releaseBackground.Task.ConfigureAwait(false);
                await architecture.DestroyAsync().ConfigureAwait(false);
            });
        };

        await architecture.InitializeAsync().WaitAsync(OperationTimeout).ConfigureAwait(false);
        releaseBackground.TrySetResult();
        await background!.WaitAsync(OperationTimeout).ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(architecture.CurrentPhase, Is.EqualTo(ArchitecturePhase.Destroyed));
            Assert.That(resource.DisposeCalls, Is.EqualTo(1));
        });
        await architecture.DestroyAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     失败初始化会解除与外部取消源的链接，自动清理不依赖调用方再显式销毁。
    /// </summary>
    /// <returns>已观察到失败、解除链接和最终销毁。</returns>
    [Test]
    public async Task Failed_Startup_Should_Release_Linked_Cancellation_Registration()
    {
        using var external = new CancellationTokenSource();
        CancellationToken captured = default;
        var hook = new PhaseHook((phase, token) =>
        {
            if (phase != ArchitecturePhase.BeforeUtilityInit)
                return ValueTask.CompletedTask;
            captured = token;
            throw new InvalidOperationException("startup hook failed");
        });
        var architecture = new BoundaryArchitecture((owner, _) => owner.RegisterLifecycleHook(hook));

        Assert.That(
            async () => await architecture.InitializeAsync(external.Token).ConfigureAwait(false),
            Throws.InvalidOperationException.With.Message.EqualTo("startup hook failed"));
        Assert.That(captured.CanBeCanceled, Is.True);

        external.Cancel();

        Assert.That(captured.IsCancellationRequested, Is.False,
            "Completed failed startup must detach its linked token from the caller's later cancellation.");
        await architecture.DestroyAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     启动及 Ready 等待者的取消结果保留调用者令牌，不暴露已释放的内部 linked-token 身份。
    /// </summary>
    /// <returns>两个等待者都以同一外部取消身份完成。</returns>
    [Test]
    public async Task Canceled_Startup_And_Ready_Waiter_Should_Preserve_Caller_Token()
    {
        using var external = new CancellationTokenSource();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var hook = new PhaseHook(async (phase, token) =>
        {
            if (phase != ArchitecturePhase.BeforeUtilityInit)
                return;
            entered.TrySetResult();
            await Task.Delay(Timeout.Infinite, token).ConfigureAwait(false);
        });
        var architecture = new BoundaryArchitecture((owner, _) => owner.RegisterLifecycleHook(hook));
        var startup = architecture.InitializeAsync(external.Token);
        var ready = architecture.WaitUntilReadyAsync();
        await entered.Task.WaitAsync(OperationTimeout).ConfigureAwait(false);

        external.Cancel();

        var startupError = await Assert.CatchAsync<OperationCanceledException>(
            async () => await startup.WaitAsync(OperationTimeout).ConfigureAwait(false)).ConfigureAwait(false);
        var readyError = await Assert.CatchAsync<OperationCanceledException>(
            async () => await ready.WaitAsync(OperationTimeout).ConfigureAwait(false)).ConfigureAwait(false);
        Assert.Multiple(() =>
        {
            Assert.That(startupError!.CancellationToken, Is.EqualTo(external.Token));
            Assert.That(readyError!.CancellationToken, Is.EqualTo(external.Token));
        });
        await architecture.DestroyAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     同一实现不能通过多个类型登记重复创建描述，拒绝后保留原先唯一有效的组件。
    /// </summary>
    /// <returns>唯一组件只激活和初始化一次。</returns>
    [Test]
    public async Task Repeated_Typed_Model_And_System_Registration_Should_Not_Add_Another_Descriptor()
    {
        var architecture = new BoundaryArchitecture((owner, services) =>
        {
            owner.RegisterModel<MultiRoleComponent>();
            var descriptorsBeforeDuplicate = services.Count;
            Assert.That(
                () => owner.RegisterSystem<MultiRoleComponent>(),
                Throws.InvalidOperationException.With.Message.Contains("already registered"));
            Assert.That(services.Count, Is.EqualTo(descriptorsBeforeDuplicate));
        });

        await architecture.InitializeAsync().ConfigureAwait(false);
        var components = architecture.Context.GetServices<MultiRoleComponent>();
        Assert.Multiple(() =>
        {
            Assert.That(components, Has.Count.EqualTo(1));
            Assert.That(components.Single().InitializeCalls, Is.EqualTo(1));
            Assert.That(components.Single().ContextReadyCalls, Is.EqualTo(1));
        });

        await architecture.DestroyAsync().ConfigureAwait(false);
        Assert.That(components.Single().StopCalls, Is.EqualTo(1));
    }

    /// <summary>
    ///     同一个外部实例可以暴露多个角色，但只绑定、初始化、接收阶段和停止一次。
    /// </summary>
    /// <returns>多角色实例的生命周期完成。</returns>
    [Test]
    public async Task External_MultiRole_Instance_Should_Initialize_In_One_Group_And_Stop_Once()
    {
        var component = new MultiRoleComponent();
        var architecture = new BoundaryArchitecture((owner, _) =>
        {
            owner.RegisterModel(component);
            owner.RegisterSystem(component);
            owner.RegisterUtility(component);
        });

        await architecture.InitializeAsync().ConfigureAwait(false);
        Assert.Multiple(() =>
        {
            Assert.That(component.InitializeCalls, Is.EqualTo(1));
            Assert.That(component.ContextReadyCalls, Is.EqualTo(1));
            Assert.That(component.InitializedAtPhase, Is.EqualTo(ArchitecturePhase.BeforeUtilityInit));
            Assert.That(component.Phases.Count(phase => phase == ArchitecturePhase.Ready), Is.EqualTo(1));
            Assert.That(architecture.Context.GetServices<MultiRoleComponent>(), Has.Count.EqualTo(1));
        });

        await architecture.DestroyAsync().ConfigureAwait(false);
        await architecture.DestroyAsync().ConfigureAwait(false);
        Assert.That(component.StopCalls, Is.EqualTo(1));
    }

    /// <summary>
    ///     只走异步初始化的派生 System 仍能通过基类 Destroy 安全停止，不要求偷偷调用同步初始化。
    /// </summary>
    /// <returns>异步启动和基类停止均完成。</returns>
    [Test]
    public async Task Async_System_Should_Stop_Without_Calling_Base_Synchronous_Initialize()
    {
        var component = new AsyncOnlySystem();
        var architecture = new BoundaryArchitecture((owner, _) => owner.RegisterSystem(component));

        await architecture.InitializeAsync().ConfigureAwait(false);
        Assert.That(component.AsyncInitializeCalls, Is.EqualTo(1));
        await architecture.DestroyAsync().ConfigureAwait(false);

        Assert.That(component.StopCalls, Is.EqualTo(1));
    }

    /// <summary>
    ///     正常或失败启动后的清理错误保持可观察；后续销毁复用结果，不重复停止或释放。
    /// </summary>
    /// <param name="failStartup">是否在组件已启动后模拟宿主失败。</param>
    /// <returns>清理错误和恰好一次的资源责任已经观察。</returns>
    [TestCase(false)]
    [TestCase(true)]
    public async Task Cleanup_Failure_Should_Be_Retained_Without_Repeated_Stop_Or_Disposal(bool failStartup)
    {
        var resource = new OwnedResource(throwOnDispose: true);
        var component = new ThrowingStopSystem();
        var architecture = new BoundaryArchitecture((owner, services) =>
        {
            services.AddSingleton(serviceProvider => resource);
            owner.RegisterSystem(component);
        });
        architecture.Started = () =>
        {
            _ = architecture.Context.GetService<OwnedResource>();
            if (failStartup)
                throw new InvalidOperationException("host failed");
        };
        if (failStartup)
        {
            var startupError = await Assert.CatchAsync<AggregateException>(
                async () => await architecture.InitializeAsync().ConfigureAwait(false));
            Assert.That(startupError!.ToString(), Does.Contain("host failed"));
        }
        else
            await architecture.InitializeAsync().ConfigureAwait(false);

        var firstError = await Assert.CatchAsync<AggregateException>(
            async () => await architecture.DestroyAsync().ConfigureAwait(false)).ConfigureAwait(false);
        var secondError = await Assert.CatchAsync<AggregateException>(
            async () => await architecture.DestroyAsync().ConfigureAwait(false)).ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(firstError!.ToString(), Does.Contain("stop failed"));
            Assert.That(firstError!.ToString(), Does.Contain("dispose failed"));
            Assert.That(secondError, Is.SameAs(firstError));
            Assert.That(component.StopCalls, Is.EqualTo(1));
            Assert.That(resource.DisposeCalls, Is.EqualTo(1));
            Assert.That(architecture.CurrentPhase, Is.EqualTo(ArchitecturePhase.Destroyed));
        });
    }

    /// <summary>
    ///     同优先级模块维持声明顺序启动，并按相反顺序停止。
    /// </summary>
    /// <returns>模块注册、启动和停止的实际顺序已观察。</returns>
    [Test]
    public async Task Equal_Priority_Modules_Should_Start_In_Declaration_Order_And_Stop_In_Reverse()
    {
        var calls = new List<string>();
        var architecture = new BoundaryArchitecture((owner, _) =>
        {
            owner.RegisterServiceModule(new RecordingModule("first", calls));
            owner.RegisterServiceModule(new RecordingModule("second", calls));
        });

        await architecture.InitializeAsync().ConfigureAwait(false);
        Assert.That(calls, Is.EqualTo(new[]
        {
            "register:first", "register:second", "initialize:first", "initialize:second"
        }));

        await architecture.DestroyAsync().ConfigureAwait(false);
        Assert.That(calls, Is.EqualTo(new[]
        {
            "register:first", "register:second", "initialize:first", "initialize:second",
            "stop:second", "stop:first"
        }));
    }

    /// <summary>独立的小型架构夹具，不依赖其他测试类的内部实现。</summary>
    /// <param name="configure">描述阶段的明确配置。</param>
    private sealed class BoundaryArchitecture(Action<BoundaryArchitecture, IServiceCollection> configure) : Architecture
    {
        /// <summary>组件启动之后的宿主动作。</summary>
        public Action? Started { get; set; }

        /// <summary>仅声明本用例需要的依赖与参与者。</summary>
        /// <param name="services">当前架构的服务集合。</param>
        protected override void ConfigureServices(IServiceCollection services) => configure(this, services);

        /// <summary>在真实启动调用链执行测试提供的宿主动作。</summary>
        protected override void OnStarted() => Started?.Invoke();
    }

    /// <summary>阶段委托夹具，对不相关阶段保持无操作。</summary>
    /// <param name="callback">当前用例的异步阶段行为。</param>
    private sealed class PhaseHook(Func<ArchitecturePhase, CancellationToken, ValueTask> callback)
        : IAsyncArchitectureLifecycleHook
    {
        /// <summary>同步模式不允许执行异步阶段夹具。</summary>
        /// <param name="phase">目标阶段。</param>
        /// <param name="architecture">当前架构。</param>
        public void OnPhase(ArchitecturePhase phase, IArchitecture architecture) =>
            throw new InvalidOperationException("Use InitializeAsync for this hook.");

        /// <summary>执行当前用例的阶段行为。</summary>
        /// <param name="phase">目标阶段。</param>
        /// <param name="architecture">当前架构。</param>
        /// <param name="cancellationToken">本轮启动令牌。</param>
        /// <returns>阶段处理完成。</returns>
        public ValueTask OnPhaseAsync(
            ArchitecturePhase phase, IArchitecture architecture, CancellationToken cancellationToken = default) =>
            callback(phase, cancellationToken);
    }

    /// <summary>三个角色共享同一个显式生命周期，用于检查别名和角色分组去重。</summary>
    private sealed class MultiRoleComponent : ContextAwareBase, IModel, ISystem, IContextUtility
    {
        private ArchitecturePhase _phase;

        /// <summary>已观察到的初始化调用次数。</summary>
        public int InitializeCalls { get; private set; }

        /// <summary>上下文就绪回调次数。</summary>
        public int ContextReadyCalls { get; private set; }

        /// <summary>业务停止调用次数。</summary>
        public int StopCalls { get; private set; }

        /// <summary>开始初始化时的实际阶段。</summary>
        public ArchitecturePhase InitializedAtPhase { get; private set; }

        /// <summary>所有实际阶段通知。</summary>
        public List<ArchitecturePhase> Phases { get; } = [];

        /// <summary>记录一次真正的组件初始化。</summary>
        public void Initialize()
        {
            InitializeCalls++;
            InitializedAtPhase = _phase;
        }

        /// <summary>记录一次业务停止。</summary>
        public void Destroy() => StopCalls++;

        /// <summary>记录上下文实际绑定，不能因角色别名重复触发。</summary>
        protected override void OnContextReady() => ContextReadyCalls++;

        /// <summary>记录阶段以区分真正执行的初始化分组。</summary>
        /// <param name="phase">当前阶段。</param>
        public void OnArchitecturePhase(ArchitecturePhase phase)
        {
            _phase = phase;
            Phases.Add(phase);
        }
    }

    /// <summary>异步初始化故意不经过 AbstractSystem.Initialize，验证停止路径的独立有效性。</summary>
    private sealed class AsyncOnlySystem : AbstractSystem, IAsyncInitializable
    {
        /// <summary>异步初始化调用次数。</summary>
        public int AsyncInitializeCalls { get; private set; }

        /// <summary>基类停止入口实际抵达派生清理的次数。</summary>
        public int StopCalls { get; private set; }

        /// <summary>提供真实异步契约，不调用同步初始化。</summary>
        /// <param name="cancellationToken">启动取消令牌。</param>
        /// <returns>异步初始化完成。</returns>
        public Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AsyncInitializeCalls++;
            return Task.CompletedTask;
        }

        /// <summary>若错误地退回同步初始化，应让测试明确失败。</summary>
        protected override void OnInit() => throw new InvalidOperationException("Unexpected synchronous initialization.");

        /// <summary>只记录业务停止，不释放 DI 资源。</summary>
        protected override void OnDestroy() => StopCalls++;
    }

    /// <summary>有意失败的业务停止夹具，验证错误不被吞掉。</summary>
    private sealed class ThrowingStopSystem : AbstractSystem
    {
        /// <summary>实际停止次数。</summary>
        public int StopCalls { get; private set; }

        /// <summary>启动不需要额外动作。</summary>
        protected override void OnInit() { }

        /// <summary>记录尝试后报告具体停止失败。</summary>
        protected override void OnDestroy()
        {
            StopCalls++;
            throw new InvalidOperationException("stop failed");
        }
    }

    /// <summary>通过 DI 工厂拥有的可计数资源，可显式模拟最终资源释放失败。</summary>
    /// <param name="throwOnDispose">是否报告资源释放失败。</param>
    private sealed class OwnedResource(bool throwOnDispose = false) : IDisposable
    {
        /// <summary>实际资源释放次数。</summary>
        public int DisposeCalls { get; private set; }

        /// <summary>记录真正的资源释放尝试。</summary>
        public void Dispose()
        {
            DisposeCalls++;
            if (throwOnDispose)
                throw new InvalidOperationException("dispose failed");
        }
    }

    /// <summary>具有相同优先级的显式模块，以字符串序列观察真实调用顺序。</summary>
    /// <param name="name">本用例内唯一名称。</param>
    /// <param name="calls">共享的可观察调用序列。</param>
    private sealed class RecordingModule(string name, List<string> calls) : IServiceModule
    {
        /// <summary>当前模块的唯一名称。</summary>
        public string ModuleName => name;

        /// <summary>两个模块拥有相同的稳定排序键。</summary>
        public int Priority => 1000;

        /// <summary>本测试模块始终参与当前架构。</summary>
        public bool IsEnabled => true;

        /// <summary>记录配置阶段描述登记，不提前启动业务。</summary>
        /// <param name="container">当前架构的容器。</param>
        public void Register(IIocContainer container) => calls.Add("register:" + name);

        /// <summary>记录当前模块启动。</summary>
        public void Initialize() => calls.Add("initialize:" + name);

        /// <summary>记录当前模块停止。</summary>
        /// <returns>本模块停止完成。</returns>
        public ValueTask DestroyAsync()
        {
            calls.Add("stop:" + name);
            return ValueTask.CompletedTask;
        }
    }
}
