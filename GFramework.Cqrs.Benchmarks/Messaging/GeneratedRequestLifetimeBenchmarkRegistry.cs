// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using GFramework.Cqrs.Abstractions.Cqrs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[assembly: GFramework.Cqrs.CqrsHandlerRegistryAttribute(
    typeof(GFramework.Cqrs.Benchmarks.Messaging.GeneratedRequestLifetimeBenchmarkRegistry))]

namespace GFramework.Cqrs.Benchmarks.Messaging;

/// <summary>
///     为 request 生命周期矩阵 benchmark 提供 hand-written generated registry，
///     以便在默认 generated-provider 宿主路径上比较不同 handler 生命周期的 dispatch 成本。
/// </summary>
public sealed class GeneratedRequestLifetimeBenchmarkRegistry :
    GFramework.Cqrs.ICqrsHandlerRegistry,
    GFramework.Cqrs.ICqrsRequestInvokerProvider,
    GFramework.Cqrs.IEnumeratesCqrsRequestInvokerDescriptors
{
    private static readonly GFramework.Cqrs.CqrsRequestInvokerDescriptor Descriptor =
        new(
            typeof(IRequestHandler<
                RequestLifetimeBenchmarks.BenchmarkRequest,
                RequestLifetimeBenchmarks.BenchmarkResponse>),
            typeof(GeneratedRequestLifetimeBenchmarkRegistry).GetMethod(
                nameof(InvokeBenchmarkRequestHandler),
                BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("Missing generated request lifetime benchmark method."));

    private static readonly IReadOnlyList<GFramework.Cqrs.CqrsRequestInvokerDescriptorEntry> Descriptors =
    [
        new GFramework.Cqrs.CqrsRequestInvokerDescriptorEntry(
            typeof(RequestLifetimeBenchmarks.BenchmarkRequest),
            typeof(RequestLifetimeBenchmarks.BenchmarkResponse),
            Descriptor)
    ];

    /// <summary>
    ///     参与程序集注册入口，但不在这里直接写入 handler 生命周期。
    /// </summary>
    /// <param name="services">当前 generated registry 拥有的服务集合。</param>
    /// <param name="logger">用于记录 generated registry 注册行为的日志器。</param>
    /// <remarks>
    ///     生命周期矩阵需要让 benchmark 主体显式控制 `Singleton / Transient` 变量。
    ///     因此 registry 只负责暴露 generated descriptor，不在这里抢先注册 handler，避免把默认单例注册混入比较结果。
    /// </remarks>
    public void Register(IServiceCollection services, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(logger);

        logger.LogDebug("Registered generated request lifetime benchmark descriptors.");
    }

    /// <summary>
    ///     返回当前 provider 暴露的全部 generated request invoker 描述符。
    /// </summary>
    /// <returns>当前 benchmark 需要的 request invoker 描述符集合。</returns>
    public IReadOnlyList<GFramework.Cqrs.CqrsRequestInvokerDescriptorEntry> GetDescriptors()
    {
        return Descriptors;
    }

    /// <summary>
    ///     为目标请求/响应类型对返回 generated request invoker 描述符。
    /// </summary>
    /// <param name="requestType">待匹配的请求类型。</param>
    /// <param name="responseType">待匹配的响应类型。</param>
    /// <param name="descriptor">命中时返回的 generated descriptor。</param>
    /// <returns>命中当前 benchmark 的请求/响应类型对时返回 <see langword="true" />。</returns>
    public bool TryGetDescriptor(
        Type requestType,
        Type responseType,
        out GFramework.Cqrs.CqrsRequestInvokerDescriptor? descriptor)
    {
        if (requestType == typeof(RequestLifetimeBenchmarks.BenchmarkRequest) &&
            responseType == typeof(RequestLifetimeBenchmarks.BenchmarkResponse))
        {
            descriptor = Descriptor;
            return true;
        }

        descriptor = null;
        return false;
    }

    /// <summary>
    ///     模拟 generated request invoker provider 为生命周期矩阵 benchmark 产出的开放静态调用入口。
    /// </summary>
    /// <param name="handler">当前请求对应的 handler 实例。</param>
    /// <param name="request">待分发的 request。</param>
    /// <param name="cancellationToken">调用方传入的取消令牌。</param>
    /// <returns>交给目标 request handler 处理后的响应任务。</returns>
    public static ValueTask<RequestLifetimeBenchmarks.BenchmarkResponse> InvokeBenchmarkRequestHandler(
        object handler,
        object request,
        CancellationToken cancellationToken)
    {
        var typedHandler = (IRequestHandler<
            RequestLifetimeBenchmarks.BenchmarkRequest,
            RequestLifetimeBenchmarks.BenchmarkResponse>)handler;
        var typedRequest = (RequestLifetimeBenchmarks.BenchmarkRequest)request;
        return typedHandler.Handle(typedRequest, cancellationToken);
    }
}
