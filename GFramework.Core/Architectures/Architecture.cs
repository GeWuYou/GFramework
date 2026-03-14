using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Enums;
using GFramework.Core.Abstractions.Environment;
using GFramework.Core.Abstractions.Ioc;
using GFramework.Core.Abstractions.Lifecycle;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Abstractions.Model;
using GFramework.Core.Abstractions.Systems;
using GFramework.Core.Abstractions.Utility;
using GFramework.Core.Environment;
using GFramework.Core.Extensions;
using GFramework.Core.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace GFramework.Core.Architectures;

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
    ///     注册中介行为管道
    ///     用于配置Mediator框架的行为拦截和处理逻辑
    /// </summary>
    /// <typeparam name="TBehavior">行为类型，必须是引用类型</typeparam>
    public void RegisterMediatorBehavior<TBehavior>() where TBehavior : class
    {
        _logger.Debug($"Registering mediator behavior: {typeof(TBehavior).Name}");
        Container.RegisterMediatorBehavior<TBehavior>();
    }

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

    private IArchitectureServices Services { get; } = services ?? new ArchitectureServices();

    /// <summary>
    ///     获取依赖注入容器
    /// </summary>
    private IIocContainer Container => Services.Container;

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

    private readonly TaskCompletionSource _readyTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    ///     获取一个布尔值，指示当前架构是否处于就绪状态。
    ///     当前架构的阶段等于 ArchitecturePhase.Ready 时返回 true，否则返回 false。
    /// </summary>
    public bool IsReady => CurrentPhase == ArchitecturePhase.Ready;

    /// <summary>
    ///     待初始化组件的去重集合。
    ///     用于存储需要初始化的组件实例，确保每个组件仅被初始化一次。
    /// </summary>
    private readonly HashSet<IInitializable> _pendingInitializableSet = [];

    /// <summary>
    ///     存储所有待初始化的组件（统一管理，保持注册顺序）
    /// </summary>
    private readonly List<IInitializable> _pendingInitializableList = [];

    /// <summary>
    ///     可销毁组件的去重集合（支持 IDestroyable 和 IAsyncDestroyable）
    /// </summary>
    private readonly HashSet<object> _disposableSet = [];

    /// <summary>
    ///     存储所有需要销毁的组件（统一管理，保持注册逆序销毁）
    /// </summary>
    private readonly List<object> _disposables = [];

    /// <summary>
    ///     生命周期感知对象列表
    /// </summary>
    private readonly List<IArchitectureLifecycleHook> _lifecycleHooks = [];

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
        foreach (var obj in Container.GetAll<IArchitecturePhaseListener>())
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
    public IArchitectureLifecycleHook RegisterLifecycleHook(IArchitectureLifecycleHook hook)
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

        // 处理销毁（支持 IDestroyable 或 IAsyncDestroyable）
        if (component is not (IDestroyable or IAsyncDestroyable)) return;
        // 原子去重：HashSet.Add 返回 true 表示添加成功（之前不存在）
        if (!_disposableSet.Add(component)) return;
        _disposables.Add(component);
        _logger.Trace($"Registered {component.GetType().Name} for destruction");
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
            await asyncInit.InitializeAsync();
        else
            component.Initialize();
    }

    /// <summary>
    ///     抽象初始化方法，由子类重写以进行自定义初始化操作
    /// </summary>
    protected abstract void OnInitialize();

    /// <summary>
    ///     异步销毁架构及所有组件
    /// </summary>
    public virtual async ValueTask DestroyAsync()
    {
        // 检查当前阶段，如果已经处于销毁或已销毁状态则直接返回
        if (CurrentPhase >= ArchitecturePhase.Destroying)
        {
            _logger.Warn("Architecture destroy called but already in destroying/destroyed state");
            return;
        }

        // 进入销毁阶段
        _logger.Info("Starting architecture destruction");
        EnterPhase(ArchitecturePhase.Destroying);

        // 销毁所有实现了 IAsyncDestroyable 或 IDestroyable 的组件（按注册逆序销毁）
        _logger.Info($"Destroying {_disposables.Count} disposable components");

        for (var i = _disposables.Count - 1; i >= 0; i--)
        {
            var component = _disposables[i];
            try
            {
                _logger.Debug($"Destroying component: {component.GetType().Name}");

                // 优先使用异步销毁
                if (component is IAsyncDestroyable asyncDestroyable)
                {
                    await asyncDestroyable.DestroyAsync();
                }
                else if (component is IDestroyable destroyable)
                {
                    destroyable.Destroy();
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error destroying {component.GetType().Name}", ex);
                // 继续销毁其他组件，不会因为一个组件失败而中断
            }
        }

        _disposables.Clear();
        _disposableSet.Clear();

        // 销毁服务模块
        await Services.ModuleManager.DestroyAllAsync();

        Container.Clear();

        // 进入已销毁阶段
        EnterPhase(ArchitecturePhase.Destroyed);
        _logger.Info("Architecture destruction completed");
    }

    /// <summary>
    ///     销毁架构并清理所有组件资源（同步方法，保留用于向后兼容）
    /// </summary>
    [Obsolete("建议使用 DestroyAsync() 以支持异步清理")]
    public virtual void Destroy()
    {
        DestroyAsync().AsTask().GetAwaiter().GetResult();
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
    /// 注册系统类型，由 DI 容器自动创建实例
    /// </summary>
    /// <typeparam name="T">系统类型</typeparam>
    /// <param name="onCreated">可选的实例创建后回调，用于自定义配置</param>
    public void RegisterSystem<T>(Action<T>? onCreated = null) where T : class, ISystem
    {
        ValidateRegistration("system");
        _logger.Debug($"Registering system type: {typeof(T).Name}");

        Container.RegisterFactory<T>(sp =>
        {
            // 1. DI 创建实例
            var system = ActivatorUtilities.CreateInstance<T>(sp);

            // 2. 框架默认处理
            system.SetContext(Context);
            RegisterLifecycleComponent(system);

            // 3. 用户自定义处理（钩子）
            onCreated?.Invoke(system);

            _logger.Debug($"System created: {typeof(T).Name}");
            return system;
        });

        _logger.Info($"System type registered: {typeof(T).Name}");
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
    /// 注册模型类型，由 DI 容器自动创建实例
    /// </summary>
    /// <typeparam name="T">模型类型</typeparam>
    /// <param name="onCreated">可选的实例创建后回调，用于自定义配置</param>
    public void RegisterModel<T>(Action<T>? onCreated = null) where T : class, IModel
    {
        ValidateRegistration("model");
        _logger.Debug($"Registering model type: {typeof(T).Name}");

        Container.RegisterFactory<T>(sp =>
        {
            var model = ActivatorUtilities.CreateInstance<T>(sp);
            model.SetContext(Context);
            RegisterLifecycleComponent(model);

            // 用户自定义钩子
            onCreated?.Invoke(model);

            _logger.Debug($"Model created: {typeof(T).Name}");
            return model;
        });

        _logger.Info($"Model type registered: {typeof(T).Name}");
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

    /// <summary>
    /// 注册工具类型，由 DI 容器自动创建实例
    /// </summary>
    /// <typeparam name="T">工具类型</typeparam>
    /// <param name="onCreated">可选的实例创建后回调，用于自定义配置</param>
    public void RegisterUtility<T>(Action<T>? onCreated = null) where T : class, IUtility
    {
        _logger.Debug($"Registering utility type: {typeof(T).Name}");

        Container.RegisterFactory<T>(sp =>
        {
            var utility = ActivatorUtilities.CreateInstance<T>(sp);

            // 如果是 IContextUtility，设置上下文
            if (utility is IContextUtility contextUtility)
            {
                contextUtility.SetContext(Context);
                RegisterLifecycleComponent(contextUtility);
            }

            // 用户自定义钩子
            onCreated?.Invoke(utility);

            _logger.Debug($"Utility created: {typeof(T).Name}");
            return utility;
        });

        _logger.Info($"Utility type registered: {typeof(T).Name}");
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

        // 注册内置服务模块
        Services.ModuleManager.RegisterBuiltInModules(Container);

        // 将 Environment 注册到容器（如果尚未注册）
        if (!Container.Contains<IEnvironment>())
            Container.RegisterPlurality(Environment);

        // 初始化架构上下文（如果尚未初始化）
        _context ??= new ArchitectureContext(Container);
        GameContext.Bind(GetType(), _context);

        // 为服务设置上下文
        Services.SetContext(_context);
        if (Configurator is null)
        {
            _logger.Debug("Mediator-based cqrs will not take effect without the service setter configured!");
        }

        // 执行服务钩子
        Container.ExecuteServicesHook(Configurator);

        // 初始化服务模块
        await Services.ModuleManager.InitializeAllAsync(asyncMode);

        // === 用户 OnInitialize ===
        _logger.Debug("Calling user OnInitialize()");
        OnInitialize();
        _logger.Debug("User OnInitialize() completed");

        // === 组件初始化阶段 ===
        await InitializeAllComponentsAsync(asyncMode);

        // === 初始化完成阶段 ===
        Container.Freeze();
        _logger.Info("IOC container frozen");

        _mInitialized = true;
        EnterPhase(ArchitecturePhase.Ready);
        // 🔥 释放 Ready await
        _readyTcs.TrySetResult();

        _logger.Info($"Architecture {GetType().Name} is ready - all components initialized");
    }

    /// <summary>
    ///     等待架构初始化完成（Ready 阶段）
    ///     如果架构已经处于就绪状态，则立即返回已完成的任务；
    ///     否则返回一个任务，该任务将在架构进入就绪状态时完成。
    /// </summary>
    /// <returns>表示等待操作的Task对象</returns>
    public Task WaitUntilReadyAsync()
    {
        return IsReady ? Task.CompletedTask : _readyTcs.Task;
    }

    /// <summary>
    ///     获取用于配置服务集合的委托
    ///     默认实现返回null，子类可以重写此属性以提供自定义配置逻辑
    /// </summary>
    /// <value>
    ///     一个可为空的Action委托，用于配置IServiceCollection实例
    /// </value>
    public virtual Action<IServiceCollection>? Configurator => null;

    #endregion
}