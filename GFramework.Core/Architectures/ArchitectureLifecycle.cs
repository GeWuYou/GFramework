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
        return _phaseCoordinator.RegisterLifecycleHook(hook);
    }

    #endregion

    #region Component Lifecycle Management

    /// <summary>
    ///     统一的组件生命周期注册逻辑
    /// </summary>
    /// <param name="component">要注册的组件</param>
    public void RegisterLifecycleComponent(object component)
    {
        if (component is IInitializable initializable)
        {
            if (_initialized)
                throw new InvalidOperationException("Cannot initialize component after Architecture is Ready");

            if (_pendingInitializableSet.Add(initializable))
            {
                _pendingInitializableList.Add(initializable);
                logger.Trace($"Added {component.GetType().Name} to pending initialization queue");
            }
        }

        _disposer.Register(component);
    }

    #endregion

    #region Phase Management

    /// <summary>
    ///     进入指定的架构阶段，并执行相应的生命周期管理操作
    /// </summary>
    /// <param name="next">要进入的下一个架构阶段</param>
    public void EnterPhase(ArchitecturePhase next)
    {
        _phaseCoordinator.EnterPhase(next);
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
    ///     架构阶段协调器
    /// </summary>
    private readonly ArchitecturePhaseCoordinator _phaseCoordinator =
        new(architecture, configuration, services, logger);

    /// <summary>
    ///     架构销毁协调器
    /// </summary>
    private readonly ArchitectureDisposer _disposer = new(services, logger);

    /// <summary>
    ///     标记架构是否已初始化完成
    /// </summary>
    private bool _initialized;

    #endregion

    #region Properties

    /// <summary>
    ///     当前架构的阶段
    /// </summary>
    public ArchitecturePhase CurrentPhase => _phaseCoordinator.CurrentPhase;

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
    public event Action<ArchitecturePhase>? PhaseChanged
    {
        add => _phaseCoordinator.PhaseChanged += value;
        remove => _phaseCoordinator.PhaseChanged -= value;
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
        await _disposer.DestroyAsync(CurrentPhase, EnterPhase);
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