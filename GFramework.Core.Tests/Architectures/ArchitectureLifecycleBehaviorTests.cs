using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Enums;
using GFramework.Core.Abstractions.Lifecycle;
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
            PhaseChanged += phase => PhaseHistory.Add(phase);
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
            PhaseChanged += phase => PhaseHistory.Add(phase);
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