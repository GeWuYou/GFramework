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
    typeof(GFramework.Cqrs.Benchmarks.Messaging.GeneratedDefaultRequestBenchmarkRegistry))]

namespace GFramework.Cqrs.Benchmarks.Messaging;

/// <summary>
///     为默认 request steady-state benchmark 提供 hand-written generated registry，
///     以便验证“默认宿主吸收 generated request invoker provider”后的热路径收益。
/// </summary>
public sealed class GeneratedDefaultRequestBenchmarkRegistry :
    GFramework.Cqrs.ICqrsHandlerRegistry,
    GFramework.Cqrs.ICqrsRequestInvokerProvider,
    GFramework.Cqrs.IEnumeratesCqrsRequestInvokerDescriptors
{
    private static readonly GFramework.Cqrs.CqrsRequestInvokerDescriptor Descriptor =
        new(
            typeof(IRequestHandler<
                RequestBenchmarks.BenchmarkRequest,
                RequestBenchmarks.BenchmarkResponse>),
            typeof(GeneratedDefaultRequestBenchmarkRegistry).GetMethod(
                nameof(InvokeBenchmarkRequestHandler),
                BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("Missing generated default request benchmark method."));

    private static readonly IReadOnlyList<GFramework.Cqrs.CqrsRequestInvokerDescriptorEntry> Descriptors =
    [
        new GFramework.Cqrs.CqrsRequestInvokerDescriptorEntry(
            typeof(RequestBenchmarks.BenchmarkRequest),
            typeof(RequestBenchmarks.BenchmarkResponse),
            Descriptor),
    ];

    /// <summary>
    ///     把默认 request benchmark handler 注册为单例，保持与原先 steady-state 宿主一致的生命周期语义。
    /// </summary>
    public void Register(IServiceCollection services, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(logger);

        services.AddSingleton<IRequestHandler<RequestBenchmarks.BenchmarkRequest, RequestBenchmarks.BenchmarkResponse>, RequestBenchmarks.BenchmarkRequestHandler>();
        logger.LogDebug("Registered generated default request benchmark handler.");
    }

    /// <summary>
    ///     返回当前 provider 暴露的全部 generated request invoker 描述符。
    /// </summary>
    public IReadOnlyList<GFramework.Cqrs.CqrsRequestInvokerDescriptorEntry> GetDescriptors()
    {
        return Descriptors;
    }

    /// <summary>
    ///     为目标请求/响应类型对返回 generated request invoker 描述符。
    /// </summary>
    public bool TryGetDescriptor(
        Type requestType,
        Type responseType,
        out GFramework.Cqrs.CqrsRequestInvokerDescriptor? descriptor)
    {
        if (requestType == typeof(RequestBenchmarks.BenchmarkRequest) &&
            responseType == typeof(RequestBenchmarks.BenchmarkResponse))
        {
            descriptor = Descriptor;
            return true;
        }

        descriptor = null;
        return false;
    }

    /// <summary>
    ///     模拟 generated invoker provider 为默认 request benchmark 产出的开放静态调用入口。
    /// </summary>
    public static ValueTask<RequestBenchmarks.BenchmarkResponse> InvokeBenchmarkRequestHandler(
        object handler,
        object request,
        CancellationToken cancellationToken)
    {
        var typedHandler = (IRequestHandler<
            RequestBenchmarks.BenchmarkRequest,
            RequestBenchmarks.BenchmarkResponse>)handler;
        var typedRequest = (RequestBenchmarks.BenchmarkRequest)request;
        return typedHandler.Handle(typedRequest, cancellationToken);
    }
}
