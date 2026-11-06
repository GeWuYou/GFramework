// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using GFramework.Cqrs.Abstractions.Cqrs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[assembly: GFramework.Cqrs.CqrsHandlerRegistryAttribute(
    typeof(GFramework.Cqrs.Benchmarks.Messaging.GeneratedStreamLifetimeBenchmarkRegistry))]

namespace GFramework.Cqrs.Benchmarks.Messaging;

/// <summary>
///     为 stream 生命周期矩阵 benchmark 提供 hand-written generated registry，
///     以便在默认 generated-provider 宿主路径上比较不同 handler 生命周期的完整枚举成本。
/// </summary>
public sealed class GeneratedStreamLifetimeBenchmarkRegistry :
    GFramework.Cqrs.ICqrsHandlerRegistry,
    GFramework.Cqrs.ICqrsStreamInvokerProvider,
    GFramework.Cqrs.IEnumeratesCqrsStreamInvokerDescriptors
{
    private static readonly GFramework.Cqrs.CqrsStreamInvokerDescriptor Descriptor =
        new(
            typeof(IStreamRequestHandler<
                StreamLifetimeBenchmarks.GeneratedBenchmarkStreamRequest,
                StreamLifetimeBenchmarks.GeneratedBenchmarkResponse>),
            typeof(GeneratedStreamLifetimeBenchmarkRegistry).GetMethod(
                nameof(InvokeBenchmarkStreamHandler),
                BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("Missing generated stream lifetime benchmark method."));

    private static readonly IReadOnlyList<GFramework.Cqrs.CqrsStreamInvokerDescriptorEntry> Descriptors =
    [
        new GFramework.Cqrs.CqrsStreamInvokerDescriptorEntry(
            typeof(StreamLifetimeBenchmarks.GeneratedBenchmarkStreamRequest),
            typeof(StreamLifetimeBenchmarks.GeneratedBenchmarkResponse),
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

        logger.LogDebug("Registered generated stream lifetime benchmark descriptors.");
    }

    /// <summary>
    ///     返回当前 provider 暴露的全部 generated stream invoker 描述符。
    /// </summary>
    public IReadOnlyList<GFramework.Cqrs.CqrsStreamInvokerDescriptorEntry> GetDescriptors()
    {
        return Descriptors;
    }

    /// <summary>
    ///     为目标流式请求/响应类型对返回 generated stream invoker 描述符。
    /// </summary>
    /// <param name="requestType">待匹配的请求类型。</param>
    /// <param name="responseType">待匹配的响应类型。</param>
    /// <param name="descriptor">命中时返回的 generated descriptor。</param>
    /// <returns>命中当前 benchmark 的请求/响应类型对时返回 <see langword="true" />。</returns>
    public bool TryGetDescriptor(
        Type requestType,
        Type responseType,
        out GFramework.Cqrs.CqrsStreamInvokerDescriptor? descriptor)
    {
        if (requestType == typeof(StreamLifetimeBenchmarks.GeneratedBenchmarkStreamRequest) &&
            responseType == typeof(StreamLifetimeBenchmarks.GeneratedBenchmarkResponse))
        {
            descriptor = Descriptor;
            return true;
        }

        descriptor = null;
        return false;
    }

    /// <summary>
    ///     模拟 generated stream invoker provider 为生命周期矩阵 benchmark 产出的开放静态调用入口。
    /// </summary>
    /// <param name="handler">当前请求对应的 handler 实例。</param>
    /// <param name="request">待分发的流式请求。</param>
    /// <param name="cancellationToken">调用方传入的取消令牌。</param>
    /// <returns>交给目标 stream handler 处理后的异步枚举。</returns>
    public static object InvokeBenchmarkStreamHandler(
        object handler,
        object request,
        CancellationToken cancellationToken)
    {
        var typedHandler = (IStreamRequestHandler<
            StreamLifetimeBenchmarks.GeneratedBenchmarkStreamRequest,
            StreamLifetimeBenchmarks.GeneratedBenchmarkResponse>)handler;
        var typedRequest = (StreamLifetimeBenchmarks.GeneratedBenchmarkStreamRequest)request;
        return typedHandler.Handle(typedRequest, cancellationToken);
    }
}
