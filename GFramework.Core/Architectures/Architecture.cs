// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

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
        var factory = resolvedConfiguration.LoggerProperties.LoggerFactoryProvider;
        var resolvedServices = services ?? new ArchitectureServices(factory);
        _services = resolvedServices;
        if (resolvedServices.Container is global::GFramework.Core.Ioc.MicrosoftDiContainer ownedContainer)
        {
            if (ownedContainer.ContextAccess != null)
                throw new InvalidOperationException("Each Architecture must own a distinct service container.");
            ownedContainer.ContextAccess = _access;
        }
        _context = context ?? new ArchitectureContext(resolvedServices.Container);

        // 日志工厂属于当前架构，不覆盖进程级配置。
        _logger = factory.CreateLogger(GetType().Name);

        // 初始化管理器
        _bootstrapper = new ArchitectureBootstrapper(resolvedEnvironment, resolvedServices, factory);
        _lifecycle = new ArchitectureLifecycle(this, resolvedConfiguration, resolvedServices, _logger);
        _lifecycle.PhaseChanged += HandleLifecyclePhaseChanged;
        _componentRegistry = new ArchitectureComponentRegistry(
            this,
            resolvedServices,
            _lifecycle);
        _modules = new ArchitectureModules(this, _logger);
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
        EnsureConfigurationOpen();
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
    public IArchitectureContext Context => _context;

    /// <summary>
    ///     获取一个布尔值，指示当前架构是否处于就绪状态
    /// </summary>
    public bool IsReady => !_access.IsStopping && _lifecycle.IsReady;

    /// <summary>
    ///     获取用于配置服务集合的委托
    ///     默认实现返回null，子类可以重写此属性以提供自定义配置逻辑
    /// </summary>
    public virtual Action<IServiceCollection>? Configurator => null;

    /// <summary>
    ///     在架构生命周期阶段发生变化时触发。
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         订阅者应通过 <see cref="ArchitecturePhaseChangedEventArgs.Phase" /> 读取当前阶段，而不是依赖内部生命周期对象。
    ///     </para>
    ///     <para>
    ///         事件委托中的 <c>sender</c> 始终为当前 <see cref="Architecture" /> 实例，便于测试与外部扩展保持稳定的发布者契约。
    ///     </para>
    /// </remarks>
    public event EventHandler<ArchitecturePhaseChangedEventArgs>? PhaseChanged;

    #endregion

    #region Fields

    /// <summary>
    ///     日志记录器实例
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    ///     架构上下文实例
    /// </summary>
    private readonly IArchitectureContext _context;

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

    /// <summary>当前架构拥有的正式服务集合和容器。</summary>
    private readonly IArchitectureServices _services;

    /// <summary>实例级服务访问窗口，清理时不开放给并发外部调用。</summary>
    private readonly ArchitectureContextAccess _access = new();

    /// <summary>初始化与销毁任务的发布锁；用户回调不在锁内执行。</summary>
#if NET9_0_OR_GREATER
    private readonly System.Threading.Lock _operationLock = new();
#else
    private readonly object _operationLock = new();
