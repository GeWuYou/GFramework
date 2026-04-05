using GFramework.Core.Abstractions.Enums;
using GFramework.Core.Abstractions.Events;
using GFramework.Core.Architectures;
using GFramework.Core.Environment;
using GFramework.Core.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     验证 <see cref="Architecture" /> 初始化编排流程的单元测试。
///     这些测试覆盖环境初始化、服务准备、上下文绑定和自定义服务配置的时序，
///     以确保核心协调器在拆分后仍保持既有行为。
/// </summary>
[TestFixture]
public class ArchitectureInitializationPipelineTests
{
    /// <summary>
    ///     为每个测试准备独立的日志工厂和游戏上下文状态。
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        LoggerFactoryResolver.Provider = new ConsoleLoggerFactoryProvider();
        GameContext.Clear();
    }

    /// <summary>
    ///     清理测试期间注册的全局游戏上下文，避免跨测试污染。
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        GameContext.Clear();
    }

    /// <summary>
    ///     验证异步初始化会在执行用户初始化逻辑之前准备环境、服务和上下文。
    /// </summary>
    [Test]
    public async Task InitializeAsync_Should_Prepare_Runtime_Before_OnInitialize()
    {
        var environment = new TrackingEnvironment();
        var marker = new BootstrapMarker();
        var architecture = new InitializationPipelineTestArchitecture(environment, marker);

        await architecture.InitializeAsync();

        AssertRuntimePrepared(architecture, environment, marker);
        await architecture.DestroyAsync();
    }

    /// <summary>
    ///     验证同步初始化路径复用同一套基础设施准备流程。
    /// </summary>
    [Test]
    public async Task Initialize_Should_Prepare_Runtime_Before_OnInitialize()
    {
        var environment = new TrackingEnvironment();
        var marker = new BootstrapMarker();
        var architecture = new InitializationPipelineTestArchitecture(environment, marker);

        architecture.Initialize();

        AssertRuntimePrepared(architecture, environment, marker);
        await architecture.DestroyAsync();
    }

    /// <summary>
    ///     断言初始化阶段所需的运行时准备工作都已经完成。
    /// </summary>
    /// <param name="architecture">待验证的测试架构实例。</param>
    /// <param name="environment">测试使用的环境对象。</param>
    /// <param name="marker">通过服务配置委托注册的标记服务。</param>
    private static void AssertRuntimePrepared(
        InitializationPipelineTestArchitecture architecture,
        TrackingEnvironment environment,
        BootstrapMarker marker)
    {
        Assert.Multiple(() =>
        {
            Assert.That(architecture.ObservedEnvironmentInitialized, Is.True);
            Assert.That(architecture.ObservedConfiguredServiceAvailable, Is.True);
            Assert.That(architecture.ObservedEventBusAvailable, Is.True);
            Assert.That(architecture.ObservedContextWasBound, Is.True);
            Assert.That(architecture.ObservedEnvironmentRegistered, Is.True);
            Assert.That(architecture.Context.GetEnvironment(), Is.SameAs(environment));
            Assert.That(architecture.Context.GetService<BootstrapMarker>(), Is.SameAs(marker));
            Assert.That(architecture.CurrentPhase, Is.EqualTo(ArchitecturePhase.Ready));
        });
    }

    /// <summary>
    ///     跟踪初始化期间关键可观察状态的测试架构。
    /// </summary>
    private sealed class InitializationPipelineTestArchitecture : Architecture
    {
        private readonly TrackingEnvironment _environment;
        private readonly BootstrapMarker _marker;

        /// <summary>
        ///     使用可观察环境和标记服务创建测试架构。
        /// </summary>
        /// <param name="environment">用于验证初始化时序的环境对象。</param>
        /// <param name="marker">用于验证服务钩子执行结果的标记服务。</param>
        public InitializationPipelineTestArchitecture(
            TrackingEnvironment environment,
            BootstrapMarker marker)
            : base(environment: environment)
        {
            _environment = environment;
            _marker = marker;
        }

        /// <summary>
        ///     记录用户初始化逻辑执行时环境是否已经准备完成。
        /// </summary>
        public bool ObservedEnvironmentInitialized { get; private set; }

        /// <summary>
        ///     记录自定义服务是否已在用户初始化前注册到容器。
        /// </summary>
        public bool ObservedConfiguredServiceAvailable { get; private set; }

        /// <summary>
        ///     记录内置事件总线是否已在用户初始化前可用。
        /// </summary>
        public bool ObservedEventBusAvailable { get; private set; }

        /// <summary>
        ///     记录当前上下文是否已在用户初始化前绑定到全局游戏上下文表。
        /// </summary>
        public bool ObservedContextWasBound { get; private set; }

        /// <summary>
        ///     记录环境对象是否已在用户初始化前注册到架构上下文。
        /// </summary>
        public bool ObservedEnvironmentRegistered { get; private set; }

        /// <summary>
        ///     为容器注册测试标记服务，用于验证初始化前的服务钩子是否执行。
        /// </summary>
        public override Action<IServiceCollection>? Configurator => services => services.AddSingleton(_marker);

        /// <summary>
        ///     在用户初始化逻辑中采集运行时准备状态。
        /// </summary>
        protected override void OnInitialize()
        {
            ObservedEnvironmentInitialized = _environment.InitializeCallCount == 1;
            ObservedConfiguredServiceAvailable = ReferenceEquals(Context.GetService<BootstrapMarker>(), _marker);
            ObservedEventBusAvailable = Context.GetService<IEventBus>() is not null;
            ObservedContextWasBound = ReferenceEquals(GameContext.GetByType(GetType()), Context);
            ObservedEnvironmentRegistered = ReferenceEquals(Context.GetEnvironment(), _environment);
        }
    }

    /// <summary>
    ///     用于验证环境初始化是否发生的测试环境。
    /// </summary>
    private sealed class TrackingEnvironment : EnvironmentBase
    {
        /// <summary>
        ///     获取测试环境名称。
        /// </summary>
        public override string Name { get; } = "Tracking";

        /// <summary>
        ///     获取环境初始化调用次数。
        /// </summary>
        public int InitializeCallCount { get; private set; }

        /// <summary>
        ///     记录环境初始化次数。
        /// </summary>
        public override void Initialize()
        {
            InitializeCallCount++;
        }
    }

    /// <summary>
    ///     通过服务配置委托注册到容器的测试标记对象。
    /// </summary>
    private sealed class BootstrapMarker
    {
    }
}