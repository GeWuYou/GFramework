namespace GFramework.Core.Abstractions.Cqrs;

/// <summary>
///     表示 CQRS 请求在管道中继续向下执行的处理委托。
/// </summary>
/// <remarks>
///     <para>管道行为可以通过不调用该委托来短路请求处理。</para>
///     <para>除显式实现重试等高级语义外，行为通常应最多调用一次该委托，以维持单次请求分发的确定性。</para>
///     <para>调用方应传递当前收到的 <paramref name="cancellationToken" />，确保取消信号沿整条管道一致传播。</para>
/// </remarks>
/// <typeparam name="TRequest">请求类型。</typeparam>
/// <typeparam name="TResponse">响应类型。</typeparam>
/// <param name="message">当前请求消息。</param>
/// <param name="cancellationToken">取消令牌。</param>
/// <returns>请求响应。</returns>
public delegate ValueTask<TResponse> MessageHandlerDelegate<in TRequest, TResponse>(
    TRequest message,
    CancellationToken cancellationToken)
    where TRequest : IRequest<TResponse>;
