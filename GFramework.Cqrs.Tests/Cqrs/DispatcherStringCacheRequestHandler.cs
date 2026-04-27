using System.Threading;
using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     处理 <see cref="DispatcherStringCacheRequest" />。
/// </summary>
internal sealed class DispatcherStringCacheRequestHandler : IRequestHandler<DispatcherStringCacheRequest, string>
{
    /// <summary>
    ///     返回固定字符串，供按响应类型缓存测试验证 string 路径。
    /// </summary>
    /// <param name="request">当前请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>固定字符串结果。</returns>
    public ValueTask<string> Handle(DispatcherStringCacheRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult("dispatcher-cache");
    }
}
