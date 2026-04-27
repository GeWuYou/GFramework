using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     用于验证 notification 服务类型缓存的测试通知。
/// </summary>
internal sealed record DispatcherCacheNotification : INotification;
