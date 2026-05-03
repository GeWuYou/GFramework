// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Ioc;
using GFramework.Core.Logging;
using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     模拟 generated registry 在实现类型隐藏、但 request handler interface 可见时，仍提供 request invoker 元数据。
/// </summary>
internal sealed class HiddenImplementationGeneratedRequestInvokerProviderRegistry :
    ICqrsHandlerRegistry,
    ICqrsRequestInvokerProvider,
    IEnumeratesCqrsRequestInvokerDescriptors
{
    private static readonly Type HandlerContractType =
        typeof(IRequestHandler<HiddenImplementationRequestInvokerContainer.VisibleRequest, string>);

    private static readonly CqrsRequestInvokerDescriptor Descriptor = new(
        HandlerContractType,
        typeof(HiddenImplementationGeneratedRequestInvokerProviderRegistry).GetMethod(
            nameof(InvokeGenerated),
            BindingFlags.NonPublic | BindingFlags.Static)!);

    private static readonly CqrsRequestInvokerDescriptorEntry DescriptorEntry = new(
        typeof(HiddenImplementationRequestInvokerContainer.VisibleRequest),
        typeof(string),
        Descriptor);

    /// <summary>
    ///     通过可见 handler interface 把隐藏实现类型注册进目标服务集合，模拟 generator 的 reflected-implementation 路径。
    /// </summary>
    /// <param name="services">承载处理器映射的服务集合。</param>
    /// <param name="logger">用于记录注册诊断的日志器。</param>
    public void Register(IServiceCollection services, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(logger);

        var implementationType = HiddenImplementationRequestInvokerContainer.HiddenHandlerType;
        services.AddTransient(HandlerContractType, implementationType);
        logger.Debug(
            $"Registered CQRS handler {implementationType.FullName} as {HandlerContractType.FullName}.");
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
        if (requestType == typeof(HiddenImplementationRequestInvokerContainer.VisibleRequest)
            && responseType == typeof(string))
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
    /// <returns>单条 hidden implementation request invoker 描述符条目。</returns>
    public IReadOnlyList<CqrsRequestInvokerDescriptorEntry> GetDescriptors()
    {
        return [DescriptorEntry];
    }

    /// <summary>
    ///     模拟 generated request invoker 在隐藏实现类型场景下直接执行后的返回值。
    /// </summary>
    /// <param name="handler">当前请求处理器实例。</param>
    /// <param name="request">当前测试请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>带有 hidden generated 前缀的结果，便于断言 dispatcher 命中了 generated provider 路径。</returns>
    private static ValueTask<string> InvokeGenerated(object handler, object request, CancellationToken cancellationToken)
    {
        _ = handler as IRequestHandler<HiddenImplementationRequestInvokerContainer.VisibleRequest, string>
            ?? throw new InvalidOperationException("Generated invoker received an incompatible hidden handler instance.");
        var typedRequest = (HiddenImplementationRequestInvokerContainer.VisibleRequest)request;
        return ValueTask.FromResult($"generated-hidden:{typedRequest.Value}");
    }
}
