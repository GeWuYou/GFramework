// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using GFramework.Core.Abstractions.Logging;
using GFramework.Cqrs.Abstractions.Cqrs;
using Microsoft.Extensions.DependencyInjection;

namespace GFramework.Cqrs.Benchmarks.Messaging;

/// <summary>
///     为 benchmark 手写一个“生成后等价物” registry，用于驱动真实的 generated invoker provider 运行时接线路径。
/// </summary>
public sealed class GeneratedRequestInvokerBenchmarkRegistry :
    GFramework.Cqrs.ICqrsHandlerRegistry,
    GFramework.Cqrs.ICqrsRequestInvokerProvider,
    GFramework.Cqrs.IEnumeratesCqrsRequestInvokerDescriptors
{
    private static readonly GFramework.Cqrs.CqrsRequestInvokerDescriptor Descriptor =
        new(
            typeof(IRequestHandler<
                RequestInvokerBenchmarks.GeneratedBenchmarkRequest,
                RequestInvokerBenchmarks.GeneratedBenchmarkResponse>),
            typeof(GeneratedRequestInvokerBenchmarkRegistry).GetMethod(
                nameof(InvokeGeneratedRequestHandler),
                BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("Missing generated request invoker benchmark method."));

    private static readonly IReadOnlyList<GFramework.Cqrs.CqrsRequestInvokerDescriptorEntry> Descriptors =
    [
        new GFramework.Cqrs.CqrsRequestInvokerDescriptorEntry(
            typeof(RequestInvokerBenchmarks.GeneratedBenchmarkRequest),
            typeof(RequestInvokerBenchmarks.GeneratedBenchmarkResponse),
            Descriptor)
    ];

    /// <summary>
    ///     将 generated benchmark request handler 注册到目标服务集合。
    /// </summary>
    public void Register(IServiceCollection services, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(logger);

        services.AddTransient(
            typeof(IRequestHandler<
                RequestInvokerBenchmarks.GeneratedBenchmarkRequest,
                RequestInvokerBenchmarks.GeneratedBenchmarkResponse>),
            typeof(RequestInvokerBenchmarks.GeneratedBenchmarkRequestHandler));
        logger.Debug("Registered generated request invoker benchmark handler.");
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
        if (requestType == typeof(RequestInvokerBenchmarks.GeneratedBenchmarkRequest) &&
            responseType == typeof(RequestInvokerBenchmarks.GeneratedBenchmarkResponse))
        {
            descriptor = Descriptor;
            return true;
        }

        descriptor = null;
        return false;
    }

    /// <summary>
    ///     模拟 generated invoker provider 产出的开放静态调用入口。
    /// </summary>
    public static ValueTask<RequestInvokerBenchmarks.GeneratedBenchmarkResponse> InvokeGeneratedRequestHandler(
        object handler,
        object request,
        CancellationToken cancellationToken)
    {
        var typedHandler = (IRequestHandler<
            RequestInvokerBenchmarks.GeneratedBenchmarkRequest,
            RequestInvokerBenchmarks.GeneratedBenchmarkResponse>)handler;
        var typedRequest = (RequestInvokerBenchmarks.GeneratedBenchmarkRequest)request;
        return typedHandler.Handle(typedRequest, cancellationToken);
    }
}
