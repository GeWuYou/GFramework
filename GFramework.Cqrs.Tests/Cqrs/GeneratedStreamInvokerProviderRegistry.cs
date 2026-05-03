// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Ioc;
using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     模拟同时提供 handler 注册与 stream invoker 元数据的 generated registry。
/// </summary>
internal sealed class GeneratedStreamInvokerProviderRegistry :
    ICqrsHandlerRegistry,
    ICqrsStreamInvokerProvider,
    IEnumeratesCqrsStreamInvokerDescriptors
{
    private static readonly CqrsStreamInvokerDescriptor Descriptor = new(
        typeof(IStreamRequestHandler<GeneratedStreamInvokerRequest, int>),
        typeof(GeneratedStreamInvokerProviderRegistry).GetMethod(
            nameof(InvokeGenerated),
            BindingFlags.NonPublic | BindingFlags.Static)!);

    private static readonly CqrsStreamInvokerDescriptorEntry DescriptorEntry = new(
        typeof(GeneratedStreamInvokerRequest),
        typeof(int),
        Descriptor);

    /// <summary>
    ///     将测试流式请求处理器注册到目标服务集合。
    /// </summary>
    /// <param name="services">承载处理器映射的服务集合。</param>
    /// <param name="logger">用于记录注册诊断的日志器。</param>
    public void Register(IServiceCollection services, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(logger);

        services.AddTransient(
            typeof(IStreamRequestHandler<GeneratedStreamInvokerRequest, int>),
            typeof(GeneratedStreamInvokerRequestHandler));
        logger.Debug(
            $"Registered CQRS handler {typeof(GeneratedStreamInvokerRequestHandler).FullName} as {typeof(IStreamRequestHandler<GeneratedStreamInvokerRequest, int>).FullName}.");
    }

    /// <summary>
    ///     尝试返回指定 stream request/response 类型对对应的 generated invoker 描述符。
    /// </summary>
    /// <param name="requestType">流式请求运行时类型。</param>
    /// <param name="responseType">流式响应元素类型。</param>
    /// <param name="descriptor">命中时返回的描述符。</param>
    /// <returns>若类型对匹配当前测试流式请求则返回 <see langword="true" />。</returns>
    public bool TryGetDescriptor(
        Type requestType,
        Type responseType,
        out CqrsStreamInvokerDescriptor? descriptor)
    {
        if (requestType == typeof(GeneratedStreamInvokerRequest) && responseType == typeof(int))
        {
            descriptor = Descriptor;
            return true;
        }

        descriptor = null;
        return false;
    }

    /// <summary>
    ///     返回当前 registry 暴露的全部 generated stream invoker 描述符。
    /// </summary>
    /// <returns>单条测试 stream invoker 描述符条目。</returns>
    public IReadOnlyList<CqrsStreamInvokerDescriptorEntry> GetDescriptors()
    {
        return [DescriptorEntry];
    }

    /// <summary>
    ///     模拟 generated stream invoker 直接执行后的返回值。
    /// </summary>
    /// <param name="handler">当前流式请求处理器实例。</param>
    /// <param name="request">当前测试流式请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>带有 generated 语义的异步流，便于断言 dispatcher 走了 provider 路径。</returns>
    private static object InvokeGenerated(object handler, object request, CancellationToken cancellationToken)
    {
        _ = handler as IStreamRequestHandler<GeneratedStreamInvokerRequest, int>
            ?? throw new InvalidOperationException("Generated stream invoker received an incompatible handler instance.");
        var typedRequest = (GeneratedStreamInvokerRequest)request;
        return StreamResultsAsync(typedRequest.Start, cancellationToken);
    }

    /// <summary>
    ///     构造供测试断言使用的固定异步流结果。
    /// </summary>
    private static async IAsyncEnumerable<int> StreamResultsAsync(
        int start,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return start * 10;
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();
        yield return start * 10 + 1;
    }
}
