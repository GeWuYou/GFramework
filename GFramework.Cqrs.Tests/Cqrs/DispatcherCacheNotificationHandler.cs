using System.Threading;
using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     处理 <see cref="DispatcherCacheNotification" />。
/// </summary>
internal sealed class DispatcherCacheNotificationHandler : INotificationHandler<DispatcherCacheNotification>
{
    /// <summary>
    ///     消费通知，不执行额外副作用。
    /// </summary>
    /// <param name="notification">当前通知。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>已完成任务。</returns>
    public ValueTask Handle(DispatcherCacheNotification notification, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}
