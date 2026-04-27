using System.Threading;
using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     处理 <see cref="DispatcherPipelineCacheRequest" />。
/// </summary>
internal sealed class DispatcherPipelineCacheRequestHandler : IRequestHandler<DispatcherPipelineCacheRequest, int>
{
    /// <summary>
    ///     返回固定结果，供 pipeline 缓存测试使用。
    /// </summary>
    /// <param name="request">当前请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>固定整数结果。</returns>
    public ValueTask<int> Handle(DispatcherPipelineCacheRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(2);
    }
}
