using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     为 stream dispatch binding 上下文刷新回归提供带分发标识的最小流请求。
/// </summary>
internal sealed record DispatcherStreamContextRefreshRequest(string DispatchId) : IStreamRequest<int>;
