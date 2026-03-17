using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Enums;
using GFramework.Core.Abstractions.Environment;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Abstractions.Model;
using GFramework.Core.Abstractions.Systems;
using GFramework.Core.Abstractions.Utility;
using GFramework.Core.Environment;
using GFramework.Core.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace GFramework.Core.Architectures;

/// <summary>
///     架构基类，提供系统、模型、工具等组件的注册与管理功能。
///     专注于生命周期管理、初始化流程控制和架构阶段转换。
///
///     重构说明：此类已重构为协调器模式，将职责委托给专门的管理器：
///     - ArchitectureLifecycle: 生命周期管理
///     - ArchitectureComponentRegistry: 组件注册管理
///     - ArchitectureModules: 模块管理
/// </summary>
public abstract class Architecture : IArchitecture
{
    #region Constructor

    /// <summary>
    ///     构造函数，初始化架构和管理器
    /// </summary>
    /// <param name="configuration">架构配置</param>
    /// <param name="environment">环境配置</param>
    /// <param name="services">服务管理器</param>
    /// <param name="context">架构上下文</param>
    protected Architecture(
        IArchitectureConfiguration? configuration = null,
        IEnvironment? environment = null,
        IArchitectureServices? services = null,
        IArchitectureContext? context = null)
    {
        Configuration = configuration ?? new ArchitectureConfiguration();
        Environment = environment ?? new DefaultEnvironment();
        Services = services ?? new ArchitectureServices();
        _context = context;

        // 初始化 Logger
        LoggerFactoryResolver.Provider = Configuration.LoggerProperties.LoggerFactoryProvider;
        _logger = LoggerFactoryResolver.Provider.CreateLogger(GetType().Name);

        // 初始化管理器
        _lifecycle = new ArchitectureLifecycle(this, Configuration, Services, _logger);
        _componentRegistry = new ArchitectureComponentRegistry(this, Configuration, Services, _lifecycle, _logger);
        _modules = new ArchitectureModules(this, Services, _logger);
    }

    #endregion

    #region Lifecycle Hook Management

    /// <summary>
    ///     注册生命周期钩子
    /// </summary>
    /// <param name="hook">生命周期钩子实例</param>
    /// <returns>注册的钩子实例</returns>
    public IArchitectureLifecycleHook RegisterLifecycleHook(IArchitectureLifecycleHook hook)
    {
        return _lifecycle.RegisterLifecycleHook(hook);
    }

    #endregion

    #region Properties

    /// <summary>
    ///     获取架构配置对象
    /// </summary>
    private IArchitectureConfiguration Configuration { get; }

    /// <summary>
    ///     获取环境配置对象
    /// </summary>
    private IEnvironment Environment { get; }

    /// <summary>
    ///     获取服务管理器
    /// </summary>
    private IArchitectureServices Services { get; }

    /// <summary>
    ///     当前架构的阶段
    /// </summary>
    public ArchitecturePhase CurrentPhase => _lifecycle.CurrentPhase;

    /// <summary>
    ///     架构上下文
    /// </summary>
    public IArchitectureContext Context => _context!;

    /// <summary>
    ///     获取一个布尔值，指示当前架构是否处于就绪状态
    /// </summary>
    public bool IsReady => _lifecycle.IsReady;

    /// <summary>
    ///     获取用于配置服务集合的委托
    ///     默认实现返回null，子类可以重写此属性以提供自定义配置逻辑
    /// </summary>
    public virtual Action<IServiceCollection>? Configurator => null;

    /// <summary>
    ///     阶段变更事件（用于测试和扩展）
    /// </summary>
    public event Action<ArchitecturePhase>? PhaseChanged
    {
        add => _lifecycle.PhaseChanged += value;
        remove => _lifecycle.PhaseChanged -= value;
    }

    #endregion

    #region Fields

    /// <summary>
    ///     日志记录器实例
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    ///     架构上下文实例
    /// </summary>
    private IArchitectureContext? _context;

    /// <summary>
    ///     生命周期管理器
    /// </summary>
    private readonly ArchitectureLifecycle _lifecycle;

    /// <summary>
    ///     组件注册管理器
    /// </summary>
    private readonly ArchitectureComponentRegistry _componentRegistry;

    /// <summary>
    ///     模块管理器
    /// </summary>
    private readonly ArchitectureModules _modules;

    #endregion

    #region Module Management

    /// <summary>
    ///     注册中介行为管道
    ///     用于配置Mediator框架的行为拦截和处理逻辑
    /// </summary>
    /// <typeparam name="TBehavior">行为类型，必须是引用类型</typeparam>
    public void RegisterMediatorBehavior<TBehavior>() where TBehavior : class
    {
        _modules.RegisterMediatorBehavior<TBehavior>();
    }

    /// <summary>
    ///     安装架构模块
    /// </summary>
    /// <param name="module">要安装的模块</param>
    /// <returns>安装的模块实例</returns>
    public IArchitectureModule InstallModule(IArchitectureModule module)
    {
        return _modules.InstallModule(module);
    }

    #endregion

    #region Component Registration

    /// <summary>
    ///     注册一个系统到架构中
    /// </summary>
    /// <typeparam name="TSystem">要注册的系统类型</typeparam>
    /// <param name="system">要注册的系统实例</param>
    /// <returns>注册成功的系统实例</returns>
    public TSystem RegisterSystem<TSystem>(TSystem system) where TSystem : ISystem
    {
        return _componentRegistry.RegisterSystem(system);
    }

