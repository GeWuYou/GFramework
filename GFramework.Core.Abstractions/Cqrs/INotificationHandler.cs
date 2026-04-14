namespace GFramework.Core.Abstractions.Cqrs;

/// <summary>
///     表示处理通知消息的处理器契约。
/// </summary>
/// <typeparam name="TNotification">通知类型。</typeparam>
public interface INotificationHandler<in TNotification>
    where TNotification : INotification
{
    /// <summary>
    ///     处理通知消息。
    /// </summary>
    /// <param name="notification">要处理的通知。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>异步处理任务。</returns>
    ValueTask Handle(TNotification notification, CancellationToken cancellationToken);
}
