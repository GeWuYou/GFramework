using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     为 notification dispatch binding 上下文刷新回归提供带分发标识的最小通知。
/// </summary>
internal sealed record DispatcherNotificationContextRefreshNotification(string DispatchId) : INotification;
