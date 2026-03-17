using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Enums;
using GFramework.Core.Abstractions.Lifecycle;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Abstractions.Model;
using GFramework.Core.Abstractions.Systems;
using GFramework.Core.Abstractions.Utility;

namespace GFramework.Core.Architectures;

/// <summary>
///     架构生命周期管理器
///     负责管理架构的阶段转换、组件初始化和销毁
/// </summary>
internal sealed class ArchitectureLifecycle(
    IArchitecture architecture,
    IArchitectureConfiguration configuration,
    IArchitectureServices services,
    ILogger logger)
{
    #region Lifecycle Hook Management

    /// <summary>
    ///     注册生命周期钩子
    /// </summary>
    /// <param name="hook">生命周期钩子实例</param>
    /// <returns>注册的钩子实例</returns>
    public IArchitectureLifecycleHook RegisterLifecycleHook(IArchitectureLifecycleHook hook)
    {
        if (CurrentPhase >= ArchitecturePhase.Ready && !configuration.ArchitectureProperties.AllowLateRegistration)
            throw new InvalidOperationException(
                "Cannot register lifecycle hook after architecture is Ready");
        _lifecycleHooks.Add(hook);
        return hook;
    }

    #endregion

    #region Component Lifecycle Management

    /// <summary>
    ///     统一的组件生命周期注册逻辑
    /// </summary>
    /// <param name="component">要注册的组件</param>
    public void RegisterLifecycleComponent(object component)
    {
        // 处理初始化
        if (component is IInitializable initializable)
        {
            if (!_initialized)
            {
                // 原子去重：HashSet.Add 返回 true 表示添加成功（之前不存在）
                if (_pendingInitializableSet.Add(initializable))
                {
                    _pendingInitializableList.Add(initializable);
                    logger.Trace($"Added {component.GetType().Name} to pending initialization queue");
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
        logger.Trace($"Registered {component.GetType().Name} for destruction");
    }

    #endregion

    #region Fields

    private readonly TaskCompletionSource _readyTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    ///     待初始化组件的去重集合
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
    private bool _initialized;

    #endregion

    #region Properties

    /// <summary>
    ///     当前架构的阶段
    /// </summary>
    public ArchitecturePhase CurrentPhase { get; private set; }

    /// <summary>
    ///     获取一个布尔值，指示当前架构是否处于就绪状态
    /// </summary>
    public bool IsReady => CurrentPhase == ArchitecturePhase.Ready;

    /// <summary>
    ///     获取一个布尔值，指示架构是否已初始化
    /// </summary>
    public bool IsInitialized => _initialized;

    /// <summary>
    ///     阶段变更事件（用于测试和扩展）
    /// </summary>
    public event Action<ArchitecturePhase>? PhaseChanged;

    #endregion

    #region Phase Management

    /// <summary>
    ///     进入指定的架构阶段，并执行相应的生命周期管理操作
    /// </summary>
    /// <param name="next">要进入的下一个架构阶段</param>
    /// <exception cref="InvalidOperationException">当阶段转换不被允许时抛出异常</exception>
    public void EnterPhase(ArchitecturePhase next)
    {
        // 验证阶段转换
        ValidatePhaseTransition(next);

        // 执行阶段转换
        var previousPhase = CurrentPhase;
        CurrentPhase = next;

        if (previousPhase != next)
            logger.Info($"Architecture phase changed: {previousPhase} -> {next}");

        // 通知阶段变更
        NotifyPhase(next);
        NotifyPhaseAwareObjects(next);

        // 触发阶段变更事件（用于测试和扩展）
        PhaseChanged?.Invoke(next);
    }

    /// <summary>
    ///     验证阶段转换是否合法
    /// </summary>
    /// <param name="next">目标阶段</param>
    /// <exception cref="InvalidOperationException">当阶段转换不合法时抛出</exception>
    private void ValidatePhaseTransition(ArchitecturePhase next)
    {
        // 不需要严格验证，直接返回
        if (!configuration.ArchitectureProperties.StrictPhaseValidation)
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
        logger.Fatal(errorMsg);
        throw new InvalidOperationException(errorMsg);
    }

    /// <summary>
    ///     通知所有架构阶段感知对象阶段变更
    /// </summary>
    /// <param name="phase">新阶段</param>
    private void NotifyPhaseAwareObjects(ArchitecturePhase phase)
    {
        foreach (var obj in services.Container.GetAll<IArchitecturePhaseListener>())
        {
            logger.Trace($"Notifying phase-aware object {obj.GetType().Name} of phase change to {phase}");
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
            hook.OnPhase(phase, architecture);
            logger.Trace($"Notifying lifecycle hook {hook.GetType().Name} of phase {phase}");
        }
    }

    #endregion

    #region Initialization

    /// <summary>
    ///     初始化所有待初始化的组件
    /// </summary>
    /// <param name="asyncMode">是否使用异步模式</param>
    public async Task InitializeAllComponentsAsync(bool asyncMode)
    {
        logger.Info($"Initializing {_pendingInitializableList.Count} components");

        // 按类型分组初始化（保持原有的阶段划分）
        var utilities = _pendingInitializableList.OfType<IContextUtility>().ToList();
        var models = _pendingInitializableList.OfType<IModel>().ToList();
        var systems = _pendingInitializableList.OfType<ISystem>().ToList();

        // 1. 工具初始化阶段
        EnterPhase(ArchitecturePhase.BeforeUtilityInit);

        if (utilities.Count != 0)
        {
            logger.Info($"Initializing {utilities.Count} context utilities");

            foreach (var utility in utilities)
            {
                logger.Debug($"Initializing utility: {utility.GetType().Name}");
                await InitializeComponentAsync(utility, asyncMode);
            }

            logger.Info("All context utilities initialized");
        }

        EnterPhase(ArchitecturePhase.AfterUtilityInit);

        // 2. 模型初始化阶段
        EnterPhase(ArchitecturePhase.BeforeModelInit);

        if (models.Count != 0)
        {
            logger.Info($"Initializing {models.Count} models");

            foreach (var model in models)
            {
                logger.Debug($"Initializing model: {model.GetType().Name}");
                await InitializeComponentAsync(model, asyncMode);
            }

            logger.Info("All models initialized");
        }

        EnterPhase(ArchitecturePhase.AfterModelInit);

        // 3. 系统初始化阶段
        EnterPhase(ArchitecturePhase.BeforeSystemInit);

        if (systems.Count != 0)
        {
            logger.Info($"Initializing {systems.Count} systems");

            foreach (var system in systems)
            {
                logger.Debug($"Initializing system: {system.GetType().Name}");
                await InitializeComponentAsync(system, asyncMode);
            }

            logger.Info("All systems initialized");
        }

        EnterPhase(ArchitecturePhase.AfterSystemInit);

        _pendingInitializableList.Clear();
        _pendingInitializableSet.Clear();
        _initialized = true;
        logger.Info("All components initialized");
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

    #endregion

    #region Destruction

    /// <summary>
    ///     异步销毁架构及所有组件
    /// </summary>
    public async ValueTask DestroyAsync()
    {
        // 检查当前阶段，如果已经处于销毁或已销毁状态则直接返回
        if (CurrentPhase >= ArchitecturePhase.Destroying)
        {
            logger.Warn("Architecture destroy called but already in destroying/destroyed state");
            return;
        }

        // 如果从未初始化（None 阶段），只清理已注册的组件，不进行阶段转换
        if (CurrentPhase == ArchitecturePhase.None)
        {
            logger.Debug("Architecture destroy called but never initialized, cleaning up registered components");
            await CleanupComponentsAsync();
            return;
        }

        // 进入销毁阶段
        logger.Info("Starting architecture destruction");
        EnterPhase(ArchitecturePhase.Destroying);

        // 清理所有组件
        await CleanupComponentsAsync();

        // 销毁服务模块
        await services.ModuleManager.DestroyAllAsync();

        services.Container.Clear();

        // 进入已销毁阶段
        EnterPhase(ArchitecturePhase.Destroyed);
        logger.Info("Architecture destruction completed");
    }

    /// <summary>
    ///     清理所有已注册的可销毁组件
    /// </summary>
    private async ValueTask CleanupComponentsAsync()
    {
        // 销毁所有实现了 IAsyncDestroyable 或 IDestroyable 的组件（按注册逆序销毁）
        logger.Info($"Destroying {_disposables.Count} disposable components");

        for (var i = _disposables.Count - 1; i >= 0; i--)
        {
            var component = _disposables[i];
            try
            {
                logger.Debug($"Destroying component: {component.GetType().Name}");

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
                logger.Error($"Error destroying {component.GetType().Name}", ex);
                // 继续销毁其他组件，不会因为一个组件失败而中断
            }
        }

        _disposables.Clear();
        _disposableSet.Clear();
    }

    /// <summary>
    ///     销毁架构并清理所有组件资源（同步方法，保留用于向后兼容）
    /// </summary>
    [Obsolete("建议使用 DestroyAsync() 以支持异步清理")]
    public void Destroy()
    {
        DestroyAsync().AsTask().GetAwaiter().GetResult();
    }

    #endregion

    #region Ready State

    /// <summary>
    ///     标记架构为就绪状态
    /// </summary>
    public void MarkAsReady()
    {
        EnterPhase(ArchitecturePhase.Ready);
        _readyTcs.TrySetResult();
    }

    /// <summary>
    ///     标记架构初始化失败
    /// </summary>
    /// <param name="exception">失败异常</param>
    public void MarkAsFailed(Exception exception)
    {
        EnterPhase(ArchitecturePhase.FailedInitialization);
        _readyTcs.TrySetException(exception);
    }

    /// <summary>
    ///     等待架构就绪
    /// </summary>
    public Task WaitUntilReadyAsync() => _readyTcs.Task;

    #endregion
}