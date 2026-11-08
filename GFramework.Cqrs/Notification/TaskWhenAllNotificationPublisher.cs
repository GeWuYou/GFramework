// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Threading.Tasks;
using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Cqrs.Notification;

/// <summary>
///     以内置 <c>Task.WhenAll(...)</c> 策略并行分发通知处理器。
/// </summary>
/// <remarks>
///     <para>该实现会先为当前发布调用中的每个处理器创建独立执行任务，再等待全部任务完成。</para>
///     <para>它不会保留默认顺序发布器的“首个异常立即停止”语义；如果多个处理器失败，返回任务会聚合这些异常。</para>
///     <para>适合处理器之间互不依赖，且调用方更关心总耗时而不是处理顺序的场景。</para>
/// </remarks>
public sealed class TaskWhenAllNotificationPublisher : INotificationPublisher
{
    /// <summary>
    ///     并行启动当前通知的所有处理器，并等待它们全部结束。
    /// </summary>
    /// <typeparam name="TNotification">通知类型。</typeparam>
    /// <param name="context">当前发布调用的执行上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>表示所有处理器都已完成的值任务。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context" /> 为 <see langword="null" />。</exception>
    public ValueTask PublishAsync<TNotification>(
        NotificationPublishContext<TNotification> context,
        CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Handlers.Count switch
        {
            0 => ValueTask.CompletedTask,
            1 => context.InvokeHandlerAsync(context.Handlers[0], cancellationToken),
            _ => PublishCoreAsync(context, cancellationToken),
        };
    }

    /// <summary>
    ///     为多处理器场景建立并行等待，确保单个处理器的同步异常也会被收敛到返回任务中。
    /// </summary>
    private static async ValueTask PublishCoreAsync<TNotification>(
        NotificationPublishContext<TNotification> context,
        CancellationToken cancellationToken)
        where TNotification : INotification
    {
        var tasks = new Task[context.Handlers.Count];

        for (var index = 0; index < context.Handlers.Count; index++)
        {
            tasks[index] = InvokeHandlerSafelyAsync(context, context.Handlers[index], cancellationToken).AsTask();
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    /// <summary>
    ///     通过异步包装把同步抛出的处理器异常也转换成可聚合的任务结果。
    /// </summary>
    private static async ValueTask InvokeHandlerSafelyAsync<TNotification>(
        NotificationPublishContext<TNotification> context,
        object handler,
        CancellationToken cancellationToken)
        where TNotification : INotification
    {
        await context.InvokeHandlerAsync(handler, cancellationToken).ConfigureAwait(false);
    }
}
