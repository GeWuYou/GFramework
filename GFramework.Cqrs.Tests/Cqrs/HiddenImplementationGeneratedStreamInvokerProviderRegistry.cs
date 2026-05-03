// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Ioc;
using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     模拟 generated registry 在实现类型隐藏、但 stream handler interface 可见时，仍提供 stream invoker 元数据。
/// </summary>
internal sealed class HiddenImplementationGeneratedStreamInvokerProviderRegistry :
    ICqrsHandlerRegistry,
    ICqrsStreamInvokerProvider,
    IEnumeratesCqrsStreamInvokerDescriptors
{
    private static readonly Type HandlerContractType =
        typeof(IStreamRequestHandler<HiddenImplementationStreamInvokerContainer.VisibleStreamRequest, int>);

    private static readonly CqrsStreamInvokerDescriptor Descriptor = new(
        HandlerContractType,
        typeof(HiddenImplementationGeneratedStreamInvokerProviderRegistry).GetMethod(
            nameof(InvokeGenerated),
            BindingFlags.NonPublic | BindingFlags.Static)!);

    private static readonly CqrsStreamInvokerDescriptorEntry DescriptorEntry = new(
        typeof(HiddenImplementationStreamInvokerContainer.VisibleStreamRequest),
        typeof(int),
        Descriptor);

    /// <summary>
    ///     通过可见 stream handler interface 把隐藏实现类型注册进目标服务集合，模拟 generator 的 reflected-implementation 路径。
    /// </summary>
    /// <param name="services">承载处理器映射的服务集合。</param>
    /// <param name="logger">用于记录注册诊断的日志器。</param>
    /// <exception cref="ArgumentNullException">
    ///     当 <paramref name="services" /> 或 <paramref name="logger" /> 为 <see langword="null" /> 时抛出。
    /// </exception>
    public void Register(IServiceCollection services, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(logger);

        var implementationType = HiddenImplementationStreamInvokerContainer.HiddenHandlerType;
        services.AddTransient(HandlerContractType, implementationType);
        logger.Debug(
            $"Registered CQRS handler {implementationType.FullName} as {HandlerContractType.FullName}.");
    }

    /// <summary>
    ///     尝试返回指定 stream request/response 类型对对应的 generated invoker 描述符。
    /// </summary>
    /// <param name="requestType">流式请求运行时类型。</param>
    /// <param name="responseType">流式响应元素类型。</param>
    /// <param name="descriptor">命中时返回的描述符。</param>
    /// <returns>若类型对匹配当前测试流式请求则返回 <see langword="true" />。</returns>
    /// <exception cref="ArgumentNullException">
    ///     当 <paramref name="requestType" /> 或 <paramref name="responseType" /> 为 <see langword="null" /> 时抛出。
    /// </exception>
    public bool TryGetDescriptor(
        Type requestType,
        Type responseType,
        out CqrsStreamInvokerDescriptor? descriptor)
    {
        ArgumentNullException.ThrowIfNull(requestType);
        ArgumentNullException.ThrowIfNull(responseType);

        if (requestType == typeof(HiddenImplementationStreamInvokerContainer.VisibleStreamRequest)
            && responseType == typeof(int))
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
    /// <returns>单条 hidden implementation stream invoker 描述符条目。</returns>
    public IReadOnlyList<CqrsStreamInvokerDescriptorEntry> GetDescriptors()
    {
        return [DescriptorEntry];
    }

    /// <summary>
    ///     模拟 generated stream invoker 在隐藏实现类型场景下直接执行后的返回值。
    /// </summary>
    /// <param name="handler">当前流式请求处理器实例。</param>
    /// <param name="request">当前测试流式请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>带有 hidden generated 语义的异步流，便于断言 dispatcher 命中了 generated provider 路径。</returns>
    private static object InvokeGenerated(object handler, object request, CancellationToken cancellationToken)
    {
        _ = handler as IStreamRequestHandler<HiddenImplementationStreamInvokerContainer.VisibleStreamRequest, int>
            ?? throw new InvalidOperationException("Generated stream invoker received an incompatible hidden handler instance.");
        var typedRequest = (HiddenImplementationStreamInvokerContainer.VisibleStreamRequest)request;
        return StreamResultsAsync(typedRequest.Start, cancellationToken);
    }

    /// <summary>
    ///     构造供测试断言使用的固定异步流结果。
    /// </summary>
    private static async IAsyncEnumerable<int> StreamResultsAsync(
        int start,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return start * 100;
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();
        yield return start * 100 + 1;
    }
}
