// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Routing;
using GFramework.Core.Abstractions.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using GFramework.Core.Logging;
using GFramework.Game.Abstractions.Enums;
using GFramework.Game.Abstractions.UI;

namespace GFramework.Game.UI;

/// <summary>
///     UI切换处理器管道，负责管理和执行UI切换扩展点
/// </summary>
public class UiTransitionPipeline(ILogger? logger = null)
{
    // 标准日志记录器；未注入时使用空输出，不读取进程级状态。
    private readonly ILogger _logger = logger ?? NullLogger<UiTransitionPipeline>.Instance;
    private readonly List<IUiAroundTransitionHandler> _aroundHandlers = [];
    private readonly Dictionary<IUiAroundTransitionHandler, UiTransitionHandlerOptions> _aroundOptions = new();
    private readonly List<IUiTransitionHandler> _handlers = [];
    private readonly Dictionary<IUiTransitionHandler, UiTransitionHandlerOptions> _options = new();

    /// <summary>
    ///     注册UI切换处理器
    /// </summary>
    /// <param name="handler">处理器实例</param>
    /// <param name="options">执行选项</param>
    public void RegisterHandler(IUiTransitionHandler handler, UiTransitionHandlerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(handler);

        if (_handlers.Contains(handler))
        {
            _logger.LogDebug("Handler already registered: {0}", handler.GetType().Name);
            return;
        }

        _handlers.Add(handler);
        _options[handler] = options ?? new UiTransitionHandlerOptions();
        _logger.LogDebug(
            "Handler registered: {0}, Priority={1}, Phases={2}, TimeoutMs={3}",
            handler.GetType().Name,
            handler.Priority,
            handler.Phases,
            _options[handler].TimeoutMs
        );
    }

    /// <summary>
    ///     注销UI切换处理器
    /// </summary>
    /// <param name="handler">处理器实例</param>
    public void UnregisterHandler(IUiTransitionHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        if (!_handlers.Remove(handler)) return;
        _options.Remove(handler);
        _logger.LogDebug("Handler unregistered: {0}", handler.GetType().Name);
    }

    /// <summary>
    ///     注册 Around 中间件处理器
    /// </summary>
    /// <param name="handler">处理器实例</param>
    /// <param name="options">执行选项</param>
    public void RegisterAroundHandler(IUiAroundTransitionHandler handler, UiTransitionHandlerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(handler);

        if (_aroundHandlers.Contains(handler))
        {
            _logger.LogDebug("Around handler already registered: {0}", handler.GetType().Name);
            return;
        }

        _aroundHandlers.Add(handler);
        _aroundOptions[handler] = options ?? new UiTransitionHandlerOptions();
        _logger.LogDebug(
            "Around handler registered: {0}, Priority={1}",
            handler.GetType().Name,
            handler.Priority
        );
    }

    /// <summary>
    ///     注销 Around 中间件处理器
    /// </summary>
    /// <param name="handler">处理器实例</param>
    public void UnregisterAroundHandler(IUiAroundTransitionHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        if (!_aroundHandlers.Remove(handler)) return;
        _aroundOptions.Remove(handler);
        _logger.LogDebug("Around handler unregistered: {0}", handler.GetType().Name);
    }