#endif

    /// <summary>启动和停止各共享一个任务，所有并发调用观察同一次结果。</summary>
    private Task? _initializationTask;

    /// <summary>当前停止任务。</summary>
    private Task? _destructionTask;

    /// <summary>宿主分离与启动失败共享同一次停止，避免重复节点/句柄清理。</summary>
    private Task? _hostStopTask;

    /// <summary>显式或销毁触发的启动取消。</summary>
    private CancellationTokenSource? _startupCancellation;

    /// <summary>仅 ConfigureServices 执行期间允许注册。</summary>
    private bool _configurationOpen;

    /// <summary>记录当前调用链正在启动，阻止从自身回调等待销毁而形成死锁。</summary>
    private readonly AsyncLocal<bool> _insideInitialization = new();

    /// <summary>阻止停止回调在同一调用链等待自身销毁任务。</summary>
    private readonly AsyncLocal<bool> _insideDestruction = new();

    #endregion

    #region Event Relays

    /// <summary>
    ///     把生命周期协作者的阶段广播重新映射到当前架构实例，
    ///     以便公开事件的 sender 始终反映真实的架构发布者。
    /// </summary>
    /// <param name="sender">生命周期协作者实例。</param>
    /// <param name="eventArgs">阶段变化事件数据。</param>
    private void HandleLifecyclePhaseChanged(object? sender, ArchitecturePhaseChangedEventArgs eventArgs)
    {
        PhaseChanged?.Invoke(this, eventArgs);
    }

    #endregion

    #region Module Management

    /// <summary>
    ///     安装架构模块
    /// </summary>
    /// <param name="module">要安装的模块</param>
    /// <returns>安装的模块实例</returns>
    public IArchitectureModule InstallModule(IArchitectureModule module)
    {
        EnsureConfigurationOpen();
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
        EnsureConfigurationOpen();
        return _componentRegistry.RegisterSystem(system);
    }

    /// <summary>
    ///     注册系统类型，由当前服务集合自动创建实例并接入本轮初始化
    /// </summary>
    /// <typeparam name="T">系统类型</typeparam>
    /// <param name="onCreated">可选的实例创建后回调</param>
    public void RegisterSystem<T>(Action<T>? onCreated = null) where T : class, ISystem
    {
        EnsureConfigurationOpen();
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
        EnsureConfigurationOpen();
        return _componentRegistry.RegisterModel(model);
    }

    /// <summary>
    ///     注册模型类型，由当前服务集合自动创建实例并接入本轮初始化
    /// </summary>
    /// <typeparam name="T">模型类型</typeparam>
    /// <param name="onCreated">可选的实例创建后回调</param>
    public void RegisterModel<T>(Action<T>? onCreated = null) where T : class, IModel
    {
        EnsureConfigurationOpen();
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
        EnsureConfigurationOpen();
        return _componentRegistry.RegisterUtility(utility);
    }

    /// <summary>
    ///     注册工具类型，由 DI 容器自动创建实例
    /// </summary>
    /// <typeparam name="T">工具类型</typeparam>
    /// <param name="onCreated">可选的实例创建后回调</param>
    public void RegisterUtility<T>(Action<T>? onCreated = null) where T : class, IUtility
    {
        EnsureConfigurationOpen();
        _componentRegistry.RegisterUtility(onCreated);
    }

    #endregion

    #region Initialization

    /// <summary>
    ///     配置当前架构的依赖与生命周期元数据。此阶段不能解析服务或执行初始化 IO，
    ///     所有组件由随后唯一构建的 provider 激活。
    /// </summary>
    /// <param name="services">当前架构将构建和拥有的服务集合。</param>
    protected virtual void ConfigureServices(IServiceCollection services)
    {
    }

    /// <summary>全部组件启动后、Ready 前的同步宿主接线，不再允许注册服务。</summary>
    protected virtual void OnStarted()
    {
    }

    /// <summary>等待 Ready 前的宿主接线；默认只调用同步宿主入口。</summary>
    /// <param name="cancellationToken">启动取消令牌。</param>
    /// <returns>宿主已准备完成。</returns>
    protected virtual ValueTask OnStartedAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        OnStarted();
        return ValueTask.CompletedTask;
    }

    /// <summary>停止宿主接线，执行时启动已结束且仍可在清理调用链中访问本实例服务。</summary>
    /// <returns>宿主资源已停止。</returns>
    protected virtual ValueTask OnStoppingAsync() => ValueTask.CompletedTask;

    /// <summary>同步启动只执行同步参与者，遇到异步组件或钩子明确要求异步入口。</summary>
    public void Initialize() => StartInitialization(false, CancellationToken.None).GetAwaiter().GetResult();

    /// <summary>配置、构建唯一 provider，并等待组件和宿主准备完成。</summary>
    /// <param name="cancellationToken">取消本轮启动并等待部分资源清理完成。</param>
    /// <returns>本轮启动完成。</returns>
    public Task InitializeAsync(CancellationToken cancellationToken = default) =>
        StartInitialization(true, cancellationToken);

    /// <summary>先发布启动任务，再执行用户回调，防止并发启动和销毁看见半发布状态。</summary>
    /// <param name="asyncMode">是否允许异步参与者。</param>
    /// <param name="cancellationToken">外部取消令牌。</param>
    /// <returns>唯一启动任务。</returns>
    private Task StartInitialization(bool asyncMode, CancellationToken cancellationToken)
    {
        TaskCompletionSource completion;
        CancellationToken token;
        lock (_operationLock)
        {
            if (_initializationTask != null || _destructionTask != null)
                throw new InvalidOperationException("Architecture can only be initialized once; create a new instance.");
            completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _initializationTask = completion.Task;
            _startupCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            token = _startupCancellation.Token;
        }
        _ = CompleteInitializationAsync(completion, asyncMode, token, cancellationToken);
        return completion.Task;
    }

    /// <summary>初始化失败和取消都先清理，再完成调用方与 Ready 等待者的任务。</summary>
    /// <param name="completion">已发布的本轮结果。</param>
    /// <param name="asyncMode">是否允许异步参与者。</param>
    /// <param name="cancellationToken">当前启动取消令牌。</param>
    /// <param name="callerToken">保留外部取消身份，用于向调用方传播取消结果。</param>
    /// <returns>内部结果发布完成；异常通过 completion 传播。</returns>
    private async Task CompleteInitializationAsync(
        TaskCompletionSource completion, bool asyncMode, CancellationToken cancellationToken, CancellationToken callerToken)
    {
        _insideInitialization.Value = true;
        Exception? failure = null;
        try
        {
            await ConfigureAndStartAsync(asyncMode, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception error)
        {
            failure = await CleanupFailedStartupAsync(error, callerToken).ConfigureAwait(false);
        }
        finally
        {
            _insideInitialization.Value = false;
            ReleaseStartupCancellation();
        }
        if (failure == null)
            completion.TrySetResult();
        else
        {
            _lifecycle.CompleteFailure(failure);
            if (failure is OperationCanceledException canceled)
                completion.TrySetCanceled(canceled.CancellationToken);
            else
                completion.TrySetException(failure);
        }
    }

    /// <summary>失败阶段通知、宿主分离和 DI 清理均不能绕过彼此；保留原异常与全部清理失败。</summary>
    /// <param name="error">原始启动错误。</param>
    /// <param name="callerToken">外部取消身份。</param>
    /// <returns>清理之后向调用方传播的失败。</returns>
    private async Task<Exception> CleanupFailedStartupAsync(Exception error, CancellationToken callerToken)
    {
        var errors = new List<Exception> { error };
        _access.BeginShutdown();
        using (_access.EnterCleanup())
        {
            await RecordCleanupErrorAsync(_lifecycle.MarkAsFailedAsync, errors).ConfigureAwait(false);
            await RecordCleanupErrorAsync(StopHostAsync, errors).ConfigureAwait(false);
            await RecordCleanupErrorAsync(_lifecycle.CleanupAsync, errors).ConfigureAwait(false);
        }
        if (errors.Count != 1)
            return new AggregateException("Architecture startup failed.", errors);
        return error is OperationCanceledException && callerToken.IsCancellationRequested
            ? new OperationCanceledException(error.Message, error, callerToken)
            : error;
    }

    /// <summary>阶段和资源清理失败只记录，不中断后续清理。</summary>
    /// <param name="cleanup">必须尝试的清理动作。</param>
    /// <param name="errors">当前操作错误集合。</param>
    /// <returns>当前动作已结束。</returns>
    private static async ValueTask RecordCleanupErrorAsync(Func<ValueTask> cleanup, List<Exception> errors)
    {
        try
        {
            await cleanup().ConfigureAwait(false);
        }
        catch (Exception error)
        {
            errors.Add(error);
        }
    }

    /// <summary>启动失败和正常停止复用宿主停止结果，包括其异常。</summary>
    /// <returns>完整宿主停止结果。</returns>
    private ValueTask StopHostAsync()
    {
        _hostStopTask ??= StopHostCoreAsync();
        return new ValueTask(_hostStopTask);
    }

    /// <summary>将宿主同步抛错也封装在缓存任务中。</summary>
    /// <returns>宿主停止完成。</returns>
    private async Task StopHostCoreAsync() => await OnStoppingAsync().ConfigureAwait(false);

    /// <summary>启动结束后释放 linked-token 注册；已移交停止操作的源由停止操作释放。</summary>
    private void ReleaseStartupCancellation()
    {
        CancellationTokenSource? cancellation;
        lock (_operationLock)
        {
            cancellation = _startupCancellation;
            _startupCancellation = null;
        }
        cancellation?.Dispose();
    }

    /// <summary>关闭描述窗口后只构建一次 provider，再按依赖顺序启动组件与宿主。</summary>
    /// <param name="asyncMode">是否允许异步参与者。</param>
    /// <param name="cancellationToken">启动取消令牌。</param>
    /// <returns>所有准备阶段完成。</returns>
    private async Task ConfigureAndStartAsync(bool asyncMode, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _configurationOpen = true;
        try
        {
            _bootstrapper.Prepare(_context, Configurator);
            ConfigureServices(_services.Container.GetServicesUnsafe);
        }
        finally
        {
            _configurationOpen = false;
        }

        cancellationToken.ThrowIfCancellationRequested();
        _services.Container.Freeze();
        _access.EnableServices();
        _componentRegistry.ActivateComponents();
        _lifecycle.CapturePhaseListeners();
        await _services.ModuleManager.InitializeAllAsync(asyncMode, cancellationToken).ConfigureAwait(false);
        await _lifecycle.InitializeAllComponentsAsync(asyncMode, cancellationToken).ConfigureAwait(false);

        if (asyncMode)
            await OnStartedAsync(cancellationToken).ConfigureAwait(false);
        else
        {
            var asyncHook = GetType().GetMethod(nameof(OnStartedAsync), BindingFlags.Instance | BindingFlags.NonPublic);
            if (asyncHook?.DeclaringType != typeof(Architecture))
                throw new InvalidOperationException("An asynchronous host hook requires InitializeAsync().");
            OnStarted();
        }
        cancellationToken.ThrowIfCancellationRequested();
        await _lifecycle.MarkAsReadyAsync(asyncMode, cancellationToken).ConfigureAwait(false);
    }


    /// <summary>只有描述阶段可以添加组件、模块或钩子；Ready 和激活回调均不可重开窗口。</summary>
    private void EnsureConfigurationOpen()
    {
        if (!_configurationOpen)
            throw new InvalidOperationException("Register services and modules only inside ConfigureServices.");
    }

    /// <summary>登记当前实例独立的服务模块，不写全局运行态。</summary>
    /// <param name="module">本实例模块。</param>
    /// <returns>传入模块。</returns>
    public IServiceModule RegisterServiceModule(IServiceModule module)
    {
        EnsureConfigurationOpen();
        ArgumentNullException.ThrowIfNull(module);
        _services.ModuleManager.RegisterModule(module);
        return module;
    }

    /// <summary>等待启动成功、失败或取消；失败清理先于任务完成。</summary>
    /// <returns>本实例启动结果。</returns>
    public Task WaitUntilReadyAsync() => _lifecycle.WaitUntilReadyAsync();

    #endregion

    #region Destruction

    /// <summary>所有销毁调用等待同一次停止和异步资源释放；不解绑其他实例的全局状态。</summary>
    /// <returns>完整清理结果。</returns>
    public virtual ValueTask DestroyAsync()
    {
        if ((_insideInitialization.Value && _initializationTask is { IsCompleted: false }) ||
            (_insideDestruction.Value && _destructionTask is { IsCompleted: false }))
            throw new InvalidOperationException("Do not await destruction from this architecture's initialization or destruction callback.");
        TaskCompletionSource completion;
        Task? startup;
        CancellationTokenSource? cancellation;
        lock (_operationLock)
        {
            if (_destructionTask != null)
                return new ValueTask(_destructionTask);
            completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _destructionTask = completion.Task;
            startup = _initializationTask;
            cancellation = _startupCancellation;
            _startupCancellation = null;
            _access.BeginShutdown();
        }
        _ = CompleteDestructionAsync(completion, startup, cancellation);
        return new ValueTask(completion.Task);
    }

    /// <summary>取消并等待正在进行的启动，再进入同一幂等清理流程。</summary>
    /// <param name="completion">已发布的销毁结果。</param>
    /// <param name="startup">可能尚未完成的启动。</param>
    /// <param name="cancellation">启动取消源。</param>
    /// <returns>内部任务结果发布完成。</returns>
    private async Task CompleteDestructionAsync(
        TaskCompletionSource completion, Task? startup, CancellationTokenSource? cancellation)
    {
        _insideDestruction.Value = true;
        var errors = new List<Exception>();
        try
        {
            try
            {
                cancellation?.Cancel();
            }
            catch (Exception error)
            {
                // 用户取消回调失败不允许绕过等待启动和释放根 provider。
                errors.Add(error);
            }
            if (startup != null)
            {
                try
                {
                    await startup.ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // 启动失败已清理并传播给启动调用方，停止仍需抵达终态。
                }
            }
            using (_access.EnterCleanup())
            {
                await RecordCleanupErrorAsync(StopHostAsync, errors).ConfigureAwait(false);
                await RecordCleanupErrorAsync(_lifecycle.DestroyAsync, errors).ConfigureAwait(false);
            }
        }
        finally
        {
            cancellation?.Dispose();
            _insideDestruction.Value = false;
        }
        if (errors.Count == 0)
            completion.TrySetResult();
        else
            completion.TrySetException(new AggregateException("Architecture destruction failed.", errors));
    }

    /// <summary>标准异步资源释放入口，与 DestroyAsync 共享同一次任务。</summary>
    /// <returns>资源全部释放。</returns>
    public ValueTask DisposeAsync() => DestroyAsync();

    /// <summary>同步销毁包装；包含异步资源时调用方应优先等待 DestroyAsync。</summary>
    [Obsolete("Use DestroyAsync() to await asynchronous cleanup.")]
    public virtual void Destroy() => DestroyAsync().AsTask().GetAwaiter().GetResult();

    #endregion
}
