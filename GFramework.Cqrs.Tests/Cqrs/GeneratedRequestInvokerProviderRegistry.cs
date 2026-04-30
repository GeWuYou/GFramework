using System.Reflection;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Ioc;
using GFramework.Core.Logging;
using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     模拟同时提供 handler 注册与 request invoker 元数据的 generated registry。
/// </summary>
internal sealed class GeneratedRequestInvokerProviderRegistry :
    ICqrsHandlerRegistry,
    ICqrsRequestInvokerProvider,
    IEnumeratesCqrsRequestInvokerDescriptors
{
    private static readonly CqrsRequestInvokerDescriptor Descriptor = new(
        typeof(IRequestHandler<GeneratedRequestInvokerRequest, string>),
        typeof(GeneratedRequestInvokerProviderRegistry).GetMethod(
            nameof(InvokeGenerated),
            BindingFlags.NonPublic | BindingFlags.Static)!);

    private static readonly CqrsRequestInvokerDescriptorEntry DescriptorEntry = new(
        typeof(GeneratedRequestInvokerRequest),
        typeof(string),
        Descriptor);

    /// <summary>
    ///     将测试请求处理器注册到目标服务集合。
    /// </summary>
    /// <param name="services">承载处理器映射的服务集合。</param>
    /// <param name="logger">用于记录注册诊断的日志器。</param>
    public void Register(IServiceCollection services, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(logger);

        services.AddTransient(
            typeof(IRequestHandler<GeneratedRequestInvokerRequest, string>),
            typeof(GeneratedRequestInvokerRequestHandler));
        logger.Debug(
            $"Registered CQRS handler {typeof(GeneratedRequestInvokerRequestHandler).FullName} as {typeof(IRequestHandler<GeneratedRequestInvokerRequest, string>).FullName}.");
    }

    /// <summary>
    ///     尝试返回指定 request/response 类型对对应的 generated invoker 描述符。
    /// </summary>
    /// <param name="requestType">请求运行时类型。</param>
    /// <param name="responseType">响应运行时类型。</param>
    /// <param name="descriptor">命中时返回的描述符。</param>
    /// <returns>若类型对匹配当前测试请求则返回 <see langword="true" />。</returns>
    public bool TryGetDescriptor(
        Type requestType,
        Type responseType,
        out CqrsRequestInvokerDescriptor? descriptor)
    {
        if (requestType == typeof(GeneratedRequestInvokerRequest) && responseType == typeof(string))
        {
            descriptor = Descriptor;
            return true;
        }

        descriptor = null;
        return false;
    }

    /// <summary>
    ///     返回当前 registry 暴露的全部 generated request invoker 描述符。
    /// </summary>
    /// <returns>单条测试 request invoker 描述符条目。</returns>
    public IReadOnlyList<CqrsRequestInvokerDescriptorEntry> GetDescriptors()
    {
        return [DescriptorEntry];
    }

    /// <summary>
    ///     模拟 generated request invoker 直接执行后的返回值。
    /// </summary>
    /// <param name="handler">当前请求处理器实例。</param>
    /// <param name="request">当前测试请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>带有 generated 前缀的结果，便于断言 dispatcher 走了 provider 路径。</returns>
    private static ValueTask<string> InvokeGenerated(object handler, object request, CancellationToken cancellationToken)
    {
        _ = handler as IRequestHandler<GeneratedRequestInvokerRequest, string>
            ?? throw new InvalidOperationException("Generated invoker received an incompatible handler instance.");
        var typedRequest = (GeneratedRequestInvokerRequest)request;
        return ValueTask.FromResult($"generated:{typedRequest.Value}");
    }
}
