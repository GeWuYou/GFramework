using GFramework.Core.Abstractions.architecture;
using GFramework.Core.Abstractions.command;
using GFramework.Core.Abstractions.enums;
using GFramework.Core.Abstractions.environment;
using GFramework.Core.Abstractions.events;
using GFramework.Core.Abstractions.ioc;
using GFramework.Core.Abstractions.logging;
using GFramework.Core.Abstractions.model;
using GFramework.Core.Abstractions.query;
using GFramework.Core.Abstractions.system;
using GFramework.Core.Abstractions.utility;
using GFramework.Core.environment;
using GFramework.Core.events;
using GFramework.Core.logging;

namespace GFramework.Core.architecture;

/// <summary>
///     架构基类，提供系统、模型、工具等组件的注册与管理功能。
///     专注于生命周期管理、初始化流程控制和架构阶段转换。
///     不直接提供业务操作方法，业务操作通过 ArchitectureRuntime 提供。
/// </summary>
public abstract class Architecture(
    IArchitectureConfiguration? configuration = null,
    IEnvironment? environment = null,
    IArchitectureServices? services = null,
    IArchitectureContext? context = null
)
    : IArchitecture
{
    /// <summary>
    ///     获取架构配置对象
    /// </summary>
    /// <value>
    ///     返回一个IArchitectureConfiguration接口的实例，默认为DefaultArchitectureConfiguration类型
    /// </value>
    private IArchitectureConfiguration Configuration { get; } = configuration ?? new ArchitectureConfiguration();

    /// <summary>
    ///     获取环境配置对象
    /// </summary>
    /// <value>
    ///     返回一个IEnvironment接口的实例，默认为DefaultEnvironment类型
    /// </value>
    private IEnvironment Environment { get; } = environment ?? new DefaultEnvironment();


    /// <summary>
    ///     获取架构服务对象
    /// </summary>
    /// <value>
    ///     返回一个IArchitectureServices接口的实例，默认为DefaultArchitectureServices类型
    /// </value>
    private IArchitectureServices Services { get; } = services ?? new ArchitectureServices();

    /// <summary>
    ///     获取依赖注入容器
    /// </summary>
    /// <value>
    ///     通过Services属性获取的IArchitectureServices中的Container属性
    /// </value>
    private IIocContainer Container => Services.Container;

    /// <summary>
    ///     获取类型事件系统
    /// </summary>
    /// <value>
    ///     通过Services属性获取的IArchitectureServices中的TypeEventSystem属性
    /// </value>
    private ITypeEventSystem TypeEventSystem => Services.TypeEventSystem;

    private ICommandBus CommandBus => Services.CommandBus;

    private IQueryBus QueryBus => Services.QueryBus;

    #region Module Management

    /// <summary>
    ///     安装架构模块
    /// </summary>
    /// <param name="module">要安装的模块</param>
    public void InstallModule(IArchitectureModule module)
    {
        var name = module.GetType().Name;
        var logger =
            LoggerFactoryResolver.Provider.CreateLogger(name);
        logger.Debug($"Installing module: {name}");
        RegisterLifecycleHook(module);
        Container.RegisterPlurality(module);
        module.Install(this);
        logger.Info($"Module installed: {name}");
    }

    #endregion

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
    private bool _mInitialized;

    /// <summary>
    ///     生命周期感知对象列表
    /// </summary>
    private readonly List<IArchitectureLifecycle> _lifecycleHooks = [];

    /// <summary>
    ///     当前架构的阶段
    /// </summary>
    public ArchitecturePhase CurrentPhase { get; private set; }

    /// <summary>
    ///     日志记录器实例，用于记录应用程序的运行日志
    /// </summary>
    private ILogger _logger = null!;

    private IArchitectureContext? _context = context;

    public IArchitectureContext Context => _context!;

    #endregion

    #region Lifecycle Management

    /// <summary>
    ///     进入指定的架构阶段，并执行相应的生命周期管理操作
    /// </summary>
    /// <param name="next">要进入的下一个架构阶段</param>
    /// <exception cref="InvalidOperationException">当阶段转换不被允许时抛出异常</exception>
    protected virtual void EnterPhase(ArchitecturePhase next)
    {
        if (Configuration.ArchitectureProperties.StrictPhaseValidation &&
            (!ArchitectureConstants.PhaseTransitions.TryGetValue(CurrentPhase, out var allowed) ||
             !allowed.Contains(next)))
        {
            // 验证阶段转换是否合法
            var errorMsg = $"Invalid phase transition: {CurrentPhase} -> {next}";
            _logger.Fatal(errorMsg);
            throw new InvalidOperationException(errorMsg);
        }

        var previousPhase = CurrentPhase;
        CurrentPhase = next;

        if (previousPhase != next) _logger.Info($"Architecture phase changed: {previousPhase} -> {next}");

        NotifyPhase(next);

        // 通知所有架构阶段感知对象阶段变更
        foreach (var obj in Container.GetAll<IArchitecturePhaseAware>())
        {
            _logger.Trace($"Notifying phase-aware object {obj.GetType().Name} of phase change to {next}");
            obj.OnArchitecturePhase(next);
        }
    }

    /// <summary>
    ///     通知所有生命周期钩子当前阶段变更
    /// </summary>
    /// <param name="phase">当前架构阶段</param>
    private void NotifyPhase(ArchitecturePhase phase)
    {
        foreach (var hook in _lifecycleHooks)
        {
            hook.OnPhase(phase, this);
            _logger.Trace($"Notifying lifecycle hook {hook.GetType().Name} of phase {phase}");
        }
    }

    /// <summary>
    ///     注册生命周期钩子
    /// </summary>
    /// <param name="hook">生命周期钩子实例</param>
    public void RegisterLifecycleHook(IArchitectureLifecycle hook)
    {
        if (CurrentPhase >= ArchitecturePhase.Ready && !Configuration.ArchitectureProperties.AllowLateRegistration)
            throw new InvalidOperationException(
                "Cannot register lifecycle hook after architecture is Ready");
        _lifecycleHooks.Add(hook);
    }


    /// <summary>
    ///     抽象初始化方法，由子类重写以进行自定义初始化操作
    /// </summary>
    protected abstract void Init();

    /// <summary>
    ///     销毁架构并清理所有系统资源
    /// </summary>
    /// <remarks>
    ///     此函数负责有序地销毁架构中的所有系统组件，并发送相应的生命周期事件。
    ///     函数会确保只执行一次销毁操作，避免重复销毁。
    /// </remarks>
    public virtual void Destroy()
    {
        // 检查当前阶段，如果已经处于销毁或已销毁状态则直接返回
        if (CurrentPhase >= ArchitecturePhase.Destroying)
        {
            _logger.Warn("Architecture destroy called but already in destroying/destroyed state");
            return;
        }

        // 进入销毁阶段并发送销毁开始事件
        _logger.Info("Starting architecture destruction");
        EnterPhase(ArchitecturePhase.Destroying);
        TypeEventSystem.Send(new ArchitectureEvents.ArchitectureDestroyingEvent());

        // 销毁所有系统组件并清空系统列表
        _logger.Info($"Destroying {_allSystems.Count} systems");
        foreach (var system in _allSystems)
        {
            _logger.Debug($"Destroying system: {system.GetType().Name}");
            system.Destroy();
        }

        _allSystems.Clear();

        // 进入已销毁阶段并发送销毁完成事件
        EnterPhase(ArchitecturePhase.Destroyed);
        TypeEventSystem.Send(new ArchitectureEvents.ArchitectureDestroyedEvent());
        _logger.Info("Architecture destruction completed");
    }

    #endregion

    #region Component Registration

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
            EnterPhase(ArchitecturePhase.FailedInitialization);
            // 发送初始化失败事件
            TypeEventSystem.Send(new ArchitectureEvents.ArchitectureFailedInitializationEvent());
        }
    }

    /// <summary>
    ///     异步初始化方法，返回Task以便调用者可以等待初始化完成
    /// </summary>
    /// <returns>表示异步初始化操作的Task</returns>
    public async Task InitializeAsync()
    {
        try
        {
            await InitializeInternalAsync(true);
        }
        catch (Exception e)
        {
            _logger.Error("Architecture initialization failed:", e);
            EnterPhase(ArchitecturePhase.FailedInitialization);
            // 发送初始化失败事件
            TypeEventSystem.Send(new ArchitectureEvents.ArchitectureFailedInitializationEvent());
        }
    }

    /// <summary>
    ///     异步初始化组件
    /// </summary>
    /// <param name="component">要初始化的组件对象</param>
    /// <param name="asyncMode">是否启用异步模式</param>
    /// <returns>表示异步操作的任务</returns>
    private static async Task InitializeComponentAsync(object component, bool asyncMode)
    {
        // 根据组件类型和异步模式选择相应的初始化方法
        if (asyncMode && component is IAsyncInitializable asyncInit)
            await asyncInit.InitializeAsync();
        else
            switch (component)
            {
                case IModel model:
                    model.Init();
                    break;
                case ISystem system:
                    system.Init();
                    break;
            }
    }

    /// <summary>
    ///     异步初始化架构内部组件，包括上下文、模型和系统的初始化
    /// </summary>
    /// <param name="asyncMode">是否启用异步模式进行组件初始化</param>
    /// <returns>异步任务，表示初始化操作的完成</returns>
    private async Task InitializeInternalAsync(bool asyncMode)
    {
        // === 基础上下文 & Logger ===
        // 设置日志工厂提供程序
        LoggerFactoryResolver.Provider = Configuration.LoggerProperties.LoggerFactoryProvider;
        // 创建日志记录器实例
        _logger = LoggerFactoryResolver.Provider.CreateLogger(GetType().Name);
        Environment.Initialize();
        // 初始化架构上下文（如果尚未初始化）
        _context ??= new ArchitectureContext(Container, TypeEventSystem, CommandBus, QueryBus, Environment);
        // 将当前架构类型与上下文绑定到游戏上下文
        GameContext.Bind(GetType(), _context);

        // 为服务设置上下文
        Services.SetContext(_context);

        // === 用户 Init ===
        // 调用子类实现的初始化方法
        _logger.Debug("Calling user Init()");
        Init();
        _logger.Debug("User Init() completed");

        // === 模型初始化阶段 ===
        // 在此阶段初始化所有注册的模型组件
        EnterPhase(ArchitecturePhase.BeforeModelInit);
        _logger.Info($"Initializing {_mModels.Count} models");

        // 异步初始化所有已注册但尚未初始化的模型
        foreach (var model in _mModels)
        {
            _logger.Debug($"Initializing model: {model.GetType().Name}");
            await InitializeComponentAsync(model, asyncMode);
        }

        _mModels.Clear();
        EnterPhase(ArchitecturePhase.AfterModelInit);
        _logger.Info("All models initialized");

        // === 系统初始化阶段 ===
        // 在此阶段初始化所有注册的系统组件
        EnterPhase(ArchitecturePhase.BeforeSystemInit);
        _logger.Info($"Initializing {_mSystems.Count} systems");

        // 异步初始化所有已注册但尚未初始化的系统
        foreach (var system in _mSystems)
        {
            _logger.Debug($"Initializing system: {system.GetType().Name}");
            await InitializeComponentAsync(system, asyncMode);
        }

        _mSystems.Clear();
        EnterPhase(ArchitecturePhase.AfterSystemInit);
        _logger.Info("All systems initialized");

        // === 初始化完成阶段 ===
        // 冻结IOC容器并标记架构为就绪状态
        Container.Freeze();
        _logger.Info("IOC container frozen");

        _mInitialized = true;
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
        if (CurrentPhase >= ArchitecturePhase.Ready && !Configuration.ArchitectureProperties.AllowLateRegistration)
        {
            const string errorMsg = "Cannot register system after Architecture is Ready";
            _logger.Error(errorMsg);
            throw new InvalidOperationException(errorMsg);
        }

        _logger.Debug($"Registering system: {typeof(TSystem).Name}");
        system.SetContext(Context);
        Container.RegisterPlurality(system);
        _allSystems.Add(system);
        if (!_mInitialized)
        {
            _mSystems.Add(system);
        }
        else
        {
            _logger.Trace($"Immediately initializing system: {typeof(TSystem).Name}");
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
        if (CurrentPhase >= ArchitecturePhase.Ready && !Configuration.ArchitectureProperties.AllowLateRegistration)
        {
            var errorMsg = "Cannot register model after Architecture is Ready";
            _logger.Error(errorMsg);
            throw new InvalidOperationException(errorMsg);
        }

        _logger.Debug($"Registering model: {typeof(TModel).Name}");
        model.SetContext(Context);
        Container.RegisterPlurality(model);

        if (!_mInitialized)
        {
            _mModels.Add(model);
        }
        else
        {
            _logger.Trace($"Immediately initializing model: {typeof(TModel).Name}");
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
}