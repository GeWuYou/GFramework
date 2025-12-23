using GFramework.Core.command;
using GFramework.Core.events;
using GFramework.Core.ioc;
using GFramework.Core.logging;
using GFramework.Core.model;
using GFramework.Core.query;
using GFramework.Core.system;
using GFramework.Core.utility;

namespace GFramework.Core.architecture;

/// <summary>
///     架构基类，提供系统、模型、工具等组件的注册与管理功能。
///     使用单例模式确保全局唯一实例，并支持命令、查询和事件机制。
/// </summary>
/// <typeparam name="T">派生类类型，用于实现单例</typeparam>
public abstract class Architecture<T> : IArchitecture
    where T : Architecture<T>, new()
{
    /// <summary>
    /// 获取架构选项的虚拟属性
    /// </summary>
    /// <returns>返回IArchitectureOptions接口实例，包含架构配置选项</returns>
    /// <remarks>
    /// 默认实现返回FunctionalArchitectureOptions实例，其中包含两个委托：
    /// 第一个委托始终返回true，第二个委托始终返回false
    /// </remarks>
    protected virtual IArchitectureOptions Options { get; } = new FunctionalArchitectureOptions(
        () => true,
        () => false
    );

    #region Fields and Properties

    /// <summary>
    ///     控制反转容器，用于存储和获取各种服务（如系统、模型、工具）
    /// </summary>
    private readonly IocContainer _mContainer = new();

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
    ///     类型化事件系统，负责事件的发布与订阅管理
    /// </summary>
    private readonly TypeEventSystem _mTypeEventSystem = new();

    /// <summary>
    ///     标记架构是否已初始化完成
    /// </summary>
    private bool _mInited;

    /// <summary>
    ///     获取架构实例的静态属性
    /// </summary>
    /// <returns>返回IArchitecture类型的架构实例</returns>
    public static IArchitecture Instance => MArchitectureLazy.Value;

    /// <summary>
    ///     生命周期感知对象列表
    /// </summary>
    private readonly List<IArchitectureLifecycle> _lifecycleHooks = [];

    /// <summary>
    ///     当前架构的阶段
    /// </summary>
    public ArchitecturePhase CurrentPhase { get; private set; }

    /// <summary>
    ///     静态只读字段，用于延迟初始化架构实例
    ///     使用Lazy确保线程安全的单例模式实现
    /// </summary>
    /// <remarks>
    ///     初始化过程包括：
    ///     1. 创建T类型的实例
    ///     2. 调用用户自定义的Init方法
    ///     3. 执行注册的补丁逻辑
    ///     4. 初始化所有已注册的模型和系统
    ///     5. 清理临时集合并标记初始化完成
    /// </remarks>
    /// <returns>T类型的架构实例</returns>
    private static readonly Lazy<T> MArchitectureLazy = new(() =>
    {
        var arch = new T();
        var logger = Log.CreateLogger("Architecture");
        
        // == Architecture Init  ==
        arch.EnterPhase(ArchitecturePhase.Created);
        logger.Info($"Architecture {typeof(T).Name} created");
        
        arch.EnterPhase(ArchitecturePhase.BeforeInit);
        logger.Info("Starting architecture initialization");
        
        // 调用用户实现的初始化
        arch.Init();
        arch.EnterPhase(ArchitecturePhase.AfterInit);
        logger.Info("Architecture initialization completed");


        // == Model Init ==
        arch.EnterPhase(ArchitecturePhase.BeforeModelInit);
        logger.Info($"Initializing {arch._mModels.Count} models");
        
        // 初始化所有已注册但尚未初始化的模型
        foreach (var model in arch._mModels)
        {
            logger.Debug($"Initializing model: {model.GetType().Name}");
            model.Init();
        }
        arch._mModels.Clear();
        arch.EnterPhase(ArchitecturePhase.AfterModelInit);
        logger.Info("All models initialized");


        // == System Init ==
        arch.EnterPhase(ArchitecturePhase.BeforeSystemInit);
        logger.Info($"Initializing {arch._mSystems.Count} systems");
        
        // 初始化所有已注册但尚未初始化的系统
        foreach (var system in arch._mSystems)
        {
            logger.Debug($"Initializing system: {system.GetType().Name}");
            system.Init();
        }
        arch._mSystems.Clear();
        arch.EnterPhase(ArchitecturePhase.AfterSystemInit);
        logger.Info("All systems initialized");


        // == Finalize ==
        // 冻结IOC容器，不允许 anymore
        arch._mContainer.Freeze();
        arch._mInited = true;
        arch.EnterPhase(ArchitecturePhase.Ready);
        // 发送架构生命周期就绪事件
        arch.SendEvent(new ArchitectureEvents.ArchitectureLifecycleReadyEvent());
        logger.Info($"Architecture {typeof(T).Name} is ready - all components initialized");
        return arch;
    }, LazyThreadSafetyMode.ExecutionAndPublication);

    #endregion

    #region Lifecycle Management

    /// <summary>
    /// 进入指定的架构阶段，并执行相应的生命周期管理操作
    /// </summary>
    /// <param name="next">要进入的下一个架构阶段</param>
    /// <exception cref="InvalidOperationException">当阶段转换不被允许时抛出异常</exception>
    private void EnterPhase(ArchitecturePhase next)
    {
        var logger = Log.CreateLogger("Architecture");
        
        if (Options.StrictPhaseValidation &&
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
        foreach (var obj in _mContainer.GetAll<IArchitecturePhaseAware>())
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
        if (CurrentPhase >= ArchitecturePhase.Ready && !Options.AllowLateRegistration)
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
        var logger = Log.CreateLogger("Architecture");
        
        // 检查当前阶段，如果已经处于销毁或已销毁状态则直接返回
        if (CurrentPhase >= ArchitecturePhase.Destroying)
        {
            logger.Warn("Architecture destroy called but already in destroying/destroyed state");
            return;
        }

        // 进入销毁阶段并发送销毁开始事件
        logger.Info("Starting architecture destruction");
        EnterPhase(ArchitecturePhase.Destroying);
        SendEvent(new ArchitectureEvents.ArchitectureDestroyingEvent());

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
        SendEvent(new ArchitectureEvents.ArchitectureDestroyedEvent());
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
        var logger = Log.CreateLogger("Architecture");
        logger.Debug($"Installing module: {module.GetType().Name}");
        RegisterLifecycleHook(module);
        _mContainer.RegisterPlurality(module);
        module.Install(this);
        logger.Info($"Module installed: {module.GetType().Name}");
    }

    #endregion

    #region Component Registration

    /// <summary>
    ///     注册一个系统到架构中。
    ///     若当前未初始化，则暂存至待初始化列表；否则立即初始化该系统。
    /// </summary>
    /// <typeparam name="TSystem">要注册的系统类型</typeparam>
    /// <param name="system">要注册的系统实例</param>
    public void RegisterSystem<TSystem>(TSystem system) where TSystem : ISystem
    {
        var logger = Log.CreateLogger("Architecture");
        
        if (CurrentPhase >= ArchitecturePhase.Ready && !Options.AllowLateRegistration)
        {
            var errorMsg = "Cannot register system after Architecture is Ready";
            logger.Error(errorMsg);
            throw new InvalidOperationException(errorMsg);
        }
        
        logger.Debug($"Registering system: {typeof(TSystem).Name}");
        system.SetArchitecture(this);
        _mContainer.RegisterPlurality(system);
        _allSystems.Add(system);
        if (!_mInited)
            _mSystems.Add(system);
        else
        {
            logger.Debug($"Immediately initializing system: {typeof(TSystem).Name}");
            system.Init();
        }
        
        logger.Info($"System registered: {typeof(TSystem).Name}");
    }

    /// <summary>
    ///     注册一个模型到架构中。
    ///     若当前未初始化，则暂存至待初始化列表；否则立即初始化该模型。
    /// </summary>
    /// <typeparam name="TModel">要注册的模型类型</typeparam>
    /// <param name="model">要注册的模型实例</param>
    public void RegisterModel<TModel>(TModel model) where TModel : IModel
    {
        var logger = Log.CreateLogger("Architecture");
        
        if (CurrentPhase >= ArchitecturePhase.Ready && !Options.AllowLateRegistration)
        {
            var errorMsg = "Cannot register model after Architecture is Ready";
            logger.Error(errorMsg);
            throw new InvalidOperationException(errorMsg);
        }
        
        logger.Debug($"Registering model: {typeof(TModel).Name}");
        model.SetArchitecture(this);
        _mContainer.RegisterPlurality(model);

        if (!_mInited)
            _mModels.Add(model);
        else
        {
            logger.Debug($"Immediately initializing model: {typeof(TModel).Name}");
            model.Init();
        }
        
        logger.Info($"Model registered: {typeof(TModel).Name}");
    }

    /// <summary>
    ///     注册一个工具到架构中。
    ///     工具不会被延迟初始化，直接放入IOC容器供后续使用。
    /// </summary>
    /// <typeparam name="TUtility">要注册的工具类型</typeparam>
    /// <param name="utility">要注册的工具实例</param>
    public void RegisterUtility<TUtility>(TUtility utility) where TUtility : IUtility
    {
        var logger = Log.CreateLogger("Architecture");
        logger.Debug($"Registering utility: {typeof(TUtility).Name}");
        _mContainer.RegisterPlurality(utility);
        logger.Info($"Utility registered: {typeof(TUtility).Name}");
    }

    #endregion

    #region Component Retrieval

    /// <summary>
    ///     从IOC容器中获取指定类型的系统实例
    /// </summary>
    /// <typeparam name="TSystem">目标系统类型</typeparam>
    /// <returns>对应的系统实例</returns>
    public TSystem? GetSystem<TSystem>() where TSystem : class, ISystem
    {
        return _mContainer.Get<TSystem>();
    }

    /// <summary>
    ///     从IOC容器中获取指定类型的模型实例
    /// </summary>
    /// <typeparam name="TModel">目标模型类型</typeparam>
    /// <returns>对应的模型实例</returns>
    public TModel? GetModel<TModel>() where TModel : class, IModel
    {
        return _mContainer.Get<TModel>();
    }

    /// <summary>
    ///     从IOC容器中获取指定类型的工具实例
    /// </summary>
    /// <typeparam name="TUtility">目标工具类型</typeparam>
    /// <returns>对应的工具实例</returns>
    public TUtility? GetUtility<TUtility>() where TUtility : class, IUtility
    {
        return _mContainer.Get<TUtility>();
    }

    #endregion

    #region Command Execution

    /// <summary>
    ///     发送一个带返回结果的命令请求
    /// </summary>
    /// <typeparam name="TResult">命令执行后的返回值类型</typeparam>
    /// <param name="command">要发送的命令对象</param>
    /// <returns>命令执行的结果</returns>
    public TResult SendCommand<TResult>(ICommand<TResult> command)
    {
        return ExecuteCommand(command);
    }

    /// <summary>
    ///     发送一个无返回结果的命令请求
    /// </summary>
    /// <typeparam name="TCommand">命令的具体类型</typeparam>
    /// <param name="command">要发送的命令对象</param>
    public void SendCommand<TCommand>(TCommand command) where TCommand : ICommand
    {
        ExecuteCommand(command);
    }

    /// <summary>
    ///     执行一个带返回结果的命令
    /// </summary>
    /// <typeparam name="TResult">命令执行后的返回值类型</typeparam>
    /// <param name="command">要执行的命令对象</param>
    /// <returns>命令执行的结果</returns>
    protected virtual TResult ExecuteCommand<TResult>(ICommand<TResult> command)
    {
        command.SetArchitecture(this);
        return command.Execute();
    }

    /// <summary>
    ///     执行一个无返回结果的命令
    /// </summary>
    /// <param name="command">要执行的命令对象</param>
    protected virtual void ExecuteCommand(ICommand command)
    {
        command.SetArchitecture(this);
        command.Execute();
    }

    #endregion

    #region Query Execution

    /// <summary>
    ///     发起一次查询请求并获得其结果
    /// </summary>
    /// <typeparam name="TResult">查询结果的数据类型</typeparam>
    /// <param name="query">要发起的查询对象</param>
    /// <returns>查询得到的结果数据</returns>
    public TResult SendQuery<TResult>(IQuery<TResult> query)
    {
        return DoQuery(query);
    }

    /// <summary>
    ///     实际执行查询逻辑的方法
    /// </summary>
    /// <typeparam name="TResult">查询结果的数据类型</typeparam>
    /// <param name="query">要处理的查询对象</param>
    /// <returns>查询结果</returns>
    protected virtual TResult DoQuery<TResult>(IQuery<TResult> query)
    {
        query.SetArchitecture(this);
        return query.Do();
    }

    #endregion

    #region Event Management

    /// <summary>
    ///     发布一个默认构造的新事件对象
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    public void SendEvent<TEvent>() where TEvent : new()
    {
        _mTypeEventSystem.Send<TEvent>();
    }

    /// <summary>
    ///     发布一个具体的事件对象
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="e">要发布的事件实例</param>
    public void SendEvent<TEvent>(TEvent e)
    {
        _mTypeEventSystem.Send(e);
    }

    /// <summary>
    ///     订阅某个特定类型的事件
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="onEvent">当事件发生时触发的动作</param>
    /// <returns>可用于取消订阅的对象</returns>
    public IUnRegister RegisterEvent<TEvent>(Action<TEvent> onEvent)
    {
        return _mTypeEventSystem.Register(onEvent);
    }

    /// <summary>
    ///     取消对某类型事件的监听
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="onEvent">之前绑定的事件处理器</param>
    public void UnRegisterEvent<TEvent>(Action<TEvent> onEvent)
    {
        _mTypeEventSystem.UnRegister(onEvent);
    }

    #endregion
}