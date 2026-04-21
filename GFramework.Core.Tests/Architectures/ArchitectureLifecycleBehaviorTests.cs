using System.Reflection;
using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Enums;
using GFramework.Core.Abstractions.Lifecycle;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Abstractions.Model;
using GFramework.Core.Abstractions.Systems;
using GFramework.Core.Abstractions.Utility;
using GFramework.Core.Architectures;
using GFramework.Core.Logging;

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
    public async Task InitializeAsync_When_OnInitialize_Throws_Should_Mark_FailedInitialization()
    {
        var architecture = new PhaseTrackingArchitecture(() => throw new InvalidOperationException("boom"));

        var exception = Assert.ThrowsAsync<InvalidOperationException>(async () => await architecture.InitializeAsync());
        Assert.That(exception, Is.Not.Null);
        Assert.That(architecture.CurrentPhase, Is.EqualTo(ArchitecturePhase.FailedInitialization));
        Assert.ThrowsAsync<InvalidOperationException>(async () => await architecture.WaitUntilReadyAsync());
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

        var exception = Assert.ThrowsAsync<InvalidOperationException>(async () => await architecture.InitializeAsync());
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
    ///     验证 Destroyed 阶段会在容器清空前广播给容器内的阶段监听器。
    ///     该回归测试保护销毁尾声的阶段通知，确保依赖最终阶段信号的服务仍能收到 Destroyed。
    /// </summary>
    [Test]
    public async Task DestroyAsync_Should_Notify_Container_Phase_Listeners_About_Destroyed_Before_Clear()
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
    ///     验证启用 AllowLateRegistration 时，生命周期层会立即初始化后注册的组件，而不是继续沿用初始化期的拒绝策略。
    ///     由于公共架构 API 在 Ready 之后会先触发容器限制，此回归测试直接覆盖生命周期协作者的对齐逻辑。
    /// </summary>
    [Test]
    public async Task
        RegisterLifecycleComponent_After_Initialization_Should_Initialize_Immediately_When_LateRegistration_Is_Enabled()
    {
        var architecture = new AllowLateRegistrationArchitecture();
        await architecture.InitializeAsync();

        var lateComponent = new LateRegisteredInitializableComponent();

        architecture.RegisterLateComponentForTesting(lateComponent);

        Assert.That(lateComponent.InitializeCallCount, Is.EqualTo(1));

        await architecture.DestroyAsync();
    }

    /// <summary>
    ///     记录阶段流转的可配置测试架构。
    /// </summary>
    private sealed class PhaseTrackingArchitecture : Architecture
    {
        private readonly Action? _onInitializeAction;

        /// <summary>
        ///     创建一个可选地在用户初始化阶段执行自定义逻辑的测试架构。
        /// </summary>
        /// <param name="onInitializeAction">用户初始化时执行的测试回调。</param>
        public PhaseTrackingArchitecture(Action? onInitializeAction = null)
        {
            _onInitializeAction = onInitializeAction;
            PhaseChanged += (_, eventArgs) => PhaseHistory.Add(eventArgs.Phase);
        }

        /// <summary>
        ///     获取架构经历过的阶段列表。
        /// </summary>
        public List<ArchitecturePhase> PhaseHistory { get; } = [];

        /// <summary>
        ///     执行测试注入的初始化逻辑。
        /// </summary>
        protected override void OnInitialize()
        {
            _onInitializeAction?.Invoke();
        }
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
        protected override void OnInitialize()
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
        protected override void OnInitialize()
        {
            RegisterUtility(new TrackingDestroyableUtility(_destroyOrder));
            RegisterModel(new TrackingDestroyableModel(_destroyOrder));
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
        protected override void OnInitialize()
        {
        }
    }

    /// <summary>
    ///     启用 AllowLateRegistration 的测试架构。
    ///     该架构暴露生命周期协作者供回归测试验证内部注册策略对齐。
    /// </summary>
    private sealed class AllowLateRegistrationArchitecture : Architecture
    {
        /// <summary>
        ///     使用允许后注册的配置创建测试架构。
        /// </summary>
        public AllowLateRegistrationArchitecture()
            : base(new ArchitectureConfiguration
            {
                ArchitectureProperties = new()
                {
                    AllowLateRegistration = true,
                    StrictPhaseValidation = true
                }
            })
        {
        }

        /// <summary>
        ///     该测试不需要初始组件。
        /// </summary>
        protected override void OnInitialize()
        {
        }

        /// <summary>
        ///     通过反射调用内部生命周期协作者的注册逻辑，以便覆盖无法通过公共 API 直接到达的后注册初始化路径。
        /// </summary>
        /// <param name="component">要登记到生命周期中的后注册组件。</param>
        public void RegisterLateComponentForTesting(object component)
        {
            var field = typeof(Architecture).GetField(
                "_lifecycle",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var lifecycle = field?.GetValue(this) ??
                            throw new InvalidOperationException("Architecture lifecycle field was not found.");
            var registerMethod = lifecycle.GetType().GetMethod(nameof(RegisterLateComponentForTesting)) ??
                                 lifecycle.GetType().GetMethod("RegisterLifecycleComponent") ??
                                 throw new InvalidOperationException(
                                     "Architecture lifecycle registration method was not found.");

            registerMethod.Invoke(lifecycle, [component]);
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
    ///     记录即时初始化次数的后注册测试组件。
    /// </summary>
    private sealed class LateRegisteredInitializableComponent : IInitializable
    {
        /// <summary>
        ///     获取组件被即时初始化的次数。
        /// </summary>
        public int InitializeCallCount { get; private set; }

        /// <summary>
        ///     记录一次初始化调用。
        /// </summary>
        public void Initialize()
        {
            InitializeCallCount++;
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

        public void Initialize()
        {
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