    /// <summary>
    ///     执行指定阶段的所有Handler
    /// </summary>
    /// <param name="event">UI切换事件</param>
    /// <param name="phases">执行阶段</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    public async Task ExecuteAsync(
        UiTransitionEvent @event,
        UiTransitionPhases phases,
        CancellationToken cancellationToken = default
    )
    {
        @event.Set("Phases", phases.ToString());

        _logger.LogDebug(
            "Execute pipeline: Phases={0}, From={1}, To={2}, Type={3}, HandlerCount={4}",
            phases,
            @event.FromUiKey ?? "None",
            @event.ToUiKey ?? "None",
            @event.TransitionType,
            _handlers.Count
        );

        var sortedHandlers = FilterAndSortHandlers(@event, phases);

        if (sortedHandlers.Count == 0)
        {
            _logger.LogDebug("No handlers to execute for phases: {0}", phases);
            return;
        }

        _logger.LogDebug(
            "Executing {0} handlers for phases {1}",
            sortedHandlers.Count,
            phases
        );

        foreach (var handler in sortedHandlers)
        {
            var options = _options[handler];
            await ExecuteSingleHandlerAsync(handler, options, @event, cancellationToken).ConfigureAwait(true);
        }

        _logger.LogDebug("Pipeline execution completed for phases: {0}", phases);
    }

    /// <summary>
    ///     执行 Around 中间件处理器，包裹核心操作
    /// </summary>
    /// <param name="event">UI切换事件</param>
    /// <param name="coreAction">核心操作委托</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    public async Task ExecuteAroundAsync(
        UiTransitionEvent @event,
        Func<Task> coreAction,
        CancellationToken cancellationToken = default)
    {
        var handlers = _aroundHandlers
            .Where(h => h.ShouldHandle(@event))
            .OrderBy(h => h.Priority)
            .ToList();

        if (handlers.Count == 0)
        {
            await coreAction().ConfigureAwait(true);
            return;
        }

        _logger.LogDebug(
            "Executing {0} around handlers for event: {1}",
            handlers.Count,
            @event.TransitionType
        );

        // 构建中间件链
        var hostContext = SynchronizationContext.Current;
        Func<Task> pipeline = () => RouterCallbackDispatcher.InvokeAsync(hostContext, coreAction);
        for (int i = handlers.Count - 1; i >= 0; i--)
        {
            var handler = handlers[i];
            var options = _aroundOptions[handler];
            var nextCallback = pipeline;
            Func<Task> next = () => RouterCallbackDispatcher.InvokeAsync(hostContext, nextCallback);

            pipeline = () => ExecuteSingleAroundHandlerAsync(
                handler, options, @event, next, cancellationToken);
        }

        await pipeline().ConfigureAwait(true);
    }

    private List<IUiTransitionHandler> FilterAndSortHandlers(
        UiTransitionEvent @event,
        UiTransitionPhases phases)
    {
        return _handlers
            .Where(h => h.Phases.HasFlag(phases) && h.ShouldHandle(@event, phases))
            .OrderBy(h => h.Priority)
            .ToList();
    }

    private async Task ExecuteSingleHandlerAsync(
        IUiTransitionHandler handler,
        UiTransitionHandlerOptions options,
        UiTransitionEvent @event,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Executing handler: {0}, Priority={1}",
            handler.GetType().Name,
            handler.Priority
        );

        using var timeoutCts = options.TimeoutMs > 0
            ? new CancellationTokenSource(options.TimeoutMs)
            : null;
        using var linkedCts = timeoutCts != null && cancellationToken.CanBeCanceled
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token)
            : null;

        try
        {
            await handler.HandleAsync(
                @event,
                linkedCts?.Token ?? timeoutCts?.Token ?? cancellationToken
            ).ConfigureAwait(true);

            _logger.LogDebug("Handler completed: {0}", handler.GetType().Name);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && timeoutCts?.IsCancellationRequested == true)
        {
            _logger.LogError(
                "Handler timeout: {0}, TimeoutMs={1}",
                handler.GetType().Name,
                options.TimeoutMs
            );

            if (options.ContinueOnError) return;
            _logger.LogError("Stopping pipeline due to timeout and ContinueOnError=false");
            throw;
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Handler cancelled: {0}", handler.GetType().Name);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError("Handler failed: {0}, Error: {1}", handler.GetType().Name, ex.Message);

            if (options.ContinueOnError) return;
            _logger.LogError("Stopping pipeline due to error and ContinueOnError=false");
            throw;
        }
    }

    private async Task ExecuteSingleAroundHandlerAsync(
        IUiAroundTransitionHandler handler,
        UiTransitionHandlerOptions options,
        UiTransitionEvent @event,
        Func<Task> next,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Executing around handler: {0}", handler.GetType().Name);

        using var timeoutCts = options.TimeoutMs > 0
            ? new CancellationTokenSource(options.TimeoutMs)
            : null;
        using var linkedCts = timeoutCts != null && cancellationToken.CanBeCanceled
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token)
            : null;

        try
        {
            await handler.HandleAsync(@event, next, linkedCts?.Token ?? timeoutCts?.Token ?? cancellationToken).ConfigureAwait(true);

            _logger.LogDebug("Around handler completed: {0}", handler.GetType().Name);
        }
        catch (OperationCanceledException)
        {
            // Cancellation must not turn into a successful transition, even with ContinueOnError.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError("Around handler failed: {0}, Error: {1}",
                handler.GetType().Name, ex.Message);

            if (!options.ContinueOnError)
                throw;
        }
    }
}
