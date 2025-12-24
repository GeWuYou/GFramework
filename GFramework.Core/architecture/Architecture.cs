using GFramework.Core.events;
using GFramework.Core.ioc;
using GFramework.Core.logging;
using GFramework.Core.model;
using GFramework.Core.system;
using GFramework.Core.utility;

namespace GFramework.Core.architecture;

/// <summary>
/// 架构基类，提供系统、模型、工具等组件的注册与管理功能。
/// 专注于生命周期管理、初始化流程控制和架构阶段转换。
/// 不直接提供业务操作方法，业务操作通过 ArchitectureRuntime 提供。
/// </summary>
public abstract class Architecture(
    IArchitectureConfiguration? configuration = null,
    IArchitectureServices? services = null,
    IArchitectureContext? context = null
    ) 
    : IArchitecture, IArchitectureLifecycle
{
    /// <summary>
    /// 获取架构配置对象
    /// </summary>
    /// <value>
    /// 返回一个IArchitectureConfiguration接口的实例，默认为DefaultArchitectureConfiguration类型
    /// </value>
    private IArchitectureConfiguration Configuration { get; } = configuration ?? new ArchitectureConfiguration();

    /// <summary>
    /// 获取架构服务对象
    /// </summary>
    /// <value>
    /// 返回一个IArchitectureServices接口的实例，默认为DefaultArchitectureServices类型
    /// </value>
    private IArchitectureServices Services { get; } = services ?? new ArchitectureServices();

    /// <summary>
    /// 获取依赖注入容器
    /// </summary>
    /// <value>
    /// 通过Services属性获取的IArchitectureServices中的Container属性
    /// </value>
    private IIocContainer Container => Services.Container;
    
    /// <summary>
    /// 获取类型事件系统
    /// </summary>
    /// <value>
    /// 通过Services属性获取的IArchitectureServices中的TypeEventSystem属性
    /// </value>
    private ITypeEventSystem TypeEventSystem => Services.TypeEventSystem;

    /// <summary>
    /// 获取架构运行时实例
    /// </summary>
    /// <value>
    /// 统一的操作入口，负责命令、查询、事件的执行
    /// </value>
    public IArchitectureRuntime Runtime { get; private set; } = null!;

    #region Fields and Properties

    /// <summary>
    ///     存储尚未初始化的模型集合，在初始化阶段统一调用Init方法
    /// </summary>
    private readonly HashSet<IModel> _mModels = [];

    /// <summary>
    ///     存储尚未初始化的系统集合，在初始化阶段统一调用Init方法
    /// </summary>
    private readonly HashSet<ISystem> _mSystems = [];

    /// <summary>
    ///     存储所有已注册的系统，用于销毁
    /// </summary>
    private readonly HashSet<ISystem> _allSystems = [];

    /// <summary>
    ///     标记架构是否已初始化完成
    /// </summary>
    private bool _mInited;

    /// <summary>
    ///     生命周期感知对象列表
    /// </summary>
    private readonly List<IArchitectureLifecycle> _lifecycleHooks = [];

    /// <summary>
    ///     当前架构的阶段
    /// </summary>
    private ArchitecturePhase CurrentPhase { get; set; }

    /// <summary>
    /// 日志记录器实例，用于记录应用程序的运行日志
    /// </summary>
    private ILogger _logger = null!;

    private IArchitectureContext? _context = context;
    
    public IArchitectureContext Context => _context!;

    #endregion

    #region Lifecycle Management

    /// <summary>
    /// 进入指定的架构阶段，并执行相应的生命周期管理操作
    /// </summary>
    /// <param name="next">要进入的下一个架构阶段</param>
    /// <exception cref="InvalidOperationException">当阶段转换不被允许时抛出异常</exception>
    private void EnterPhase(ArchitecturePhase next)
    {
        var logger = Configuration.LoggerFactory.GetLogger(nameof(Architecture));
        if (Configuration.Options.StrictPhaseValidation &&
            (!ArchitectureConstants.PhaseTransitions.TryGetValue(CurrentPhase, out var allowed) ||
             !allowed.Contains(next)))
        {
            // 验证阶段转换是否合法
            var errorMsg = $"Invalid phase transition: {CurrentPhase} -> {next}";
            logger.Fatal(errorMsg);
            throw new InvalidOperationException(errorMsg);
        }

        var previousPhase = CurrentPhase;
        CurrentPhase = next;

        if (previousPhase != next)
        {
            logger.Info($"Architecture phase changed: {previousPhase} -> {next}");
        }

        NotifyPhase(next);

        // 通知所有架构阶段感知对象阶段变更
        foreach (var obj in Container.GetAll<IArchitecturePhaseAware>())
        {
            logger.Debug($"Notifying phase-aware object {obj.GetType().Name} of phase change to {next}");
            obj.OnArchitecturePhase(next);
        }
    }

    /// <summary>
    /// 通知所有生命周期钩子当前阶段变更
    /// </summary>
    /// <param name="phase">当前架构阶段</param>
    private void NotifyPhase(ArchitecturePhase phase)
    {
        foreach (var hook in _lifecycleHooks)
            hook.OnPhase(phase, this);
    }

    /// <summary>
    /// 注册生命周期钩子
    /// </summary>
    /// <param name="hook">生命周期钩子实例</param>
    public void RegisterLifecycleHook(IArchitectureLifecycle hook)
    {
        if (CurrentPhase >= ArchitecturePhase.Ready && !Configuration.Options.AllowLateRegistration)
            throw new InvalidOperationException(
                "Cannot register lifecycle hook after architecture is Ready");
        _lifecycleHooks.Add(hook);
    }


    /// <summary>
    ///     抽象初始化方法，由子类重写以进行自定义初始化操作
    /// </summary>
    protected abstract void Init();

    /// <summary>
    /// 销毁架构并清理所有系统资源
    /// </summary>
    /// <remarks>
    /// 此函数负责有序地销毁架构中的所有系统组件，并发送相应的生命周期事件。
    /// 函数会确保只执行一次销毁操作，避免重复销毁。
    /// </remarks>
    public virtual void Destroy()
    {
        var logger = Configuration.LoggerFactory.GetLogger(nameof(Architecture));

        // 检查当前阶段，如果已经处于销毁或已销毁状态则直接返回
        if (CurrentPhase >= ArchitecturePhase.Destroying)
        {
            logger.Warn("Architecture destroy called but already in destroying/destroyed state");
            return;
        }

        // 进入销毁阶段并发送销毁开始事件
        logger.Info("Starting architecture destruction");
        EnterPhase(ArchitecturePhase.Destroying);
        TypeEventSystem.Send(new ArchitectureEvents.ArchitectureDestroyingEvent());

        // 销毁所有系统组件并清空系统列表
        logger.Info($"Destroying {_allSystems.Count} systems");
        foreach (var system in _allSystems)
        {
            logger.Debug($"Destroying system: {system.GetType().Name}");
            system.Destroy();
        }

        _allSystems.Clear();

        // 进入已销毁阶段并发送销毁完成事件
        EnterPhase(ArchitecturePhase.Destroyed);
        TypeEventSystem.Send(new ArchitectureEvents.ArchitectureDestroyedEvent());
        logger.Info("Architecture destruction completed");
    }

    #endregion

    #region Module Management

    /// <summary>
    /// 安装架构模块
    /// </summary>
    /// <param name="module">要安装的模块</param>
    public void InstallModule(IArchitectureModule module)
    {
        var logger = Configuration.LoggerFactory.GetLogger(nameof(Architecture));
        logger.Debug($"Installing module: {module.GetType().Name}");
        RegisterLifecycleHook(module);
        Container.RegisterPlurality(module);
        module.Install(this);
        logger.Info($"Module installed: {module.GetType().Name}");
    }

    #endregion

    #region Component Registration

    public void Initialize()
    {
        _logger = Configuration.LoggerFactory.GetLogger(GetType().Name);
        _context ??= new ArchitectureContext(Container, TypeEventSystem, _logger);
        
        // 创建架构运行时实例
        Runtime = new ArchitectureRuntime(_context);
        ((ArchitectureContext)_context).Runtime = Runtime;
        // 调用用户实现的初始化
        Init();

        // == Model Init ==
        EnterPhase(ArchitecturePhase.BeforeModelInit);
        _logger.Info($"Initializing {_mModels.Count} models");

        // 初始化所有已注册但尚未初始化的模型
        foreach (var model in _mModels)
        {
            _logger.Debug($"Initializing model: {model.GetType().Name}");
            model.Init();
        }

        _mModels.Clear();
        EnterPhase(ArchitecturePhase.AfterModelInit);
        _logger.Info("All models initialized");

        // == System Init ==
        EnterPhase(ArchitecturePhase.BeforeSystemInit);
        _logger.Info($"Initializing {_mSystems.Count} systems");

        // 初始化所有已注册但尚未初始化的系统
        foreach (var system in _mSystems)
        {
            _logger.Debug($"Initializing system: {system.GetType().Name}");
            system.Init();
        }

        _mSystems.Clear();
        EnterPhase(ArchitecturePhase.AfterSystemInit);
        _logger.Info("All systems initialized");

        // == Finalize ==
        // 冻结IOC容器，不允许 anymore
        Container.Freeze();
        _mInited = true;
        EnterPhase(ArchitecturePhase.Ready);
        // 发送架构生命周期就绪事件
        TypeEventSystem.Send(new ArchitectureEvents.ArchitectureLifecycleReadyEvent());
        _logger.Info($"Architecture {GetType().Name} is ready - all components initialized");
    }

    public async Task InitializeAsync()
    {
        _logger = Configuration.LoggerFactory.GetLogger(GetType().Name);
        _context ??= new ArchitectureContext(Container, TypeEventSystem, _logger);
        
        // 创建架构运行时实例
        Runtime = new ArchitectureRuntime(_context);
        ((ArchitectureContext)_context).Runtime = Runtime;
        // 调用用户实现的初始化
        Init();

        // == Model Init ==
        EnterPhase(ArchitecturePhase.BeforeModelInit);
        _logger.Info($"Initializing {_mModels.Count} models");

        // 异步初始化所有已注册但尚未初始化的模型
        foreach (var model in _mModels)
        {
            _logger.Debug($"Initializing model: {model.GetType().Name}");
            if (model is IAsyncInitializable asyncModel)
                await asyncModel.InitializeAsync();
            else
                model.Init();
        }

        _mModels.Clear();
        EnterPhase(ArchitecturePhase.AfterModelInit);
        _logger.Info("All models initialized");

        // == System Init ==
        EnterPhase(ArchitecturePhase.BeforeSystemInit);
        _logger.Info($"Initializing {_mSystems.Count} systems");

        // 异步初始化所有已注册但尚未初始化的系统
        foreach (var system in _mSystems)
        {
            _logger.Debug($"Initializing system: {system.GetType().Name}");
            if (system is IAsyncInitializable asyncSystem)
                await asyncSystem.InitializeAsync();
            else
                system.Init();
        }

        _mSystems.Clear();
        EnterPhase(ArchitecturePhase.AfterSystemInit);
        _logger.Info("All systems initialized");

        // == Finalize ==
        Container.Freeze();
        _mInited = true;
        EnterPhase(ArchitecturePhase.Ready);
        TypeEventSystem.Send(new ArchitectureEvents.ArchitectureLifecycleReadyEvent());
        _logger.Info($"Architecture {GetType().Name} is ready - all components initialized");
    }

    /// <summary>
    ///     注册一个系统到架构中。
    ///     若当前未初始化，则暂存至待初始化列表；否则立即初始化该系统。
    /// </summary>
    /// <typeparam name="TSystem">要注册的系统类型</typeparam>
    /// <param name="system">要注册的系统实例</param>
    public void RegisterSystem<TSystem>(TSystem system) where TSystem : ISystem
    {
        if (CurrentPhase >= ArchitecturePhase.Ready && !Configuration.Options.AllowLateRegistration)
        {
            var errorMsg = "Cannot register system after Architecture is Ready";
            _logger.Error(errorMsg);
            throw new InvalidOperationException(errorMsg);
        }

        _logger.Debug($"Registering system: {typeof(TSystem).Name}");
        system.SetContext(Context);
        Container.RegisterPlurality(system);
        _allSystems.Add(system);
        if (!_mInited)
            _mSystems.Add(system);
        else
        {
            _logger.Debug($"Immediately initializing system: {typeof(TSystem).Name}");
            system.Init();
        }

        _logger.Info($"System registered: {typeof(TSystem).Name}");
    }

    /// <summary>
    ///     注册一个模型到架构中。
    ///     若当前未初始化，则暂存至待初始化列表；否则立即初始化该模型。
    /// </summary>
    /// <typeparam name="TModel">要注册的模型类型</typeparam>
    /// <param name="model">要注册的模型实例</param>
    public void RegisterModel<TModel>(TModel model) where TModel : IModel
    {
        if (CurrentPhase >= ArchitecturePhase.Ready && !Configuration.Options.AllowLateRegistration)
        {
            var errorMsg = "Cannot register model after Architecture is Ready";
            _logger.Error(errorMsg);
            throw new InvalidOperationException(errorMsg);
        }

        _logger.Debug($"Registering model: {typeof(TModel).Name}");
        // 对于有 SetArchitecture 方法的模型，尝试调用该方法
        var setArchitectureMethod = typeof(TModel).GetMethod("SetArchitecture", [typeof(IArchitecture)]);
        setArchitectureMethod?.Invoke(model, [this]);
        Container.RegisterPlurality(model);

        if (!_mInited)
            _mModels.Add(model);
        else
        {
            _logger.Debug($"Immediately initializing model: {typeof(TModel).Name}");
            model.Init();
        }

        _logger.Info($"Model registered: {typeof(TModel).Name}");
    }

    /// <summary>
    ///     注册一个工具到架构中。
    ///     工具不会被延迟初始化，直接放入IOC容器供后续使用。
    /// </summary>
    /// <typeparam name="TUtility">要注册的工具类型</typeparam>
    /// <param name="utility">要注册的工具实例</param>
    public void RegisterUtility<TUtility>(TUtility utility) where TUtility : IUtility
    {
        _logger.Debug($"Registering utility: {typeof(TUtility).Name}");
        Container.RegisterPlurality(utility);
        _logger.Info($"Utility registered: {typeof(TUtility).Name}");
    }

    #endregion

    #region IArchitectureLifecycle Implementation

    /// <summary>
    /// 处理架构阶段变更通知
    /// </summary>
    /// <param name="phase">当前架构阶段</param>
    /// <param name="architecture">架构实例</param>
    public virtual void OnPhase(ArchitecturePhase phase, IArchitecture architecture)
    {
        
    }

    #endregion
}