using GFramework.Core.Abstractions.logging;
using GFramework.Core.logging;
using GFramework.Game.Abstractions.enums;
using GFramework.Game.Abstractions.ui;

namespace GFramework.Game.ui.handler;

/// <summary>
///     日志UI切换处理器，用于记录UI切换的详细信息
/// </summary>
public sealed class LoggingTransitionHandler : UiTransitionHandlerBase
{
    /// <summary>
    ///     日志记录器实例，用于记录UI切换相关信息
    /// </summary>
    private static readonly ILogger Log = LoggerFactoryResolver.Provider.CreateLogger(nameof(LoggingTransitionHandler));

    /// <summary>
    ///     获取处理器优先级，数值越大优先级越高
    /// </summary>
    public override int Priority => 999;

    /// <summary>
    ///     获取处理器处理的UI切换阶段，处理所有阶段
    /// </summary>
    public override UiTransitionPhases Phases => UiTransitionPhases.All;

    /// <summary>
    ///     处理UI切换事件的异步方法
    /// </summary>
    /// <param name="event">UI切换事件对象，包含切换的相关信息</param>
    /// <param name="cancellationToken">取消令牌，用于控制异步操作的取消</param>
    /// <returns>表示异步操作的任务</returns>
    public override Task HandleAsync(UiTransitionEvent @event, CancellationToken cancellationToken)
    {
        // 记录UI切换的详细信息到日志
        Log.Info(
            "UI Transition: Phases={0}, Type={1}, From={2}, To={3}, Policy={4}",
            @event.Get<string>("Phases", "Unknown"),
            @event.TransitionType,
            @event.FromUiKey,
            @event.ToUiKey ?? "None",
            @event.Policy
        );

        return Task.CompletedTask;
    }
}