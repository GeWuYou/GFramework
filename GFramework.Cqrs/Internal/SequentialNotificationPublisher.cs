using GFramework.Cqrs.Abstractions.Cqrs;
using GFramework.Cqrs.Notification;

namespace GFramework.Cqrs.Internal;

/// <summary>
///     默认的通知发布器实现。
/// </summary>
/// <remarks>
///     该实现完整保留当前 CQRS runtime 的既有通知语义：按已解析顺序逐个执行处理器，
///     并在首个处理器抛出异常时立即停止后续发布。
/// </remarks>
internal sealed class SequentialNotificationPublisher : INotificationPublisher
{
    /// <summary>
    ///     按既定顺序逐个执行当前通知的处理器。
    /// </summary>
    /// <typeparam name="TNotification">通知类型。</typeparam>
    /// <param name="context">当前发布调用的执行上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>表示通知发布完成的值任务。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context" /> 为 <see langword="null" />。</exception>
    public async ValueTask PublishAsync<TNotification>(
        NotificationPublishContext<TNotification> context,
        CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (var handler in context.Handlers)
        {
            await context.InvokeHandlerAsync(handler, cancellationToken).ConfigureAwait(false);
        }
    }
}
