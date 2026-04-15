using GFramework.Core.Abstractions.Architectures;
using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Core.Abstractions.Cqrs;

/// <summary>
///     定义架构上下文使用的 CQRS runtime seam。
///     该抽象把请求分发、通知发布与流式处理从具体实现中解耦，
///     使 <see cref="IArchitectureContext" /> 不再直接依赖某个固定的 runtime 类型。
/// </summary>
public interface ICqrsRuntime
{
    /// <summary>
    ///     发送请求并返回响应。
    /// </summary>
    /// <typeparam name="TResponse">响应类型。</typeparam>
    /// <param name="context">当前架构上下文，用于上下文感知处理器注入与嵌套请求访问。</param>
    /// <param name="request">要分发的请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>请求响应。</returns>
    ValueTask<TResponse> SendAsync<TResponse>(
        IArchitectureContext context,
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     发布通知到所有已注册处理器。
    /// </summary>
    /// <typeparam name="TNotification">通知类型。</typeparam>
    /// <param name="context">当前架构上下文，用于上下文感知处理器注入。</param>
    /// <param name="notification">要发布的通知。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>表示通知分发完成的值任务。</returns>
    ValueTask PublishAsync<TNotification>(
        IArchitectureContext context,
        TNotification notification,
        CancellationToken cancellationToken = default)
        where TNotification : INotification;

    /// <summary>
    ///     创建流式请求的异步响应序列。
    /// </summary>
    /// <typeparam name="TResponse">流元素类型。</typeparam>
    /// <param name="context">当前架构上下文，用于上下文感知处理器注入。</param>
    /// <param name="request">流式请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>按需生成的异步响应序列。</returns>
    IAsyncEnumerable<TResponse> CreateStream<TResponse>(
        IArchitectureContext context,
        IStreamRequest<TResponse> request,
        CancellationToken cancellationToken = default);
}
