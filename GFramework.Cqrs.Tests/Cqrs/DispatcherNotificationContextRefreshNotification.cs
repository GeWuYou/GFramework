using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     为 notification dispatch binding 上下文刷新回归提供带分发标识的最小通知。
/// </summary>
/// <param name="DispatchId">当前分发的稳定标识，便于断言缓存 binding 复用时观察到的是同一次通知。</param>
internal sealed record DispatcherNotificationContextRefreshNotification(string DispatchId) : INotification;