    /// <summary>
    ///     注册系统类型，由 DI 容器自动创建实例
    /// </summary>
    /// <typeparam name="T">系统类型</typeparam>
    /// <param name="onCreated">可选的实例创建后回调</param>
    public void RegisterSystem<T>(Action<T>? onCreated = null) where T : class, ISystem
    {
        _componentRegistry.RegisterSystem(onCreated);
    }

    /// <summary>
    ///     注册一个模型到架构中
    /// </summary>
    /// <typeparam name="TModel">要注册的模型类型</typeparam>
    /// <param name="model">要注册的模型实例</param>
    /// <returns>注册成功的模型实例</returns>
    public TModel RegisterModel<TModel>(TModel model) where TModel : IModel
    {
        return _componentRegistry.RegisterModel(model);
    }

    /// <summary>
    ///     注册模型类型，由 DI 容器自动创建实例
    /// </summary>
    /// <typeparam name="T">模型类型</typeparam>
    /// <param name="onCreated">可选的实例创建后回调</param>
    public void RegisterModel<T>(Action<T>? onCreated = null) where T : class, IModel
    {
        _componentRegistry.RegisterModel(onCreated);
    }

    /// <summary>
    ///     注册一个工具到架构中
    /// </summary>
    /// <typeparam name="TUtility">要注册的工具类型</typeparam>
    /// <param name="utility">要注册的工具实例</param>
    /// <returns>注册成功的工具实例</returns>
    public TUtility RegisterUtility<TUtility>(TUtility utility) where TUtility : IUtility
    {
        return _componentRegistry.RegisterUtility(utility);
    }

    /// <summary>
    ///     注册工具类型，由 DI 容器自动创建实例
    /// </summary>
    /// <typeparam name="T">工具类型</typeparam>
    /// <param name="onCreated">可选的实例创建后回调</param>
    public void RegisterUtility<T>(Action<T>? onCreated = null) where T : class, IUtility
    {
        _componentRegistry.RegisterUtility(onCreated);
    }

    #endregion

    #region Initialization

    /// <summary>
    ///     抽象初始化方法，由子类重写以进行自定义初始化操作
    /// </summary>
    protected abstract void OnInitialize();

    /// <summary>
    ///     同步初始化方法，阻塞当前线程直到初始化完成
    /// </summary>
    public void Initialize()
    {
        try
        {
            InitializeInternalAsync(false).GetAwaiter().GetResult();
        }
        catch (Exception e)
        {
            _logger.Error("Architecture initialization failed:", e);
            _lifecycle.MarkAsFailed(e);
            throw;
        }
    }

    /// <summary>
    ///     异步初始化方法，返回Task以便调用者可以等待初始化完成
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            await InitializeInternalAsync(true);
        }
        catch (Exception e)
        {
            _logger.Error("Architecture initialization failed:", e);
            _lifecycle.MarkAsFailed(e);
            throw;
        }
    }

    /// <summary>
    ///     异步初始化架构内部组件
    /// </summary>
    /// <param name="asyncMode">是否启用异步模式</param>
    private async Task InitializeInternalAsync(bool asyncMode)
    {
        // === 基础环境初始化 ===
        Environment.Initialize();

        // 注册内置服务模块
        Services.ModuleManager.RegisterBuiltInModules(Services.Container);

        // 将 Environment 注册到容器
        if (!Services.Container.Contains<IEnvironment>())
            Services.Container.RegisterPlurality(Environment);

        // 初始化架构上下文
        _context ??= new ArchitectureContext(Services.Container);
        GameContext.Bind(GetType(), _context);

        // 为服务设置上下文
        Services.SetContext(_context);
        if (Configurator is null)
        {
            _logger.Debug("Mediator-based cqrs will not take effect without the service setter configured!");
        }

        // 执行服务钩子
        Services.Container.ExecuteServicesHook(Configurator);

        // 初始化服务模块
        await Services.ModuleManager.InitializeAllAsync(asyncMode);

        // === 用户 OnInitialize ===
        _logger.Debug("Calling user OnInitialize()");
        OnInitialize();
        _logger.Debug("User OnInitialize() completed");

        // === 组件初始化阶段 ===
        await _lifecycle.InitializeAllComponentsAsync(asyncMode);

        // === 初始化完成阶段 ===
        Services.Container.Freeze();
        _logger.Info("IOC container frozen");

        _lifecycle.MarkAsReady();
        _logger.Info($"Architecture {GetType().Name} is ready - all components initialized");
    }

    /// <summary>
    ///     等待架构初始化完成（Ready 阶段）
    /// </summary>
    public Task WaitUntilReadyAsync()
    {
        return _lifecycle.WaitUntilReadyAsync();
    }

    #endregion

    #region Destruction

    /// <summary>
    ///     异步销毁架构及所有组件
    /// </summary>
    public virtual async ValueTask DestroyAsync()
    {
        await _lifecycle.DestroyAsync();
    }

    /// <summary>
    ///     销毁架构并清理所有组件资源（同步方法，保留用于向后兼容）
    /// </summary>
    [Obsolete("建议使用 DestroyAsync() 以支持异步清理")]
    public virtual void Destroy()
    {
        _lifecycle.Destroy();
    }

    #endregion
}