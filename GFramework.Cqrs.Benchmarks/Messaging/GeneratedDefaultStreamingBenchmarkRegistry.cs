// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using GFramework.Cqrs.Abstractions.Cqrs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GFramework.Cqrs.Benchmarks.Messaging;

/// <summary>
///     为默认 stream steady-state benchmark 提供 hand-written generated registry，
///     以便验证“默认 stream 宿主吸收 generated stream invoker provider”后的完整枚举收益。
/// </summary>
public sealed class GeneratedDefaultStreamingBenchmarkRegistry :
    GFramework.Cqrs.ICqrsHandlerRegistry,
    GFramework.Cqrs.ICqrsStreamInvokerProvider,
    GFramework.Cqrs.IEnumeratesCqrsStreamInvokerDescriptors
{
    private static readonly GFramework.Cqrs.CqrsStreamInvokerDescriptor Descriptor =
        new(
            typeof(IStreamRequestHandler<
                StreamingBenchmarks.BenchmarkStreamRequest,
                StreamingBenchmarks.BenchmarkResponse>),
            typeof(GeneratedDefaultStreamingBenchmarkRegistry).GetMethod(
                nameof(InvokeBenchmarkStreamHandler),
                BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("Missing generated default streaming benchmark method."));

    private static readonly IReadOnlyList<GFramework.Cqrs.CqrsStreamInvokerDescriptorEntry> Descriptors =
    [
        new GFramework.Cqrs.CqrsStreamInvokerDescriptorEntry(
            typeof(StreamingBenchmarks.BenchmarkStreamRequest),
            typeof(StreamingBenchmarks.BenchmarkResponse),
            Descriptor)
    ];

    /// <summary>
    ///     把默认 stream benchmark handler 注册为单例，保持与原先 steady-state 宿主一致的生命周期语义。
    /// </summary>
    public void Register(IServiceCollection services, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(logger);

        services.AddSingleton(
            typeof(IStreamRequestHandler<StreamingBenchmarks.BenchmarkStreamRequest, StreamingBenchmarks.BenchmarkResponse>),
            typeof(StreamingBenchmarks.BenchmarkStreamHandler));
        logger.LogDebug("Registered generated default streaming benchmark handler.");
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
        if (requestType == typeof(StreamingBenchmarks.BenchmarkStreamRequest) &&
            responseType == typeof(StreamingBenchmarks.BenchmarkResponse))
        {
            descriptor = Descriptor;
            return true;
        }

        descriptor = null;
        return false;
    }

    /// <summary>
    ///     模拟 generated stream invoker provider 为默认 stream benchmark 产出的开放静态调用入口。
    /// </summary>
    public static object InvokeBenchmarkStreamHandler(
        object handler,
        object request,
        CancellationToken cancellationToken)
    {
        var typedHandler = (IStreamRequestHandler<
            StreamingBenchmarks.BenchmarkStreamRequest,
            StreamingBenchmarks.BenchmarkResponse>)handler;
        var typedRequest = (StreamingBenchmarks.BenchmarkStreamRequest)request;
        return typedHandler.Handle(typedRequest, cancellationToken);
    }
}
