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
    typeof(GFramework.Cqrs.Benchmarks.Messaging.GeneratedRequestPipelineBenchmarkRegistry))]

namespace GFramework.Cqrs.Benchmarks.Messaging;

/// <summary>
///     为 request pipeline benchmark 提供 handwritten generated registry，
///     让默认 pipeline 宿主也能走真实的 generated request invoker provider 接线路径。
/// </summary>
public sealed class GeneratedRequestPipelineBenchmarkRegistry :
    GFramework.Cqrs.ICqrsHandlerRegistry,
    GFramework.Cqrs.ICqrsRequestInvokerProvider,
    GFramework.Cqrs.IEnumeratesCqrsRequestInvokerDescriptors
{
    private static readonly GFramework.Cqrs.CqrsRequestInvokerDescriptor Descriptor =
        new(
            typeof(IRequestHandler<
                RequestPipelineBenchmarks.BenchmarkRequest,
                RequestPipelineBenchmarks.BenchmarkResponse>),
            typeof(GeneratedRequestPipelineBenchmarkRegistry).GetMethod(
                nameof(InvokeBenchmarkRequestHandler),
                BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("Missing generated request pipeline benchmark method."));

    private static readonly IReadOnlyList<GFramework.Cqrs.CqrsRequestInvokerDescriptorEntry> Descriptors =
    [
        new GFramework.Cqrs.CqrsRequestInvokerDescriptorEntry(
            typeof(RequestPipelineBenchmarks.BenchmarkRequest),
            typeof(RequestPipelineBenchmarks.BenchmarkResponse),
            Descriptor),
    ];

    /// <summary>
    ///     将 request pipeline benchmark handler 注册为单例，保持与当前矩阵宿主一致的生命周期语义。
    /// </summary>
    public void Register(IServiceCollection services, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(logger);

        services.AddSingleton<IRequestHandler<RequestPipelineBenchmarks.BenchmarkRequest, RequestPipelineBenchmarks.BenchmarkResponse>, RequestPipelineBenchmarks.BenchmarkRequestHandler>();
        logger.LogDebug("Registered generated request pipeline benchmark handler.");
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
        if (requestType == typeof(RequestPipelineBenchmarks.BenchmarkRequest) &&
            responseType == typeof(RequestPipelineBenchmarks.BenchmarkResponse))
        {
            descriptor = Descriptor;
            return true;
        }

        descriptor = null;
        return false;
    }

    /// <summary>
    ///     模拟 generated invoker provider 为 request pipeline benchmark 产出的开放静态调用入口。
    /// </summary>
    public static ValueTask<RequestPipelineBenchmarks.BenchmarkResponse> InvokeBenchmarkRequestHandler(
        object handler,
        object request,
        CancellationToken cancellationToken)
    {
        var typedHandler = (IRequestHandler<
            RequestPipelineBenchmarks.BenchmarkRequest,
            RequestPipelineBenchmarks.BenchmarkResponse>)handler;
        var typedRequest = (RequestPipelineBenchmarks.BenchmarkRequest)request;
        return typedHandler.Handle(typedRequest, cancellationToken);
    }
}
