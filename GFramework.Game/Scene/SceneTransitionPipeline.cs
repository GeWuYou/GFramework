// Copyright (c) 2026 GeWuYou
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using GFramework.Game.Routing;
using GFramework.Core.Abstractions.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using GFramework.Core.Logging;
using GFramework.Game.Abstractions.Enums;
using GFramework.Game.Abstractions.Scene;

namespace GFramework.Game.Scene;

/// <summary>
/// 场景过渡处理器管道，负责管理和执行场景切换扩展点。
/// 提供了处理器的注册、注销和按优先级顺序执行的功能。
/// </summary>
public class SceneTransitionPipeline(ILogger? logger = null)
{
    // 标准日志记录器；未注入时使用空输出，不读取进程级状态。
    private readonly ILogger _logger = logger ?? NullLogger<SceneTransitionPipeline>.Instance;
    private readonly List<ISceneAroundTransitionHandler> _aroundHandlers = [];
    private readonly Dictionary<ISceneAroundTransitionHandler, SceneTransitionHandlerOptions> _aroundOptions = [];
    private readonly List<ISceneTransitionHandler> _handlers = [];
    private readonly Dictionary<ISceneTransitionHandler, SceneTransitionHandlerOptions> _options = [];

    /// <summary>
    /// 注册场景过渡处理器。
    /// </summary>
    /// <param name="handler">处理器实例。</param>
    /// <param name="options">执行选项，如果为 null 则使用默认选项。</param>
    public void RegisterHandler(ISceneTransitionHandler handler, SceneTransitionHandlerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(handler);

        if (_handlers.Contains(handler))
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Handler already registered: {HandlerType}", handler.GetType().Name);
            }
            return;
        }

        _handlers.Add(handler);
        _options[handler] = options ?? new SceneTransitionHandlerOptions();
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug(
                "Handler registered: {HandlerType}, Priority={Priority}, Phases={Phases}, TimeoutMs={TimeoutMs}",
                handler.GetType().Name, handler.Priority, handler.Phases, _options[handler].TimeoutMs);
        }
    }

    /// <summary>
    /// 注销场景过渡处理器。
    /// </summary>
    /// <param name="handler">处理器实例。</param>
    public void UnregisterHandler(ISceneTransitionHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        if (!_handlers.Remove(handler))
            return;
        _options.Remove(handler);
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Handler unregistered: {HandlerType}", handler.GetType().Name);
        }
    }

    /// <summary>
    /// 注册 Around 中间件处理器。
    /// </summary>
    /// <param name="handler">处理器实例。</param>
    /// <param name="options">执行选项，如果为 null 则使用默认选项。</param>
    public void RegisterAroundHandler(ISceneAroundTransitionHandler handler,
        SceneTransitionHandlerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(handler);

        if (_aroundHandlers.Contains(handler))
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Around handler already registered: {HandlerType}", handler.GetType().Name);
            }
            return;
        }

        _aroundHandlers.Add(handler);
        _aroundOptions[handler] = options ?? new SceneTransitionHandlerOptions();
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Around handler registered: {HandlerType}, Priority={Priority}",
                handler.GetType().Name, handler.Priority);
        }
    }

    /// <summary>
    /// 注销 Around 中间件处理器。
    /// </summary>
    /// <param name="handler">处理器实例。</param>
    public void UnregisterAroundHandler(ISceneAroundTransitionHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        if (!_aroundHandlers.Remove(handler))
            return;
        _aroundOptions.Remove(handler);
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Around handler unregistered: {HandlerType}", handler.GetType().Name);
        }
    }

    /// <summary>
    /// 执行指定阶段的所有处理器。
    /// </summary>
    /// <param name="event">场景过渡事件。</param>
    /// <param name="phases">执行阶段。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>异步任务。</returns>
    public async Task ExecuteAsync(
        SceneTransitionEvent @event,
        SceneTransitionPhases phases,
        CancellationToken cancellationToken = default
    )
    {
        @event.Set("Phases", phases.ToString());

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug(
                "Execute pipeline: Phases={Phases}, From={FromScene}, To={ToScene}, Type={TransitionType}, HandlerCount={HandlerCount}",
                phases, @event.FromSceneKey ?? "None", @event.ToSceneKey ?? "None", @event.TransitionType, _handlers.Count);
        }

        var sortedHandlers = FilterAndSortHandlers(@event, phases);

        if (sortedHandlers.Count == 0)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("No handlers to execute for phases: {Phases}", phases);
            }
            return;
        }

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Executing {HandlerCount} handlers for phases {Phases}", sortedHandlers.Count, phases);
        }

        foreach (var handler in sortedHandlers)
        {
            var options = _options[handler];
            await ExecuteSingleHandlerAsync(handler, options, @event, cancellationToken).ConfigureAwait(true);
        }

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Pipeline execution completed for phases: {Phases}", phases);
        }
    }

    /// <summary>
    /// 执行 Around 中间件处理器，包裹核心操作。
    /// </summary>
    /// <param name="event">场景过渡事件。</param>
    /// <param name="coreAction">核心操作委托。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>异步任务。</returns>
    public async Task ExecuteAroundAsync(
        SceneTransitionEvent @event,
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

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Executing {HandlerCount} around handlers for event: {TransitionType}", handlers.Count, @event.TransitionType);
        }

        // 构建中间件链
        var hostContext = SynchronizationContext.Current;
        Func<Task> pipeline = () => RouterCallbackDispatcher.InvokeAsync(hostContext, coreAction);
        for (int i = handlers.Count - 1; i >= 0; i--)
        {
            var handler = handlers[i];
            var options = _aroundOptions[handler];
            var nextCallback = pipeline;
            Task next() => RouterCallbackDispatcher.InvokeAsync(hostContext, nextCallback);

            pipeline = () => ExecuteSingleAroundHandlerAsync(
                handler, options, @event, next, cancellationToken);
        }

        await pipeline().ConfigureAwait(true);
    }

    private List<ISceneTransitionHandler> FilterAndSortHandlers(
        SceneTransitionEvent @event,
        SceneTransitionPhases phases)
    {
        return [.. _handlers
            .Where(h => h.Phases.HasFlag(phases) && h.ShouldHandle(@event, phases))
            .OrderBy(h => h.Priority)];
    }

    private async Task ExecuteSingleHandlerAsync(
        ISceneTransitionHandler handler,
        SceneTransitionHandlerOptions options,
        SceneTransitionEvent @event,
        CancellationToken cancellationToken)
    {
        LogHandlerStarting(handler);

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

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Handler completed: {HandlerType}", handler.GetType().Name);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && timeoutCts?.IsCancellationRequested == true)
        {
            LogHandlerTimeout(handler, options.TimeoutMs);

            if (options.ContinueOnError)
                return;
            _logger.LogError("Stopping pipeline due to timeout and ContinueOnError=false");
            throw;
        }
        catch (OperationCanceledException)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Handler cancelled: {HandlerType}", handler.GetType().Name);
            }
            throw;
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex, "Scene transition handler {HandlerType} failed", handler.GetType().Name);
            }

            if (options.ContinueOnError)
                return;
            _logger.LogError("Stopping pipeline due to error and ContinueOnError=false");
            throw;
        }
    }

    private async Task ExecuteSingleAroundHandlerAsync(
        ISceneAroundTransitionHandler handler,
        SceneTransitionHandlerOptions options,
        SceneTransitionEvent @event,
        Func<Task> next,
        CancellationToken cancellationToken)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Executing around handler: {HandlerType}", handler.GetType().Name);
        }

        using var timeoutCts = options.TimeoutMs > 0
            ? new CancellationTokenSource(options.TimeoutMs)
            : null;
        using var linkedCts = timeoutCts != null && cancellationToken.CanBeCanceled
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token)
            : null;

        try
        {
            await handler.HandleAsync(@event, next, linkedCts?.Token ?? timeoutCts?.Token ?? cancellationToken).ConfigureAwait(true);

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Around handler completed: {HandlerType}", handler.GetType().Name);
            }
        }
        catch (OperationCanceledException)
        {
            // Cancellation must not turn into a successful transition, even with ContinueOnError.
            throw;
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex, "Scene transition middleware {HandlerType} failed", handler.GetType().Name);
            }

            if (!options.ContinueOnError)
                throw;
        }
    }

    /// <summary>Defers diagnostic argument evaluation until the configured factory enables stage-start logging.</summary>
    /// <param name="handler">The stage handler about to execute.</param>
    private void LogHandlerStarting(ISceneTransitionHandler handler)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Executing handler: {HandlerType}, Priority={Priority}", handler.GetType().Name, handler.Priority);
        }
    }

    /// <summary>Reports an independently timed-out handler without changing timeout or continuation policy.</summary>
    /// <param name="handler">The timed-out stage handler.</param>
    /// <param name="timeoutMs">The configured timeout.</param>
    private void LogHandlerTimeout(ISceneTransitionHandler handler, int timeoutMs)
    {
        if (_logger.IsEnabled(LogLevel.Error))
        {
            _logger.LogError("Handler timeout: {HandlerType}, TimeoutMs={TimeoutMs}", handler.GetType().Name, timeoutMs);
        }
    }
}
