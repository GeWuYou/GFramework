using System.Reflection;
using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Enums;
using GFramework.Core.Abstractions.Environment;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Abstractions.Model;
using GFramework.Core.Abstractions.Systems;
using GFramework.Core.Abstractions.Utility;
using GFramework.Core.Environment;

namespace GFramework.Core.Architectures;

/// <summary>
///     架构基类，提供系统、模型、工具等组件的注册与管理功能。
///     专注于生命周期管理、初始化流程控制和架构阶段转换。
///
///     重构说明：此类已重构为协调器模式，将职责委托给专门的管理器：
///     - ArchitectureBootstrapper: 初始化基础设施编排
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
        var resolvedConfiguration = configuration ?? new ArchitectureConfiguration();
        var resolvedEnvironment = environment ?? new DefaultEnvironment();
        var resolvedServices = services ?? new ArchitectureServices();
        _context = context;

        // 初始化 Logger
        LoggerFactoryResolver.Provider = resolvedConfiguration.LoggerProperties.LoggerFactoryProvider;
        _logger = LoggerFactoryResolver.Provider.CreateLogger(GetType().Name);

        // 初始化管理器
        _bootstrapper = new ArchitectureBootstrapper(GetType(), resolvedEnvironment, resolvedServices, _logger);
        _lifecycle = new ArchitectureLifecycle(this, resolvedConfiguration, resolvedServices, _logger);
        _componentRegistry = new ArchitectureComponentRegistry(
            this,
            resolvedConfiguration,
            resolvedServices,
            _lifecycle,
            _logger);
        _modules = new ArchitectureModules(this, resolvedServices, _logger);
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
    ///     初始化基础设施编排器
    /// </summary>
    private readonly ArchitectureBootstrapper _bootstrapper;

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
    ///     注册 CQRS 请求管道行为。
    ///     可以传入开放泛型行为类型，也可以传入绑定到特定请求的封闭行为类型。
    /// </summary>
    /// <typeparam name="TBehavior">行为类型，必须是引用类型</typeparam>
    public void RegisterCqrsPipelineBehavior<TBehavior>() where TBehavior : class
    {
        _modules.RegisterCqrsPipelineBehavior<TBehavior>();
    }

    /// <summary>
    ///     从指定程序集显式注册 CQRS 处理器。
    ///     该入口适用于把拆分到其他模块或扩展包程序集中的 handlers 接入当前架构。
    /// </summary>
    /// <param name="assembly">包含 CQRS 处理器或生成注册器的程序集。</param>
    /// <exception cref="ArgumentNullException"><paramref name="assembly" /> 为 <see langword="null" />。</exception>
    /// <exception cref="InvalidOperationException">当前架构的底层容器已冻结，无法继续注册处理器。</exception>
    public void RegisterCqrsHandlersFromAssembly(Assembly assembly)
    {
        _modules.RegisterCqrsHandlersFromAssembly(assembly);
    }

    /// <summary>
    ///     从多个程序集显式注册 CQRS 处理器。
    ///     适用于在初始化阶段批量接入多个扩展程序集，并沿用容器的去重策略避免重复注册。
    /// </summary>
    /// <param name="assemblies">要接入的程序集集合。</param>
    /// <exception cref="ArgumentNullException"><paramref name="assemblies" /> 为 <see langword="null" />。</exception>
    /// <exception cref="InvalidOperationException">当前架构的底层容器已冻结，无法继续注册处理器。</exception>
    public void RegisterCqrsHandlersFromAssemblies(IEnumerable<Assembly> assemblies)
    {
        _modules.RegisterCqrsHandlersFromAssemblies(assemblies);
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
    ///     注册系统类型，由当前服务集合自动创建实例并接入本轮初始化
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
    ///     注册模型类型，由当前服务集合自动创建实例并接入本轮初始化
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
        _context = await _bootstrapper.PrepareForInitializationAsync(_context, Configurator, asyncMode);

        // === 用户 OnInitialize ===
        _logger.Debug("Calling user OnInitialize()");
        OnInitialize();
        _logger.Debug("User OnInitialize() completed");

        // === 组件初始化阶段 ===
        await _lifecycle.InitializeAllComponentsAsync(asyncMode);

        // === 初始化完成阶段 ===
        _bootstrapper.CompleteInitialization();
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
