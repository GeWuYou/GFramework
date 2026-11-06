// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Enums;
using GFramework.Core.Abstractions.Lifecycle;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Abstractions.Rule;
using GFramework.Core.Abstractions.Model;
using GFramework.Core.Abstractions.Systems;
using GFramework.Core.Abstractions.Utility;
using GFramework.Core.Architectures;
using GFramework.Core.Logging;
using GFramework.Core.Rule;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     验证 Architecture 生命周期行为的集成测试。
///     这些测试覆盖阶段流转、失败状态传播和逆序销毁规则，
///     用于保护拆分后的生命周期管理、阶段协调与销毁协调行为。
/// </summary>
[TestFixture]
public class ArchitectureLifecycleBehaviorTests
{
    /// <summary>
    ///     为每个测试准备独立的日志工厂和全局上下文状态。
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        LoggerFactoryResolver.Provider = new ConsoleLoggerFactoryProvider();
        GameContext.Clear();
    }

    /// <summary>
    ///     清理测试注册到全局上下文表的架构上下文。
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        GameContext.Clear();
    }

    /// <summary>
    ///     验证初始化流程会按既定顺序推进所有生命周期阶段。
    /// </summary>
    [Test]
    public async Task InitializeAsync_Should_Enter_Expected_Phases_In_Order()
    {
        var architecture = new PhaseTrackingArchitecture();

        await architecture.InitializeAsync();

        Assert.That(architecture.PhaseHistory, Is.EqualTo(new[]
        {
            ArchitecturePhase.BeforeUtilityInit,
            ArchitecturePhase.AfterUtilityInit,
            ArchitecturePhase.BeforeModelInit,
            ArchitecturePhase.AfterModelInit,
            ArchitecturePhase.BeforeSystemInit,
            ArchitecturePhase.AfterSystemInit,
            ArchitecturePhase.Ready
        }));

        await architecture.DestroyAsync();
    }

    /// <summary>
    ///     验证阶段变更事件会以架构实例作为 sender，并通过事件参数暴露阶段值。
    /// </summary>
    [Test]
    public async Task InitializeAsync_Should_Raise_PhaseChanged_With_Sender_And_EventArgs()
    {
        var architecture = new PhaseTrackingArchitecture();
        var observations = new List<(object? Sender, ArchitecturePhase Phase)>();

        architecture.PhaseChanged += (sender, eventArgs) => observations.Add((sender, eventArgs.Phase));

        await architecture.InitializeAsync();

        Assert.That(observations, Is.Not.Empty);
        Assert.That(observations.All(item => ReferenceEquals(item.Sender, architecture)), Is.True);
        Assert.That(observations.Select(static item => item.Phase), Is.EqualTo(new[]
        {
            ArchitecturePhase.BeforeUtilityInit,
            ArchitecturePhase.AfterUtilityInit,
            ArchitecturePhase.BeforeModelInit,
            ArchitecturePhase.AfterModelInit,
            ArchitecturePhase.BeforeSystemInit,
            ArchitecturePhase.AfterSystemInit,
            ArchitecturePhase.Ready
        }));

        await architecture.DestroyAsync();
    }

    /// <summary>
    ///     验证用户初始化失败时，等待 Ready 的任务会失败并进入 FailedInitialization 阶段。
    /// </summary>
    [Test]
    public void InitializeAsync_When_Configuration_Throws_Should_Mark_FailedInitialization()
    {
        var architecture = new PhaseTrackingArchitecture(() => throw new InvalidOperationException("boom"));

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() => architecture.InitializeAsync());
        Assert.That(exception, Is.Not.Null);
        Assert.That(architecture.CurrentPhase, Is.EqualTo(ArchitecturePhase.FailedInitialization));
        Assert.ThrowsAsync<InvalidOperationException>(() => architecture.WaitUntilReadyAsync());
    }

    /// <summary>
    ///     验证销毁流程会按注册逆序释放组件，并推进 Destroying/Destroyed 阶段。
    /// </summary>
    [Test]
    public async Task DestroyAsync_Should_Destroy_Components_In_Reverse_Registration_Order()
    {
        var destroyOrder = new List<string>();
        var architecture = new DestroyOrderArchitecture(destroyOrder);

        await architecture.InitializeAsync();
        await architecture.DestroyAsync();

        Assert.Multiple(() =>
        {
            Assert.That(destroyOrder, Is.EqualTo(new[] { "system", "model", "utility" }));
            Assert.That(architecture.CurrentPhase, Is.EqualTo(ArchitecturePhase.Destroyed));
            Assert.That(architecture.PhaseHistory[^2..], Is.EqualTo(new[]
            {
                ArchitecturePhase.Destroying,
                ArchitecturePhase.Destroyed
            }));
        });
    }

    /// <summary>
    ///     验证初始化失败后仍然允许执行销毁流程。
    ///     该回归测试用于保护 FailedInitialization → Destroying 的合法迁移，避免失败路径上的组件与容器泄漏。
    /// </summary>
    [Test]
    public async Task DestroyAsync_After_FailedInitialization_Should_Cleanup_And_Enter_Destroyed()
    {
        var destroyOrder = new List<string>();
        var architecture = new FailingInitializationArchitecture(destroyOrder);

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() => architecture.InitializeAsync());
        Assert.That(exception, Is.Not.Null);
        Assert.That(architecture.CurrentPhase, Is.EqualTo(ArchitecturePhase.FailedInitialization));

        await architecture.DestroyAsync();

        Assert.Multiple(() =>
        {
            Assert.That(destroyOrder, Is.EqualTo(new[] { "model", "utility" }));
            Assert.That(architecture.CurrentPhase, Is.EqualTo(ArchitecturePhase.Destroyed));
            Assert.That(architecture.PhaseHistory[^3..], Is.EqualTo(new[]
            {
                ArchitecturePhase.FailedInitialization,
                ArchitecturePhase.Destroying,
                ArchitecturePhase.Destroyed
            }));
        });
    }

    /// <summary>
    ///     验证已登记的阶段监听器收到销毁阶段通知。
    /// </summary>
    [Test]
    public async Task DestroyAsync_Should_Notify_Phase_Listeners_About_Destroyed()
    {
        var listener = new TrackingPhaseListener();
        var architecture = new ListenerTrackingArchitecture(listener);

        await architecture.InitializeAsync();
        await architecture.DestroyAsync();

        Assert.That(listener.ObservedPhases[^2..], Is.EqualTo(new[]
        {
            ArchitecturePhase.Destroying,
            ArchitecturePhase.Destroyed
        }));
    }

    /// <summary>
    ///     验证架构销毁后拒绝通过其原实例上下文继续解析服务。
    /// </summary>
    [Test]
    public async Task DestroyAsync_Should_Invalidate_Instance_Context()
    {
        var architecture = new PhaseTrackingArchitecture();
        await architecture.InitializeAsync();
        var context = architecture.Context;
        Assert.That(GameContext.TryGet<ArchitectureContext>(out _), Is.False);

        await architecture.DestroyAsync();

        Assert.Throws<ObjectDisposedException>(() => context.GetService<TrackingPhaseListener>());
    }

    /// <summary>
    ///     验证失败初始化自动清理，不再等待调用方显式销毁才释放服务。
    /// </summary>
    [Test]
    public async Task FailedInitialization_Should_Invalidate_Instance_Context_Automatically()
    {
        var destroyOrder = new List<string>();
        var architecture = new FailingInitializationArchitecture(destroyOrder);

        Assert.ThrowsAsync<InvalidOperationException>(() => architecture.InitializeAsync());
        var context = architecture.Context;
        Assert.Multiple(() =>
        {
            Assert.That(destroyOrder, Is.EqualTo(new[] { "model", "utility" }));
            Assert.That(GameContext.TryGet<ArchitectureContext>(out _), Is.False);
            Assert.Throws<ObjectDisposedException>(() => context.GetService<TrackingPhaseListener>());
        });

        await architecture.DestroyAsync();
        Assert.That(destroyOrder, Is.EqualTo(new[] { "model", "utility" }));
    }

    /// <summary>
    ///     验证 ContextAware 从启动开始就只接受显式实例上下文。
    /// </summary>
    [Test]
    public async Task ContextAware_Should_Require_Explicit_Context_Even_While_Architecture_IsReady()
    {
        var architecture = new PhaseTrackingArchitecture();
        await architecture.InitializeAsync();
        IContextAware probe = new LifecycleContextAwareProbe();
        Assert.Throws<InvalidOperationException>(() => probe.GetContext());

        probe.SetContext(architecture.Context);
        Assert.That(probe.GetContext(), Is.SameAs(architecture.Context));
        await architecture.DestroyAsync();

        Assert.Throws<ObjectDisposedException>(() => probe.GetContext().GetService<TrackingPhaseListener>());
    }

    /// <summary>
    ///     验证同步销毁入口与异步入口共享终态和实例上下文失效语义。
    /// </summary>
    [Test]
    public async Task Destroy_Should_Invalidate_Instance_Context()
    {
        var architecture = new PhaseTrackingArchitecture();
        await architecture.InitializeAsync();
        var context = architecture.Context;

#pragma warning disable CS0618
        architecture.Destroy();
#pragma warning restore CS0618

        Assert.Throws<ObjectDisposedException>(() => context.GetService<TrackingPhaseListener>());
        await architecture.DestroyAsync();
    }

    /// <summary>
    ///     配置结束后即关闭注册窗口；就绪前的启动回调也不能追加组件或触发其初始化。
    /// </summary>
    [Test]
    public async Task Registration_After_Configuration_Should_Reject_Without_Initializing_Component()
    {
        var destroyOrder = new List<string>();
        var lateComponent = new TrackingDestroyableSystem(destroyOrder);
        var architecture = new PhaseTrackingArchitecture(onStarted: target =>
            Assert.Throws<InvalidOperationException>(() => target.RegisterSystem(lateComponent)));

        await architecture.InitializeAsync();

        Assert.Multiple(() =>
        {
            Assert.That(lateComponent.InitializeCallCount, Is.Zero);
            Assert.Throws<InvalidOperationException>(() =>
                architecture.Context.GetSystem<TrackingDestroyableSystem>());
        });

        await architecture.DestroyAsync();
        Assert.That(destroyOrder, Is.Empty);
    }

    /// <summary>
    ///     记录阶段流转的可配置测试架构。
    /// </summary>
    private sealed class PhaseTrackingArchitecture : Architecture
    {
        private readonly Action? _onInitializeAction;
        private readonly Action<IArchitecture>? _onStarted;

        /// <summary>
        ///     创建一个可选地在用户初始化阶段执行自定义逻辑的测试架构。
        /// </summary>
        /// <param name="onInitializeAction">配置阶段执行的测试回调。</param>
        /// <param name="onStarted">配置结束后、就绪前执行的回调。</param>
        public PhaseTrackingArchitecture(
            Action? onInitializeAction = null,
            Action<IArchitecture>? onStarted = null)
        {
            _onInitializeAction = onInitializeAction;
            _onStarted = onStarted;
            PhaseChanged += (_, eventArgs) => PhaseHistory.Add(eventArgs.Phase);
        }

        /// <summary>
        ///     获取架构经历过的阶段列表。
        /// </summary>
        public List<ArchitecturePhase> PhaseHistory { get; } = [];

        /// <summary>
        ///     执行测试注入的初始化逻辑。
        /// </summary>
        /// <param name="services">当前配置阶段的服务描述集合。</param>
        protected override void ConfigureServices(IServiceCollection services)
        {
            _onInitializeAction?.Invoke();
        }

        /// <summary>
        ///     验证服务已经构建后的操作约束。
        /// </summary>
        protected override void OnStarted()
        {
            _onStarted?.Invoke(this);
        }
    }

    /// <summary>
    ///     仅用于验证销毁后全局上下文回退是否仍然泄漏的最小 ContextAware 探针。
    /// </summary>
    private sealed class LifecycleContextAwareProbe : ContextAwareBase
    {
    }

    /// <summary>
    ///     在初始化时注册可销毁组件的测试架构。
    /// </summary>
    private sealed class DestroyOrderArchitecture : Architecture
    {
        private readonly List<string> _destroyOrder;

        /// <summary>
        ///     创建用于验证销毁顺序的测试架构。
        /// </summary>
        /// <param name="destroyOrder">记录组件销毁顺序的列表。</param>
        public DestroyOrderArchitecture(List<string> destroyOrder)
        {
            _destroyOrder = destroyOrder;
            PhaseChanged += (_, eventArgs) => PhaseHistory.Add(eventArgs.Phase);
        }

        /// <summary>
        ///     获取架构经历过的阶段列表。
        /// </summary>
        public List<ArchitecturePhase> PhaseHistory { get; } = [];

        /// <summary>
        ///     注册会记录销毁顺序的 Utility、Model 和 System。
        /// </summary>
        /// <param name="services">当前配置阶段的服务描述集合。</param>
        protected override void ConfigureServices(IServiceCollection services)
        {
            RegisterUtility(new TrackingDestroyableUtility(_destroyOrder));
            RegisterModel(new TrackingDestroyableModel(_destroyOrder));
            RegisterSystem(new TrackingDestroyableSystem(_destroyOrder));
        }
    }

    /// <summary>
    ///     在初始化阶段注册可销毁组件并随后抛出异常的测试架构。
    /// </summary>
    private sealed class FailingInitializationArchitecture : Architecture
    {
        private readonly List<string> _destroyOrder;

        /// <summary>
        ///     创建用于验证失败后销毁行为的测试架构。
        /// </summary>
        /// <param name="destroyOrder">记录失败后清理顺序的列表。</param>
        public FailingInitializationArchitecture(List<string> destroyOrder)
        {
            _destroyOrder = destroyOrder;
            PhaseChanged += (_, eventArgs) => PhaseHistory.Add(eventArgs.Phase);
        }

        /// <summary>
        ///     获取架构经历过的阶段列表。
        /// </summary>
        public List<ArchitecturePhase> PhaseHistory { get; } = [];

        /// <summary>
        ///     注册可销毁组件后故意抛出异常，模拟初始化失败场景。
        /// </summary>
        /// <param name="services">当前配置阶段的服务描述集合。</param>
        protected override void ConfigureServices(IServiceCollection services)
        {
            RegisterUtility(new TrackingDestroyableUtility(_destroyOrder));
            RegisterModel(new TrackingDestroyableModel(_destroyOrder));
        }

        /// <summary>
        ///     在组件已经成功初始化后失败，验证自动停止已启动组件。
        /// </summary>
        protected override void OnStarted()
        {
            throw new InvalidOperationException("boom");
        }
    }

    /// <summary>
    ///     通过配置器把阶段监听器注册到容器中的测试架构。
    /// </summary>
    private sealed class ListenerTrackingArchitecture(TrackingPhaseListener listener) : Architecture
    {
        /// <summary>
        ///     保持对监听器的引用，以便配置器在初始化前把同一实例注册到容器。
        /// </summary>
        public override Action<IServiceCollection>? Configurator =>
            services => services.AddSingleton<IArchitecturePhaseListener>(listener);

        /// <summary>
        ///     该测试不需要额外组件注册。
        /// </summary>
        /// <param name="services">当前配置阶段的服务描述集合。</param>
        protected override void ConfigureServices(IServiceCollection services)
        {
        }
    }

    /// <summary>
    ///     用于验证逆序销毁的上下文工具。
    /// </summary>
    private sealed class TrackingDestroyableUtility(List<string> destroyOrder) : IContextUtility
    {
        private IArchitectureContext _context = null!;

        public void Initialize()
        {
        }

        public void Destroy()
        {
            destroyOrder.Add("utility");
        }

        public void SetContext(IArchitectureContext context)
        {
            _context = context;
        }

        public IArchitectureContext GetContext()
        {
            return _context;
        }
    }

    /// <summary>
    ///     记录容器阶段通知顺序的监听器。
    /// </summary>
    private sealed class TrackingPhaseListener : IArchitecturePhaseListener
    {
        /// <summary>
        ///     获取监听到的阶段列表。
        /// </summary>
        public List<ArchitecturePhase> ObservedPhases { get; } = [];

        /// <summary>
        ///     记录收到的阶段通知。
        /// </summary>
        /// <param name="phase">当前阶段。</param>
        public void OnArchitecturePhase(ArchitecturePhase phase)
        {
            ObservedPhases.Add(phase);
        }
    }

    /// <summary>
    ///     用于验证逆序销毁的模型。
    /// </summary>
    private sealed class TrackingDestroyableModel(List<string> destroyOrder) : IModel, IDestroyable
    {
        private IArchitectureContext _context = null!;

        public void Destroy()
        {
            destroyOrder.Add("model");
        }

        public void Initialize()
        {
        }

        public void OnArchitecturePhase(ArchitecturePhase phase)
        {
        }

        public void SetContext(IArchitectureContext context)
        {
            _context = context;
        }

        public IArchitectureContext GetContext()
        {
            return _context;
        }
    }

    /// <summary>
    ///     用于验证逆序销毁的系统。
    /// </summary>
    private sealed class TrackingDestroyableSystem(List<string> destroyOrder) : ISystem
    {
        private IArchitectureContext _context = null!;

        /// <summary>
        ///     记录是否真的进入过初始化。
        /// </summary>
        public int InitializeCallCount { get; private set; }

        /// <summary>
        ///     记录一次初始化。
        /// </summary>
        public void Initialize()
        {
            InitializeCallCount++;
        }

        public void Destroy()
        {
            destroyOrder.Add("system");
        }

        public void OnArchitecturePhase(ArchitecturePhase phase)
        {
        }

        public void SetContext(IArchitectureContext context)
        {
            _context = context;
        }

        public IArchitectureContext GetContext()
        {
            return _context;
        }
    }
}
