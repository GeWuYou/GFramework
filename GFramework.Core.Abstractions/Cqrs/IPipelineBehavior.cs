namespace GFramework.Core.Abstractions.Cqrs;

/// <summary>
///     定义 CQRS 请求处理前后的管道行为。
/// </summary>
/// <typeparam name="TRequest">请求类型。</typeparam>
/// <typeparam name="TResponse">响应类型。</typeparam>
public interface IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    ///     处理当前请求，并决定是否继续调用后续行为或最终处理器。
    /// </summary>
    /// <param name="message">当前请求消息。</param>
    /// <param name="next">下一个处理委托。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>请求响应。</returns>
    ValueTask<TResponse> Handle(
        TRequest message,
        MessageHandlerDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken);
}
