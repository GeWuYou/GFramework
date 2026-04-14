namespace GFramework.Core.Abstractions.Cqrs;

/// <summary>
///     表示 CQRS 请求在管道中继续向下执行的处理委托。
/// </summary>
/// <typeparam name="TRequest">请求类型。</typeparam>
/// <typeparam name="TResponse">响应类型。</typeparam>
/// <param name="message">当前请求消息。</param>
/// <param name="cancellationToken">取消令牌。</param>
/// <returns>请求响应。</returns>
public delegate ValueTask<TResponse> MessageHandlerDelegate<in TRequest, TResponse>(
    TRequest message,
    CancellationToken cancellationToken)
    where TRequest : IRequest<TResponse>;
