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
            {
                if (!configuration.ArchitectureProperties.AllowLateRegistration)
                    throw new InvalidOperationException("Cannot initialize component after Architecture is Ready");

                InitializeLateRegisteredComponent(initializable);
            }

            else if (_pendingInitializableSet.Add(initializable))
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
        PhaseChanged?.Invoke(this, new ArchitecturePhaseChangedEventArgs(next));
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
    public event EventHandler<ArchitecturePhaseChangedEventArgs>? PhaseChanged;

    #endregion

    #region Initialization

    /// <summary>
    ///     初始化所有待初始化的组件
    /// </summary>
    /// <param name="asyncMode">是否使用异步模式</param>
    public async Task InitializeAllComponentsAsync(bool asyncMode)
    {
        logger.Info($"Initializing {_pendingInitializableList.Count} components");

        var initializationPlan = CreateInitializationPlan();

        await InitializePhaseComponentsAsync(
                initializationPlan.Utilities,
                ArchitecturePhase.BeforeUtilityInit,
                ArchitecturePhase.AfterUtilityInit,
                "context utilities",
                "utility",
                asyncMode)
            .ConfigureAwait(false);
        await InitializePhaseComponentsAsync(
                initializationPlan.Models,
                ArchitecturePhase.BeforeModelInit,
                ArchitecturePhase.AfterModelInit,
                "models",
                "model",
                asyncMode)
            .ConfigureAwait(false);
        await InitializePhaseComponentsAsync(
                initializationPlan.Systems,
                ArchitecturePhase.BeforeSystemInit,
                ArchitecturePhase.AfterSystemInit,
                "systems",
                "system",
                asyncMode)
            .ConfigureAwait(false);

        MarkInitializationCompleted();
    }

    /// <summary>
    ///     异步初始化单个组件
    /// </summary>
    /// <param name="component">要初始化的组件</param>
    /// <param name="asyncMode">是否使用异步模式</param>
    private static async Task InitializeComponentAsync(IInitializable component, bool asyncMode)
    {
        if (asyncMode && component is IAsyncInitializable asyncInit)
            await asyncInit.InitializeAsync().ConfigureAwait(false);
        else
            component.Initialize();
    }

    /// <summary>
    ///     按架构既有阶段语义把待初始化组件拆分为 utility、model 和 system 三个批次。
    ///     这样可以在压缩主流程复杂度的同时，继续复用注册顺序和接口类型决定的初始化分层。
    /// </summary>
    /// <returns>当前待初始化组件的阶段化批次。</returns>
    private InitializationPlan CreateInitializationPlan()
    {
        return new InitializationPlan(
            _pendingInitializableList.OfType<IContextUtility>().ToList(),
            _pendingInitializableList.OfType<IModel>().ToList(),
            _pendingInitializableList.OfType<ISystem>().ToList());
    }

    /// <summary>
    ///     执行单个生命周期阶段的批量初始化，并统一维护阶段切换、日志输出和异步初始化策略。
    /// </summary>
    /// <typeparam name="TComponent">当前阶段要初始化的组件类型。</typeparam>
    /// <param name="components">当前阶段的组件列表。</param>
    /// <param name="beforePhase">阶段开始前要进入的生命周期状态。</param>
    /// <param name="afterPhase">阶段结束后要进入的生命周期状态。</param>
    /// <param name="componentGroupName">用于批量日志的组件组名称。</param>
    /// <param name="componentLogName">用于单个组件日志的组件角色名称。</param>
    /// <param name="asyncMode">是否允许优先走异步初始化契约。</param>
    private async Task InitializePhaseComponentsAsync<TComponent>(
        IReadOnlyList<TComponent> components,
        ArchitecturePhase beforePhase,
        ArchitecturePhase afterPhase,
        string componentGroupName,
        string componentLogName,
        bool asyncMode)
        where TComponent : class, IInitializable
    {
        EnterPhase(beforePhase);

        if (components.Count != 0)
        {
            logger.Info($"Initializing {components.Count} {componentGroupName}");

            foreach (var component in components)
            {
                logger.Debug($"Initializing {componentLogName}: {component.GetType().Name}");
                await InitializeComponentAsync(component, asyncMode).ConfigureAwait(false);
            }

            logger.Info($"All {componentGroupName} initialized");
        }

        EnterPhase(afterPhase);
    }

    /// <summary>
    ///     在所有阶段初始化完成后清理挂起列表，并把生命周期状态切换到“已初始化”。
    /// </summary>
    private void MarkInitializationCompleted()
    {
        _pendingInitializableList.Clear();
        _pendingInitializableSet.Clear();
        _initialized = true;
        logger.Info("All components initialized");
    }

    /// <summary>
    ///     立即初始化在常规初始化批次完成后新增的组件。
    ///     当启用 <c>AllowLateRegistration</c> 时，生命周期层需要和注册层保持一致，
    ///     让新增组件在注册当下完成同步初始化，而不是停留在未初始化状态。
    /// </summary>
    /// <param name="component">后注册的可初始化组件。</param>
    private void InitializeLateRegisteredComponent(IInitializable component)
    {
        logger.Debug($"Initializing late-registered component: {component.GetType().Name}");
        component.Initialize();
    }

    #endregion

    #region Destruction

    /// <summary>
    ///     异步销毁架构及所有组件
    /// </summary>
    public async ValueTask DestroyAsync()
    {
        await _disposer.DestroyAsync(CurrentPhase, EnterPhase).ConfigureAwait(false);
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

    /// <summary>
    ///     保存一次完整初始化流程所需的三个阶段批次。
    /// </summary>
    /// <param name="Utilities">Utility 初始化批次。</param>
    /// <param name="Models">Model 初始化批次。</param>
    /// <param name="Systems">System 初始化批次。</param>
    private readonly record struct InitializationPlan(
        IReadOnlyList<IContextUtility> Utilities,
        IReadOnlyList<IModel> Models,
        IReadOnlyList<ISystem> Systems);

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
