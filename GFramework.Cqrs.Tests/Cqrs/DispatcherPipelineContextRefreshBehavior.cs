using System.Threading;
using GFramework.Cqrs.Abstractions.Cqrs;
using GFramework.Cqrs.Cqrs;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     记录缓存 executor 复用场景下每次分发注入到 behavior 的上下文与实例身份。
/// </summary>
internal sealed class DispatcherPipelineContextRefreshBehavior
    : CqrsContextAwareHandlerBase,
        IPipelineBehavior<DispatcherPipelineContextRefreshRequest, int>
{
    private readonly int _instanceId = DispatcherPipelineContextRefreshState.AllocateBehaviorInstanceId();

    /// <summary>
    ///     记录当前 behavior 实例实际收到的上下文，然后继续执行下游处理器。
    /// </summary>
    /// <param name="request">当前请求。</param>
    /// <param name="next">下一个处理阶段。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>下游处理结果。</returns>
    public async ValueTask<int> Handle(
        DispatcherPipelineContextRefreshRequest request,
        MessageHandlerDelegate<DispatcherPipelineContextRefreshRequest, int> next,
        CancellationToken cancellationToken)
    {
        DispatcherPipelineContextRefreshState.RecordBehavior(request.DispatchId, _instanceId, Context);
        return await next(request, cancellationToken).ConfigureAwait(false);
    }
}
