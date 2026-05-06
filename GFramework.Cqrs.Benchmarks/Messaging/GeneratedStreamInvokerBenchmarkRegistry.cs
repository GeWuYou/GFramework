// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using GFramework.Core.Abstractions.Logging;
using GFramework.Cqrs.Abstractions.Cqrs;
using Microsoft.Extensions.DependencyInjection;

namespace GFramework.Cqrs.Benchmarks.Messaging;

/// <summary>
///     为 benchmark 手写一个“生成后等价物” stream registry，用于驱动真实的 generated stream invoker provider 运行时接线路径。
/// </summary>
public sealed class GeneratedStreamInvokerBenchmarkRegistry :
    GFramework.Cqrs.ICqrsHandlerRegistry,
    GFramework.Cqrs.ICqrsStreamInvokerProvider,
    GFramework.Cqrs.IEnumeratesCqrsStreamInvokerDescriptors
{
    private static readonly GFramework.Cqrs.CqrsStreamInvokerDescriptor Descriptor =
        new(
            typeof(IStreamRequestHandler<
                StreamInvokerBenchmarks.GeneratedBenchmarkStreamRequest,
                StreamInvokerBenchmarks.GeneratedBenchmarkResponse>),
            typeof(GeneratedStreamInvokerBenchmarkRegistry).GetMethod(
                nameof(InvokeGeneratedStreamHandler),
                BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("Missing generated stream invoker benchmark method."));

    private static readonly IReadOnlyList<GFramework.Cqrs.CqrsStreamInvokerDescriptorEntry> Descriptors =
    [
        new GFramework.Cqrs.CqrsStreamInvokerDescriptorEntry(
            typeof(StreamInvokerBenchmarks.GeneratedBenchmarkStreamRequest),
            typeof(StreamInvokerBenchmarks.GeneratedBenchmarkResponse),
            Descriptor)
    ];

    /// <summary>
    ///     将 generated benchmark stream handler 注册到目标服务集合。
    /// </summary>
    public void Register(IServiceCollection services, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(logger);

        services.AddTransient(
            typeof(IStreamRequestHandler<
                StreamInvokerBenchmarks.GeneratedBenchmarkStreamRequest,
                StreamInvokerBenchmarks.GeneratedBenchmarkResponse>),
            typeof(StreamInvokerBenchmarks.GeneratedBenchmarkStreamHandler));
        logger.Debug("Registered generated stream invoker benchmark handler.");
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
    public bool TryGetDescriptor(
        Type requestType,
        Type responseType,
        out GFramework.Cqrs.CqrsStreamInvokerDescriptor? descriptor)
    {
        if (requestType == typeof(StreamInvokerBenchmarks.GeneratedBenchmarkStreamRequest) &&
            responseType == typeof(StreamInvokerBenchmarks.GeneratedBenchmarkResponse))
        {
            descriptor = Descriptor;
            return true;
        }

        descriptor = null;
        return false;
    }

    /// <summary>
    ///     模拟 generated stream invoker provider 产出的开放静态调用入口。
    /// </summary>
    public static object InvokeGeneratedStreamHandler(object handler, object request, CancellationToken cancellationToken)
    {
        var typedHandler = (IStreamRequestHandler<
            StreamInvokerBenchmarks.GeneratedBenchmarkStreamRequest,
            StreamInvokerBenchmarks.GeneratedBenchmarkResponse>)handler;
        var typedRequest = (StreamInvokerBenchmarks.GeneratedBenchmarkStreamRequest)request;
        return typedHandler.Handle(typedRequest, cancellationToken);
    }
}
