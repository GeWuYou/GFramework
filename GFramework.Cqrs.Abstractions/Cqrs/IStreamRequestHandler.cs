namespace GFramework.Cqrs.Abstractions.Cqrs;

/// <summary>
///     表示处理流式 CQRS 请求的处理器契约。
/// </summary>
/// <typeparam name="TRequest">流式请求类型。</typeparam>
/// <typeparam name="TResponse">流式响应元素类型。</typeparam>
public interface IStreamRequestHandler<in TRequest, out TResponse>
    where TRequest : IStreamRequest<TResponse>
{
    /// <summary>
    ///     处理流式请求并返回异步响应序列。
    /// </summary>
    /// <param name="request">要处理的请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>异步响应序列。</returns>
    IAsyncEnumerable<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}
