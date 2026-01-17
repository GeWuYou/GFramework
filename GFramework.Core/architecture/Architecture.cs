using GFramework.Core.Abstractions.architecture;
using GFramework.Core.Abstractions.command;
using GFramework.Core.Abstractions.enums;
using GFramework.Core.Abstractions.environment;
using GFramework.Core.Abstractions.events;
using GFramework.Core.Abstractions.ioc;
using GFramework.Core.Abstractions.lifecycle;
using GFramework.Core.Abstractions.logging;
using GFramework.Core.Abstractions.model;
using GFramework.Core.Abstractions.query;
using GFramework.Core.Abstractions.system;
using GFramework.Core.Abstractions.utility;
using GFramework.Core.environment;
using GFramework.Core.events;
using GFramework.Core.extensions;
using GFramework.Core.logging;
using IDisposable = GFramework.Core.Abstractions.lifecycle.IDisposable;

namespace GFramework.Core.architecture;

/// <summary>
///     架构基类，提供系统、模型、工具等组件的注册与管理功能。
///     专注于生命周期管理、初始化流程控制和架构阶段转换。
/// </summary>
public abstract class Architecture(
    IArchitectureConfiguration? configuration = null,
    IEnvironment? environment = null,
    IArchitectureServices? services = null,
    IArchitectureContext? context = null
)
    : IArchitecture
{
    #region Module Management

    /// <summary>
    ///     安装架构模块
    /// </summary>
    /// <param name="module">要安装的模块</param>
    /// <returns>安装的模块实例</returns>
    public IArchitectureModule InstallModule(IArchitectureModule module)
    {
        var name = module.GetType().Name;
        var logger = LoggerFactoryResolver.Provider.CreateLogger(name);
        logger.Debug($"Installing module: {name}");
        RegisterLifecycleHook(module);
        Container.RegisterPlurality(module);
        module.Install(this);
        logger.Info($"Module installed: {name}");
        return module;
    }

    #endregion

    #region Properties

    /// <summary>
    ///     获取架构配置对象
    /// </summary>
    private IArchitectureConfiguration Configuration { get; } = configuration ?? new ArchitectureConfiguration();

    /// <summary>
    ///     获取环境配置对象
    /// </summary>
    private IEnvironment Environment { get; } = environment ?? new DefaultEnvironment();

    /// <summary>
    ///     获取架构服务对象
    /// </summary>
    private IArchitectureServices Services { get; } = services ?? new ArchitectureServices();

    /// <summary>
    ///     获取依赖注入容器
    /// </summary>
    private IIocContainer Container => Services.Container;

    /// <summary>
    ///     获取事件总线
    /// </summary>
    private IEventBus EventBus => Services.EventBus;

    /// <summary>
    ///     获取命令总线
    /// </summary>
    private ICommandBus CommandBus => Services.CommandBus;

    /// <summary>
    ///     获取查询总线
    /// </summary>
    private IQueryBus QueryBus => Services.QueryBus;

    /// <summary>
    ///     当前架构的阶段
    /// </summary>
    public ArchitecturePhase CurrentPhase { get; private set; }

    /// <summary>
    ///     架构上下文
    /// </summary>
    public IArchitectureContext Context => _context!;

    #endregion

    #region Fields

    /// <summary>
    ///     待初始化组件的去重集合
    /// </summary>
    private readonly HashSet<IInitializable> _pendingInitializableSet = [];

    /// <summary>
    ///     存储所有待初始化的组件（统一管理，保持注册顺序）
    /// </summary>
    private readonly List<IInitializable> _pendingInitializableList = [];

    /// <summary>
    ///     可销毁组件的去重集合
    /// </summary>
    private readonly HashSet<IDisposable> _disposableSet = [];

    /// <summary>
    ///     存储所有需要销毁的组件（统一管理，保持注册逆序销毁）
    /// </summary>
    private readonly List<IDisposable> _disposables = [];

    /// <summary>
    ///     生命周期感知对象列表
    /// </summary>
    private readonly List<IArchitectureLifecycle> _lifecycleHooks = [];

    /// <summary>
    ///     标记架构是否已初始化完成
    /// </summary>
    private bool _mInitialized;

    /// <summary>
    ///     日志记录器实例
    /// </summary>
    private ILogger _logger = null!;

    /// <summary>
    ///     架构上下文实例
    /// </summary>
    private IArchitectureContext? _context = context;

    #endregion

    #region Lifecycle Management

    /// <summary>
    ///     进入指定的架构阶段，并执行相应的生命周期管理操作
    /// </summary>
    /// <param name="next">要进入的下一个架构阶段</param>
    /// <exception cref="InvalidOperationException">当阶段转换不被允许时抛出异常</exception>
    protected virtual void EnterPhase(ArchitecturePhase next)
    {
        // 验证阶段转换
        ValidatePhaseTransition(next);

        // 执行阶段转换
        var previousPhase = CurrentPhase;
        CurrentPhase = next;

        if (previousPhase != next)
            _logger.Info($"Architecture phase changed: {previousPhase} -> {next}");

        // 通知阶段变更
        NotifyPhase(next);
        NotifyPhaseAwareObjects(next);
    }

    /// <summary>
    ///     验证阶段转换是否合法
    /// </summary>
    /// <param name="next">目标阶段</param>
    /// <exception cref="InvalidOperationException">当阶段转换不合法时抛出</exception>
    private void ValidatePhaseTransition(ArchitecturePhase next)
    {
        // 不需要严格验证，直接返回
        if (!Configuration.ArchitectureProperties.StrictPhaseValidation)
            return;

        // FailedInitialization 可以从任何阶段转换，直接返回
        if (next == ArchitecturePhase.FailedInitialization)
            return;

        // 检查转换是否在允许列表中
        if (ArchitectureConstants.PhaseTransitions.TryGetValue(CurrentPhase, out var allowed) &&
            allowed.Contains(next))
            return;

        // 转换不合法，抛出异常
        var errorMsg = $"Invalid phase transition: {CurrentPhase} -> {next}";
        _logger.Fatal(errorMsg);
        throw new InvalidOperationException(errorMsg);
    }

    /// <summary>
    ///     通知所有架构阶段感知对象阶段变更
    /// </summary>
    /// <param name="phase">新阶段</param>
    private void NotifyPhaseAwareObjects(ArchitecturePhase phase)
    {
        foreach (var obj in Container.GetAll<IArchitecturePhaseAware>())
        {
            _logger.Trace($"Notifying phase-aware object {obj.GetType().Name} of phase change to {phase}");
            obj.OnArchitecturePhase(phase);
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
    /// <returns>注册的钩子实例</returns>
    public IArchitectureLifecycle RegisterLifecycleHook(IArchitectureLifecycle hook)
    {
        if (CurrentPhase >= ArchitecturePhase.Ready && !Configuration.ArchitectureProperties.AllowLateRegistration)
            throw new InvalidOperationException(
                "Cannot register lifecycle hook after architecture is Ready");
        _lifecycleHooks.Add(hook);
        return hook;
    }

    /// <summary>
    ///     统一的组件生命周期注册逻辑
    /// </summary>
    /// <param name="component">要注册的组件</param>
    private void RegisterLifecycleComponent<T>(T component)
    {
        // 处理初始化
        if (component is IInitializable initializable)
        {
            if (!_mInitialized)
            {
                // 原子去重：HashSet.Add 返回 true 表示添加成功（之前不存在）
                if (_pendingInitializableSet.Add(initializable))
                {
                    _pendingInitializableList.Add(initializable);
                    _logger.Trace($"Added {component.GetType().Name} to pending initialization queue");
                }
            }
            else
            {
                throw new InvalidOperationException(
                    "Cannot initialize component after Architecture is Ready");
            }
        }

        // 处理销毁
        if (component is not IDisposable disposable) return;
        // 原子去重：HashSet.Add 返回 true 表示添加成功（之前不存在）
        if (_disposableSet.Add(disposable))
        {
            _disposables.Add(disposable);
            _logger.Trace($"Registered {component.GetType().Name} for destruction");
        }
    }

    /// <summary>
    ///     初始化所有待初始化的组件
    /// </summary>
    /// <param name="asyncMode">是否使用异步模式</param>
    private async Task InitializeAllComponentsAsync(bool asyncMode)
    {
        _logger.Info($"Initializing {_pendingInitializableList.Count} components");

        // 按类型分组初始化（保持原有的阶段划分）
        var utilities = _pendingInitializableList.OfType<IContextUtility>().ToList();
        var models = _pendingInitializableList.OfType<IModel>().ToList();
        var systems = _pendingInitializableList.OfType<ISystem>().ToList();

        // 1. 工具初始化阶段（始终进入阶段，仅在有组件时执行初始化）
        EnterPhase(ArchitecturePhase.BeforeUtilityInit);

        if (utilities.Count != 0)
        {
            _logger.Info($"Initializing {utilities.Count} context utilities");

            foreach (var utility in utilities)
            {
                _logger.Debug($"Initializing utility: {utility.GetType().Name}");
                await InitializeComponentAsync(utility, asyncMode);
            }

            _logger.Info("All context utilities initialized");
        }

        EnterPhase(ArchitecturePhase.AfterUtilityInit);

        // 2. 模型初始化阶段（始终进入阶段，仅在有组件时执行初始化）
        EnterPhase(ArchitecturePhase.BeforeModelInit);

        if (models.Count != 0)
        {
            _logger.Info($"Initializing {models.Count} models");

            foreach (var model in models)
            {
                _logger.Debug($"Initializing model: {model.GetType().Name}");
                await InitializeComponentAsync(model, asyncMode);
            }

            _logger.Info("All models initialized");
        }

        EnterPhase(ArchitecturePhase.AfterModelInit);

        // 3. 系统初始化阶段（始终进入阶段，仅在有组件时执行初始化）
        EnterPhase(ArchitecturePhase.BeforeSystemInit);

        if (systems.Count != 0)
        {
            _logger.Info($"Initializing {systems.Count} systems");

            foreach (var system in systems)
            {
                _logger.Debug($"Initializing system: {system.GetType().Name}");
                await InitializeComponentAsync(system, asyncMode);
            }

            _logger.Info("All systems initialized");
        }

        EnterPhase(ArchitecturePhase.AfterSystemInit);

        _pendingInitializableList.Clear();
        _pendingInitializableSet.Clear();
        _logger.Info("All components initialized");
    }

    /// <summary>
    ///     异步初始化单个组件
    /// </summary>
    /// <param name="component">要初始化的组件</param>
    /// <param name="asyncMode">是否使用异步模式</param>
    private static async Task InitializeComponentAsync(IInitializable component, bool asyncMode)
    {
        if (asyncMode && component is IAsyncInitializable asyncInit)
        {
            await asyncInit.InitializeAsync();
        }
        else
        {
            component.Init();
        }
    }

    /// <summary>
    ///     抽象初始化方法，由子类重写以进行自定义初始化操作
    /// </summary>
    protected abstract void Init();

    /// <summary>
    ///     销毁架构并清理所有组件资源
    /// </summary>
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
        EventBus.Send(new ArchitectureEvents.ArchitectureDestroyingEvent());

        // 销毁所有实现了 IDisposable 的组件（按注册逆序销毁）
        _logger.Info($"Destroying {_disposables.Count} disposable components");

        for (var i = _disposables.Count - 1; i >= 0; i--)
        {
            var disposable = _disposables[i];
            try
            {
                _logger.Debug($"Destroying component: {disposable.GetType().Name}");
                disposable.Destroy();
            }
            catch (Exception ex)
            {
                _logger.Error($"Error destroying {disposable.GetType().Name}", ex);
                // 继续销毁其他组件，不会因为一个组件失败而中断
            }
        }

        _disposables.Clear();
        _disposableSet.Clear();

        // 进入已销毁阶段并发送销毁完成事件
        EnterPhase(ArchitecturePhase.Destroyed);
        EventBus.Send(new ArchitectureEvents.ArchitectureDestroyedEvent());
        _logger.Info("Architecture destruction completed");
    }

    #endregion

    #region Component Registration

    /// <summary>
    ///     验证是否允许注册组件
    /// </summary>
    /// <param name="componentType">组件类型描述</param>
    /// <exception cref="InvalidOperationException">当不允许注册时抛出</exception>
    private void ValidateRegistration(string componentType)
    {
        if (CurrentPhase < ArchitecturePhase.Ready ||
            Configuration.ArchitectureProperties.AllowLateRegistration) return;
        var errorMsg = $"Cannot register {componentType} after Architecture is Ready";
        _logger.Error(errorMsg);
        throw new InvalidOperationException(errorMsg);
    }

    /// <summary>
    ///     注册一个系统到架构中。
    ///     若当前未初始化，则暂存至待初始化列表；否则立即初始化该系统。
    /// </summary>
    /// <typeparam name="TSystem">要注册的系统类型，必须实现ISystem接口</typeparam>
    /// <param name="system">要注册的系统实例</param>
    /// <returns>注册成功的系统实例</returns>
    public TSystem RegisterSystem<TSystem>(TSystem system) where TSystem : ISystem
    {
        ValidateRegistration("system");

        _logger.Debug($"Registering system: {typeof(TSystem).Name}");

        system.SetContext(Context);
        Container.RegisterPlurality(system);

        // 处理生命周期
        RegisterLifecycleComponent(system);

        _logger.Info($"System registered: {typeof(TSystem).Name}");
        return system;
    }

    /// <summary>
    ///     注册一个模型到架构中。
    ///     若当前未初始化，则暂存至待初始化列表；否则立即初始化该模型。
    /// </summary>
    /// <typeparam name="TModel">要注册的模型类型，必须实现IModel接口</typeparam>
    /// <param name="model">要注册的模型实例</param>
    /// <returns>注册成功的模型实例</returns>
    public TModel RegisterModel<TModel>(TModel model) where TModel : IModel
    {
        ValidateRegistration("model");

        _logger.Debug($"Registering model: {typeof(TModel).Name}");

        model.SetContext(Context);
        Container.RegisterPlurality(model);

        // 处理生命周期
        RegisterLifecycleComponent(model);

        _logger.Info($"Model registered: {typeof(TModel).Name}");
        return model;
    }

    /// <summary>
    ///     注册一个工具到架构中
    /// </summary>
    /// <typeparam name="TUtility">要注册的工具类型，必须实现IUtility接口</typeparam>
    /// <param name="utility">要注册的工具实例</param>
    /// <returns>注册成功的工具实例</returns>
    public TUtility RegisterUtility<TUtility>(TUtility utility) where TUtility : IUtility
    {
        _logger.Debug($"Registering utility: {typeof(TUtility).Name}");

        // 处理上下文工具类型的设置和生命周期管理
        utility.IfType<IContextUtility>(contextUtility =>
        {
            contextUtility.SetContext(Context);
            // 处理生命周期
            RegisterLifecycleComponent(contextUtility);
        });

        Container.RegisterPlurality(utility);
        _logger.Info($"Utility registered: {typeof(TUtility).Name}");
        return utility;
    }

    #endregion

    #region Initialization

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
            EventBus.Send(new ArchitectureEvents.ArchitectureFailedInitializationEvent());
            throw;
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
            EventBus.Send(new ArchitectureEvents.ArchitectureFailedInitializationEvent());
            throw;
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
        LoggerFactoryResolver.Provider = Configuration.LoggerProperties.LoggerFactoryProvider;
        _logger = LoggerFactoryResolver.Provider.CreateLogger(GetType().Name);
        Environment.Initialize();

        // 初始化架构上下文（如果尚未初始化）
        _context ??= new ArchitectureContext(Container, EventBus, CommandBus, QueryBus, Environment);
        GameContext.Bind(GetType(), _context);

        // 为服务设置上下文
        Services.SetContext(_context);

        // === 用户 Init ===
        _logger.Debug("Calling user Init()");
        Init();
        _logger.Debug("User Init() completed");

        // === 组件初始化阶段 ===
        await InitializeAllComponentsAsync(asyncMode);

        // === 初始化完成阶段 ===
        Container.Freeze();
        _logger.Info("IOC container frozen");

        _mInitialized = true;
        EnterPhase(ArchitecturePhase.Ready);
        EventBus.Send(new ArchitectureEvents.ArchitectureLifecycleReadyEvent());

        _logger.Info($"Architecture {GetType().Name} is ready - all components initialized");
    }

    #endregion
}