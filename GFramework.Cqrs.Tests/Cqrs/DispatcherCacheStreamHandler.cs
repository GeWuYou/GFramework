using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     处理 <see cref="DispatcherCacheStreamRequest" />。
/// </summary>
internal sealed class DispatcherCacheStreamHandler : IStreamRequestHandler<DispatcherCacheStreamRequest, int>
{
    /// <summary>
    ///     返回一个最小流，供缓存测试命中 stream 分发路径。
    /// </summary>
    /// <param name="request">当前流请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>包含一个固定元素的异步流。</returns>
    public async IAsyncEnumerable<int> Handle(
        DispatcherCacheStreamRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return 1;
        await Task.CompletedTask.ConfigureAwait(false);
    }
}
