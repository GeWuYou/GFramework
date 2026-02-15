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

using GFramework.Core.Abstractions.logging;
using GFramework.Core.logging;
using GFramework.Game.Abstractions.enums;
using GFramework.Game.Abstractions.scene;

namespace GFramework.Game.scene;

/// <summary>
/// 场景过渡处理器管道，负责管理和执行场景切换扩展点。
/// 提供了处理器的注册、注销和按优先级顺序执行的功能。
/// </summary>
public class SceneTransitionPipeline
{
    private static readonly ILogger Log = LoggerFactoryResolver.Provider.CreateLogger(nameof(SceneTransitionPipeline));
    private readonly List<ISceneTransitionHandler> _handlers = [];
    private readonly Dictionary<ISceneTransitionHandler, SceneTransitionHandlerOptions> _options = new();

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
            Log.Debug("Handler already registered: {0}", handler.GetType().Name);
            return;
        }

        _handlers.Add(handler);
        _options[handler] = options ?? new SceneTransitionHandlerOptions();
        Log.Debug(
            "Handler registered: {0}, Priority={1}, Phases={2}, TimeoutMs={3}",
            handler.GetType().Name,
            handler.Priority,
            handler.Phases,
            _options[handler].TimeoutMs
        );
    }

    /// <summary>
    /// 注销场景过渡处理器。
    /// </summary>
    /// <param name="handler">处理器实例。</param>
    public void UnregisterHandler(ISceneTransitionHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        if (!_handlers.Remove(handler)) return;
        _options.Remove(handler);
        Log.Debug("Handler unregistered: {0}", handler.GetType().Name);
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

        Log.Debug(
            "Execute pipeline: Phases={0}, From={1}, To={2}, Type={3}, HandlerCount={4}",
            phases,
            @event.FromSceneKey,
            @event.ToSceneKey ?? "None",
            @event.TransitionType,
            _handlers.Count
        );

        var sortedHandlers = FilterAndSortHandlers(@event, phases);

        if (sortedHandlers.Count == 0)
        {
            Log.Debug("No handlers to execute for phases: {0}", phases);
            return;
        }

        Log.Debug(
            "Executing {0} handlers for phases {1}",
            sortedHandlers.Count,
            phases
        );

        foreach (var handler in sortedHandlers)
        {
            var options = _options[handler];
            await ExecuteSingleHandlerAsync(handler, options, @event, cancellationToken);
        }

        Log.Debug("Pipeline execution completed for phases: {0}", phases);
    }

    private List<ISceneTransitionHandler> FilterAndSortHandlers(
        SceneTransitionEvent @event,
        SceneTransitionPhases phases)
    {
        return _handlers
            .Where(h => h.Phases.HasFlag(phases) && h.ShouldHandle(@event, phases))
            .OrderBy(h => h.Priority)
            .ToList();
    }

    private static async Task ExecuteSingleHandlerAsync(
        ISceneTransitionHandler handler,
        SceneTransitionHandlerOptions options,
        SceneTransitionEvent @event,
        CancellationToken cancellationToken)
    {
        Log.Debug(
            "Executing handler: {0}, Priority={1}",
            handler.GetType().Name,
            handler.Priority
        );

        try
        {
            using var timeoutCts = options.TimeoutMs > 0
                ? new CancellationTokenSource(options.TimeoutMs)
                : null;

            using var linkedCts = timeoutCts != null && cancellationToken.CanBeCanceled
                ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token)
                : null;

            await handler.HandleAsync(
                @event,
                linkedCts?.Token ?? cancellationToken
            ).ConfigureAwait(false);

            Log.Debug("Handler completed: {0}", handler.GetType().Name);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            Log.Error(
                "Handler timeout: {0}, TimeoutMs={1}",
                handler.GetType().Name,
                options.TimeoutMs
            );

            if (options.ContinueOnError) return;
            Log.Error("Stopping pipeline due to timeout and ContinueOnError=false");
            throw;
        }
        catch (OperationCanceledException)
        {
            Log.Debug("Handler cancelled: {0}", handler.GetType().Name);
            throw;
        }
        catch (Exception ex)
        {
            Log.Error("Handler failed: {0}, Error: {1}", handler.GetType().Name, ex.Message);

            if (options.ContinueOnError) return;
            Log.Error("Stopping pipeline due to error and ContinueOnError=false");
            throw;
        }
    }
}