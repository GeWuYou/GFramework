using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Enums;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Abstractions.Model;
using GFramework.Core.Abstractions.Systems;
using GFramework.Core.Abstractions.Utility;
using GFramework.Core.Architectures;
using GFramework.Core.Logging;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     验证 Architecture 通过 <c>ArchitectureComponentRegistry</c> 暴露出的组件注册行为。
///     这些测试覆盖实例注册、工厂注册、上下文注入、生命周期初始化和 Ready 后注册约束，
///     用于保护组件注册器在继续重构后的既有契约。
/// </summary>
[TestFixture]
public class ArchitectureComponentRegistryBehaviorTests
{
    /// <summary>
    ///     初始化日志工厂和全局上下文状态。
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        LoggerFactoryResolver.Provider = new ConsoleLoggerFactoryProvider();
        GameContext.Clear();
    }

    /// <summary>
    ///     清理测试过程中绑定到全局表的架构上下文。
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        GameContext.Clear();
    }

    /// <summary>
    ///     验证系统实例注册会注入上下文并参与生命周期初始化。
    /// </summary>
    [Test]
    public async Task RegisterSystem_Instance_Should_Set_Context_And_Initialize_System()
    {
        var system = new TrackingSystem();
        var architecture = new RegistryTestArchitecture(target => target.RegisterSystem(system));

        await architecture.InitializeAsync();

        Assert.Multiple(() =>
        {
            Assert.That(system.GetContext(), Is.SameAs(architecture.Context));
            Assert.That(system.InitializeCallCount, Is.EqualTo(1));
            Assert.That(architecture.Context.GetSystem<TrackingSystem>(), Is.SameAs(system));
        });

        await architecture.DestroyAsync();
    }

    /// <summary>
    ///     验证模型实例注册会注入上下文并参与生命周期初始化。
    /// </summary>
    [Test]
    public async Task RegisterModel_Instance_Should_Set_Context_And_Initialize_Model()
    {
        var model = new TrackingModel();
        var architecture = new RegistryTestArchitecture(target => target.RegisterModel(model));

        await architecture.InitializeAsync();

        Assert.Multiple(() =>
        {
            Assert.That(model.GetContext(), Is.SameAs(architecture.Context));
            Assert.That(model.InitializeCallCount, Is.EqualTo(1));
            Assert.That(architecture.Context.GetModel<TrackingModel>(), Is.SameAs(model));
        });

        await architecture.DestroyAsync();
    }

    /// <summary>
    ///     验证上下文工具注册会注入上下文并参与生命周期初始化。
    /// </summary>
    [Test]
    public async Task RegisterUtility_Instance_Should_Set_Context_For_ContextUtility()
    {
        var utility = new TrackingContextUtility();
        var architecture = new RegistryTestArchitecture(target => target.RegisterUtility(utility));

        await architecture.InitializeAsync();

        Assert.Multiple(() =>
        {
            Assert.That(utility.GetContext(), Is.SameAs(architecture.Context));
            Assert.That(utility.InitializeCallCount, Is.EqualTo(1));
            Assert.That(architecture.Context.GetUtility<TrackingContextUtility>(), Is.SameAs(utility));
        });

        await architecture.DestroyAsync();
    }

    /// <summary>
    ///     验证普通工具的工厂注册会在首次解析时创建单例并执行创建回调。
    /// </summary>
    [Test]
    public async Task RegisterUtility_Type_Should_Create_Singleton_And_Invoke_Callback()
    {
        FactoryCreatedUtility? callbackInstance = null;
        var architecture = new RegistryTestArchitecture(target =>
            target.RegisterUtility<FactoryCreatedUtility>(created => callbackInstance = created));

        await architecture.InitializeAsync();

        var first = architecture.Context.GetUtility<FactoryCreatedUtility>();
        var second = architecture.Context.GetUtility<FactoryCreatedUtility>();

        Assert.Multiple(() =>
        {
            Assert.That(callbackInstance, Is.SameAs(first));
            Assert.That(second, Is.SameAs(first));
        });

        await architecture.DestroyAsync();
    }

    /// <summary>
    ///     验证系统类型注册会在初始化期间物化实例、注入构造函数依赖并执行创建回调。
    /// </summary>
    [Test]
    public async Task RegisterSystem_Type_Should_Create_Instance_During_Initialization()
    {
        var dependency = new ConstructorDependency("system-dependency");
        FactoryCreatedSystem? callbackInstance = null;
        var architecture = new RegistryTestArchitecture(
            target => target.RegisterSystem<FactoryCreatedSystem>(created => callbackInstance = created),
            services => services.AddSingleton(dependency));

        await architecture.InitializeAsync();

        var resolved = architecture.Context.GetSystem<FactoryCreatedSystem>();

        Assert.Multiple(() =>
        {
            Assert.That(callbackInstance, Is.Not.Null);
            Assert.That(resolved, Is.SameAs(callbackInstance));
            Assert.That(resolved.Dependency, Is.SameAs(dependency));
            Assert.That(resolved.GetContext(), Is.SameAs(architecture.Context));
            Assert.That(resolved.InitializeCallCount, Is.EqualTo(1));
        });

        await architecture.DestroyAsync();
    }

    /// <summary>
    ///     验证模型类型注册会在初始化期间物化实例、注入构造函数依赖并执行创建回调。
    /// </summary>
    [Test]
    public async Task RegisterModel_Type_Should_Create_Instance_During_Initialization()
    {
        var dependency = new ConstructorDependency("model-dependency");
        FactoryCreatedModel? callbackInstance = null;
        var architecture = new RegistryTestArchitecture(
            target => target.RegisterModel<FactoryCreatedModel>(created => callbackInstance = created),
            services => services.AddSingleton(dependency));

        await architecture.InitializeAsync();

        var resolved = architecture.Context.GetModel<FactoryCreatedModel>();

        Assert.Multiple(() =>
        {
            Assert.That(callbackInstance, Is.Not.Null);
            Assert.That(resolved, Is.SameAs(callbackInstance));
            Assert.That(resolved.Dependency, Is.SameAs(dependency));
            Assert.That(resolved.GetContext(), Is.SameAs(architecture.Context));
            Assert.That(resolved.InitializeCallCount, Is.EqualTo(1));
        });

        await architecture.DestroyAsync();
    }

    /// <summary>
    ///     验证预冻结阶段通过实现类型注册的单例依赖会在同一轮组件激活中复用同一个实例。
    ///     该回归测试用于保护 <see cref="ArchitectureComponentActivator" /> 的共享单例缓存，避免系统和模型分别创建重复单例。
    /// </summary>
    [Test]
    public async Task RegisterSystem_And_Model_Type_Should_Reuse_ImplementationType_Singleton_During_Activation()
    {
        var counter = new DependencyConstructionCounter();
        var architecture = new RegistryTestArchitecture(
            target =>
            {
                target.RegisterSystem<ImplementationTypeDependencySystem>();
                target.RegisterModel<ImplementationTypeDependencyModel>();
            },
            services =>
            {
                services.AddSingleton(counter);
                services.AddSingleton<ImplementationTypeSharedDependency>();
            });

        await architecture.InitializeAsync();

        var system = architecture.Context.GetSystem<ImplementationTypeDependencySystem>();
        var model = architecture.Context.GetModel<ImplementationTypeDependencyModel>();

        Assert.Multiple(() =>
        {
            Assert.That(counter.CreationCount, Is.EqualTo(1));
            Assert.That(system.Dependency, Is.SameAs(model.Dependency));
        });

        await architecture.DestroyAsync();
    }

    /// <summary>
    ///     验证预冻结阶段通过工厂注册的单例依赖会在同一轮组件激活中复用同一个实例。
    ///     该回归测试覆盖 <c>ImplementationFactory</c> 描述符路径，避免用户工厂在初始化时被重复调用。
    /// </summary>
    [Test]
    public async Task RegisterSystem_And_Model_Type_Should_Reuse_Factory_Singleton_During_Activation()
    {
        var creationCount = 0;
        var architecture = new RegistryTestArchitecture(
            target =>
            {
                target.RegisterSystem<FactoryDependencySystem>();
                target.RegisterModel<FactoryDependencyModel>();
            },
            services =>
            {
                services.AddSingleton(_ =>
                {
                    creationCount++;
                    return new FactorySharedDependency();
                });
            });

        await architecture.InitializeAsync();

        var system = architecture.Context.GetSystem<FactoryDependencySystem>();
        var model = architecture.Context.GetModel<FactoryDependencyModel>();

        Assert.Multiple(() =>
        {
            Assert.That(creationCount, Is.EqualTo(1));
            Assert.That(system.Dependency, Is.SameAs(model.Dependency));
        });

        await architecture.DestroyAsync();
    }

    /// <summary>
    ///     验证 Ready 阶段后不允许继续注册 Utility，保持与系统和模型一致的约束。
    /// </summary>
    [Test]
    public async Task RegisterUtility_After_Ready_Should_Throw_InvalidOperationException()
    {
        var architecture = new RegistryTestArchitecture(_ => { });
        await architecture.InitializeAsync();

        Assert.That(
            () => architecture.RegisterUtility(new TrackingContextUtility()),
            Throws.InvalidOperationException.With.Message.EqualTo(
                "Cannot register utility after Architecture is Ready"));

        await architecture.DestroyAsync();
    }

    /// <summary>
    ///     用于测试组件注册行为的最小架构实现。
    /// </summary>
    private sealed class RegistryTestArchitecture : Architecture
    {
        private readonly Action<IServiceCollection>? _configurator;
        private readonly Action<RegistryTestArchitecture> _registrationAction;

        /// <summary>
        ///     创建一个可选地附带服务配置逻辑的测试架构。
        /// </summary>
        /// <param name="registrationAction">初始化阶段执行的组件注册逻辑。</param>
        /// <param name="configurator">初始化前执行的服务配置逻辑。</param>
        public RegistryTestArchitecture(
            Action<RegistryTestArchitecture> registrationAction,
            Action<IServiceCollection>? configurator = null)
        {
            _registrationAction = registrationAction;
            _configurator = configurator;
        }

        /// <summary>
        ///     返回测试注入的服务配置逻辑。
        /// </summary>
        public override Action<IServiceCollection>? Configurator => _configurator;

        /// <summary>
        ///     在初始化阶段执行测试注入的注册逻辑。
        /// </summary>
        protected override void OnInitialize()
        {
            _registrationAction(this);
        }
    }

    /// <summary>
    ///     记录初始化与上下文注入情况的测试系统。
    /// </summary>
    private sealed class TrackingSystem : ISystem
    {
        private IArchitectureContext _context = null!;

        /// <summary>
        ///     获取系统初始化调用次数。
        /// </summary>
        public int InitializeCallCount { get; private set; }

        /// <summary>
        ///     记录初始化调用。
        /// </summary>
        public void Initialize()
        {
            InitializeCallCount++;
        }

        /// <summary>
        ///     该测试系统不关心阶段变更。
        /// </summary>
        /// <param name="phase">当前架构阶段。</param>
        public void OnArchitecturePhase(ArchitecturePhase phase)
        {
        }

        /// <summary>
        ///     存储注入的架构上下文。
        /// </summary>
        /// <param name="context">架构上下文。</param>
        public void SetContext(IArchitectureContext context)
        {
            _context = context;
        }

        /// <summary>
        ///     返回当前持有的架构上下文。
        /// </summary>
        public IArchitectureContext GetContext()
        {
            return _context;
        }

        /// <summary>
        ///     该测试系统没有额外销毁逻辑。
        /// </summary>
        public void Destroy()
        {
        }
    }

    /// <summary>
    ///     记录初始化与上下文注入情况的测试模型。
    /// </summary>
    private sealed class TrackingModel : IModel
    {
        private IArchitectureContext _context = null!;

        /// <summary>
        ///     获取模型初始化调用次数。
        /// </summary>
        public int InitializeCallCount { get; private set; }

        /// <summary>
        ///     记录初始化调用。
        /// </summary>
        public void Initialize()
        {
            InitializeCallCount++;
        }

        /// <summary>
        ///     该测试模型不关心阶段变更。
        /// </summary>
        /// <param name="phase">当前架构阶段。</param>
        public void OnArchitecturePhase(ArchitecturePhase phase)
        {
        }

        /// <summary>
        ///     存储注入的架构上下文。
        /// </summary>
        /// <param name="context">架构上下文。</param>
        public void SetContext(IArchitectureContext context)
        {
            _context = context;
        }

        /// <summary>
        ///     返回当前持有的架构上下文。
        /// </summary>
        public IArchitectureContext GetContext()
        {
            return _context;
        }
    }

    /// <summary>
    ///     记录初始化与上下文注入情况的测试上下文工具。
    /// </summary>
    private sealed class TrackingContextUtility : IContextUtility
    {
        private IArchitectureContext _context = null!;

        /// <summary>
        ///     获取工具初始化调用次数。
        /// </summary>
        public int InitializeCallCount { get; private set; }

        /// <summary>
        ///     记录初始化调用。
        /// </summary>
        public void Initialize()
        {
            InitializeCallCount++;
        }

        /// <summary>
        ///     存储注入的架构上下文。
        /// </summary>
        /// <param name="context">架构上下文。</param>
        public void SetContext(IArchitectureContext context)
        {
            _context = context;
        }

        /// <summary>
        ///     返回当前持有的架构上下文。
        /// </summary>
        public IArchitectureContext GetContext()
        {
            return _context;
        }

        /// <summary>
        ///     该测试工具没有额外销毁逻辑。
        /// </summary>
        public void Destroy()
        {
        }
    }

    /// <summary>
    ///     用于验证普通工厂注册路径的简单工具。
    /// </summary>
    private sealed class FactoryCreatedUtility : IUtility
    {
    }

    /// <summary>
    ///     用于验证构造函数依赖注入的简单依赖对象。
    /// </summary>
    private sealed class ConstructorDependency(string name)
    {
        /// <summary>
        ///     获取依赖对象名称。
        /// </summary>
        public string Name { get; } = name;
    }

    /// <summary>
    ///     用于验证系统类型注册路径的工厂创建系统。
    /// </summary>
    private sealed class FactoryCreatedSystem(ConstructorDependency dependency) : ISystem
    {
        private IArchitectureContext _context = null!;

        /// <summary>
        ///     获取构造函数注入的依赖对象。
        /// </summary>
        public ConstructorDependency Dependency { get; } = dependency;

        /// <summary>
        ///     获取初始化调用次数。
        /// </summary>
        public int InitializeCallCount { get; private set; }

        public void Initialize()
        {
            InitializeCallCount++;
        }

        public void Destroy()
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
    ///     用于验证模型类型注册路径的工厂创建模型。
    /// </summary>
    private sealed class FactoryCreatedModel(ConstructorDependency dependency) : IModel
    {
        private IArchitectureContext _context = null!;

        /// <summary>
        ///     获取构造函数注入的依赖对象。
        /// </summary>
        public ConstructorDependency Dependency { get; } = dependency;

        /// <summary>
        ///     获取初始化调用次数。
        /// </summary>
        public int InitializeCallCount { get; private set; }

        public void Initialize()
        {
            InitializeCallCount++;
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
    ///     统计实现类型单例在预冻结激活阶段的构造次数。
    /// </summary>
    private sealed class DependencyConstructionCounter
    {
        /// <summary>
        ///     获取共享依赖被构造的次数。
        /// </summary>
        public int CreationCount { get; private set; }

        /// <summary>
        ///     记录一次新的依赖构造。
        /// </summary>
        public void RecordCreation()
        {
            CreationCount++;
        }
    }

    /// <summary>
    ///     用于覆盖 ImplementationType 单例描述符路径的共享依赖。
    /// </summary>
    private sealed class ImplementationTypeSharedDependency
    {
        /// <summary>
        ///     创建共享依赖并记录构造次数。
        /// </summary>
        /// <param name="counter">用于统计构造次数的计数器。</param>
        public ImplementationTypeSharedDependency(DependencyConstructionCounter counter)
        {
            counter.RecordCreation();
        }
    }

    /// <summary>
    ///     用于覆盖 ImplementationType 单例复用路径的测试系统。
    /// </summary>
    private sealed class ImplementationTypeDependencySystem(ImplementationTypeSharedDependency dependency) : ISystem
    {
        private IArchitectureContext _context = null!;

        /// <summary>
        ///     获取构造函数注入的共享依赖。
        /// </summary>
        public ImplementationTypeSharedDependency Dependency { get; } = dependency;

        public void Initialize()
        {
        }

        public void Destroy()
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
    ///     用于覆盖 ImplementationType 单例复用路径的测试模型。
    /// </summary>
    private sealed class ImplementationTypeDependencyModel(ImplementationTypeSharedDependency dependency) : IModel
    {
        private IArchitectureContext _context = null!;

        /// <summary>
        ///     获取构造函数注入的共享依赖。
        /// </summary>
        public ImplementationTypeSharedDependency Dependency { get; } = dependency;

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
    ///     用于覆盖 ImplementationFactory 单例描述符路径的共享依赖。
    /// </summary>
    private sealed class FactorySharedDependency
    {
    }

    /// <summary>
    ///     用于覆盖 ImplementationFactory 单例复用路径的测试系统。
    /// </summary>
    private sealed class FactoryDependencySystem(FactorySharedDependency dependency) : ISystem
    {
        private IArchitectureContext _context = null!;

        /// <summary>
        ///     获取构造函数注入的共享依赖。
        /// </summary>
        public FactorySharedDependency Dependency { get; } = dependency;

        public void Initialize()
        {
        }

        public void Destroy()
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
    ///     用于覆盖 ImplementationFactory 单例复用路径的测试模型。
    /// </summary>
    private sealed class FactoryDependencyModel(FactorySharedDependency dependency) : IModel
    {
        private IArchitectureContext _context = null!;

        /// <summary>
        ///     获取构造函数注入的共享依赖。
        /// </summary>
        public FactorySharedDependency Dependency { get; } = dependency;

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
}
