using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     用于验证 request 服务类型缓存的测试请求。
/// </summary>
internal sealed record DispatcherCacheRequest : IRequest<int>;
