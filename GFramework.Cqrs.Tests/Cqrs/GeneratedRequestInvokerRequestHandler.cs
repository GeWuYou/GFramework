using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     供 generated request invoker provider 测试使用的请求处理器。
/// </summary>
internal sealed class GeneratedRequestInvokerRequestHandler : IRequestHandler<GeneratedRequestInvokerRequest, string>
{
    /// <summary>
    ///     返回带有运行时处理器前缀的结果，便于和 generated invoker 自定义结果区分。
    /// </summary>
    /// <param name="request">当前测试请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>运行时处理器生成的响应字符串。</returns>
    public ValueTask<string> Handle(GeneratedRequestInvokerRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ValueTask.FromResult($"runtime:{request.Value}");
    }
}
