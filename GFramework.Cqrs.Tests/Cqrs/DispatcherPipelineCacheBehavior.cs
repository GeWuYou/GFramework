using System.Threading;
using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     为 <see cref="DispatcherPipelineCacheRequest" /> 提供最小 pipeline 行为，
///     用于命中 dispatcher 的 pipeline invoker 缓存分支。
/// </summary>
internal sealed class DispatcherPipelineCacheBehavior : IPipelineBehavior<DispatcherPipelineCacheRequest, int>
{
    /// <summary>
    ///     直接转发到下一个处理器。
    /// </summary>
    /// <param name="request">当前请求。</param>
    /// <param name="next">下一个处理器委托。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>下游处理器结果。</returns>
    public ValueTask<int> Handle(
        DispatcherPipelineCacheRequest request,
        MessageHandlerDelegate<DispatcherPipelineCacheRequest, int> next,
        CancellationToken cancellationToken)
    {
        return next(request, cancellationToken);
    }
}
