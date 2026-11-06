// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Enums;
using GFramework.Core.Abstractions.Lifecycle;
using Microsoft.Extensions.Logging;
using GFramework.Core.Abstractions.Model;
using GFramework.Core.Abstractions.Utility;

namespace GFramework.Core.Architectures;

/// <summary>
///     在同一个正式 provider 上编排组件初始化、阶段和业务停止。配置、激活和资源释放职责分开，
///     失败的启动与正常销毁共享一次清理，不需要额外容器或预注册单例缓存。
/// </summary>
internal sealed class ArchitectureLifecycle(
    IArchitecture architecture,
    IArchitectureConfiguration configuration,
    IArchitectureServices services,
    ILogger logger)
{
    private readonly ArchitecturePhaseCoordinator _phases = new(architecture, configuration, logger);
    private readonly ArchitectureDisposer _disposer = new();
    private readonly List<object> _components = [];
    private readonly HashSet<object> _componentSet = new(ReferenceEqualityComparer.Instance);
    private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool _initialized;
    private Task? _cleanupTask;

    /// <summary>当前阶段。</summary>
    public ArchitecturePhase CurrentPhase => _phases.CurrentPhase;

    /// <summary>当前实例是否完全就绪。</summary>
    public bool IsReady => CurrentPhase == ArchitecturePhase.Ready && _ready.Task.IsCompletedSuccessfully;

    /// <summary>组件初始化是否已经完成。</summary>
    public bool IsInitialized => _initialized;

    /// <summary>阶段完成广播；异常会进入同一失败清理路径。</summary>
    public event EventHandler<ArchitecturePhaseChangedEventArgs>? PhaseChanged;

    /// <summary>在配置阶段登记当前实例钩子。</summary>
    /// <param name="hook">钩子。</param>
    /// <returns>传入钩子。</returns>
    public IArchitectureLifecycleHook RegisterLifecycleHook(IArchitectureLifecycleHook hook) =>
        _phases.RegisterLifecycleHook(hook);

    /// <summary>正式 provider 激活后登记参与者，不通过全局或 alias 反向推断生命周期。</summary>
    /// <param name="component">实际实例。</param>
    public void RegisterLifecycleComponent(object component)
    {
        if (_initialized)
            throw new InvalidOperationException("Component registration is closed.");
        if (_componentSet.Add(component))
            _components.Add(component);
    }

    /// <summary>为阶段广播保留组件和额外显式监听器；不重复调用同一个 alias。</summary>
    public void CapturePhaseListeners()
    {
        _phases.SetListeners(_components.OfType<IArchitecturePhaseListener>()
            .Concat(services.Container.GetAll<IArchitecturePhaseListener>()));
    }

    /// <summary>按 Utility、Model、System 顺序初始化，不在初始化过程中修改容器。</summary>
    /// <param name="asyncMode">是否允许异步参与者。</param>
    /// <param name="cancellationToken">启动取消令牌。</param>
    /// <returns>全部组件初始化完成。</returns>
    public async Task InitializeAllComponentsAsync(bool asyncMode, CancellationToken cancellationToken)
    {
        await InitializeGroupAsync(
            _components.Where(component => component is IUtility),
            ArchitecturePhase.BeforeUtilityInit, ArchitecturePhase.AfterUtilityInit,
            asyncMode, cancellationToken).ConfigureAwait(false);
        await InitializeGroupAsync(
            _components.Where(component => component is IModel and not IUtility),
            ArchitecturePhase.BeforeModelInit, ArchitecturePhase.AfterModelInit,
            asyncMode, cancellationToken).ConfigureAwait(false);
        await InitializeGroupAsync(
            _components.Where(component => component is not IUtility and not IModel),
            ArchitecturePhase.BeforeSystemInit, ArchitecturePhase.AfterSystemInit,
            asyncMode, cancellationToken).ConfigureAwait(false);
        _initialized = true;
    }

    /// <summary>进入阶段、顺序初始化参与者，再完成阶段通知。</summary>
    /// <param name="components">当前角色实例。</param>
    /// <param name="before">开始阶段。</param>
    /// <param name="after">完成阶段。</param>
    /// <param name="asyncMode">是否允许异步初始化。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>当前批次完成。</returns>
    private async Task InitializeGroupAsync(
        IEnumerable<object> components,
        ArchitecturePhase before,
        ArchitecturePhase after,
        bool asyncMode,
        CancellationToken cancellationToken)
    {
        await EnterPhaseAsync(before, asyncMode, cancellationToken).ConfigureAwait(false);
        foreach (var component in components)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (component is IAsyncInitializable && !asyncMode)
                throw new InvalidOperationException("An asynchronous component requires InitializeAsync().");
            // 只记录真正开始的初始化；尝试中途失败仍需要业务清理。
            _disposer.Register(component);
            if (component is IAsyncInitializable asyncInitializable)
            {
                await asyncInitializable.InitializeAsync(cancellationToken).ConfigureAwait(false);
            }
            else if (component is IInitializable initializable)
                initializable.Initialize();
            cancellationToken.ThrowIfCancellationRequested();
        }
        await EnterPhaseAsync(after, asyncMode, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>更新阶段并通知观察者。</summary>
    /// <param name="phase">目标阶段。</param>
    /// <param name="asyncMode">是否允许异步钩子。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>阶段处理完成。</returns>
    private async ValueTask EnterPhaseAsync(
        ArchitecturePhase phase, bool asyncMode, CancellationToken cancellationToken = default)
    {
        await _phases.EnterPhaseAsync(phase, asyncMode, cancellationToken).ConfigureAwait(false);
        PhaseChanged?.Invoke(this, new ArchitecturePhaseChangedEventArgs(phase));
    }

    /// <summary>完成 Ready 阶段之后才唤醒等待者。</summary>
    /// <param name="asyncMode">是否允许异步阶段钩子。</param>
    /// <param name="cancellationToken">启动取消令牌。</param>
    /// <returns>就绪广播完成。</returns>
    public async ValueTask MarkAsReadyAsync(bool asyncMode, CancellationToken cancellationToken)
    {
        await EnterPhaseAsync(ArchitecturePhase.Ready, asyncMode, cancellationToken).ConfigureAwait(false);
        _ready.TrySetResult();
    }

    /// <summary>先标记失败；等待者由外层在自动清理完成后唤醒。</summary>
    /// <returns>失败阶段广播完成。</returns>
    public ValueTask MarkAsFailedAsync() => EnterPhaseAsync(ArchitecturePhase.FailedInitialization, true);

    /// <summary>失败清理完成后让 Ready 等待者观察原失败或取消。</summary>
    /// <param name="exception">启动失败。</param>
    public void CompleteFailure(Exception exception)
    {
        if (exception is OperationCanceledException canceled)
            _ready.TrySetCanceled(canceled.CancellationToken);
        else
            _ready.TrySetException(exception);
    }

    /// <summary>等待本轮启动结果；仅创建但未启动的架构不会被误判为就绪。</summary>
    /// <returns>启动结果。</returns>
    public Task WaitUntilReadyAsync() => _ready.Task;

    /// <summary>只执行一次的业务停止、模块停止及异步 provider 释放，失败时继续后续清理。</summary>
    /// <returns>全部清理完成。</returns>
    public ValueTask CleanupAsync()
    {
        _cleanupTask ??= CleanupCoreAsync();
        return new ValueTask(_cleanupTask);
    }

    /// <summary>缓存成功或失败结果，避免失败启动后的显式销毁遗失先前清理异常。</summary>
    /// <returns>唯一资源清理结果。</returns>
    private async Task CleanupCoreAsync()
    {
        var errors = new List<Exception>();
        errors.AddRange(await _disposer.StopAsync().ConfigureAwait(false));
        try
        {
            await services.ModuleManager.DestroyAllAsync().ConfigureAwait(false);
        }
        catch (Exception error)
        {
            errors.Add(error);
        }
        try
        {
            await services.Container.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception error)
        {
            errors.Add(error);
        }
        if (errors.Count != 0)
            throw new AggregateException("Architecture cleanup failed.", errors);
    }

    /// <summary>停止阶段回调失败不能绕过资源清理；终态在 provider 释放之后广播。</summary>
    /// <returns>停止与释放全部完成。</returns>
    public async ValueTask DestroyAsync()
    {
        var errors = new List<Exception>();
        try
        {
            await EnterPhaseAsync(ArchitecturePhase.Destroying, true).ConfigureAwait(false);
        }
        catch (Exception error)
        {
            errors.Add(error);
        }
        try
        {
            await CleanupAsync().ConfigureAwait(false);
        }
        catch (Exception error)
        {
            errors.Add(error);
        }
        try
        {
            await EnterPhaseAsync(ArchitecturePhase.Destroyed, true).ConfigureAwait(false);
        }
        catch (Exception error)
        {
            errors.Add(error);
        }
        _ready.TrySetException(new ObjectDisposedException(nameof(Architecture)));
        if (errors.Count != 0)
            throw new AggregateException("Architecture destruction failed.", errors);
    }
}
