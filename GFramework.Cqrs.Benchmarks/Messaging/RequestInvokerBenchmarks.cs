// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Ioc;
using GFramework.Core.Logging;
using GFramework.Cqrs.Abstractions.Cqrs;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

[assembly: GFramework.Cqrs.CqrsHandlerRegistryAttribute(
    typeof(GFramework.Cqrs.Benchmarks.Messaging.GeneratedRequestInvokerBenchmarkRegistry))]

namespace GFramework.Cqrs.Benchmarks.Messaging;

/// <summary>
///     对比 request steady-state dispatch 在 direct handler、GFramework 反射路径、GFramework generated invoker 路径与 MediatR 之间的开销差异。
/// </summary>
[Config(typeof(Config))]
public class RequestInvokerBenchmarks
{
    private MicrosoftDiContainer _reflectionContainer = null!;
    private ICqrsRuntime _reflectionRuntime = null!;
    private MicrosoftDiContainer _generatedContainer = null!;
    private ICqrsRuntime _generatedRuntime = null!;
    private ServiceProvider _serviceProvider = null!;
    private IMediator _mediatr = null!;
    private ReflectionBenchmarkRequestHandler _baselineHandler = null!;
    private ReflectionBenchmarkRequest _reflectionRequest = null!;
    private GeneratedBenchmarkRequest _generatedRequest = null!;
    private MediatRBenchmarkRequest _mediatrRequest = null!;

    /// <summary>
    ///     配置 request invoker benchmark 的公共输出格式。
    /// </summary>
    private sealed class Config : ManualConfig
    {
        public Config()
        {
            AddJob(Job.Default);
            AddColumnProvider(DefaultColumnProviders.Instance);
            AddColumn(new CustomColumn("Scenario", static (_, _) => "RequestInvoker"));
            AddDiagnoser(MemoryDiagnoser.Default);
            WithOrderer(new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest, MethodOrderPolicy.Declared));
        }
    }

    /// <summary>
    ///     构建 reflection / generated / MediatR 三组 request dispatch 对照宿主。
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        LoggerFactoryResolver.Provider = new ConsoleLoggerFactoryProvider
        {
            MinLevel = LogLevel.Fatal
        };
        Fixture.Setup("RequestInvoker", handlerCount: 1, pipelineCount: 0);
        ClearDispatcherCaches();

        _baselineHandler = new ReflectionBenchmarkRequestHandler();
        _reflectionRequest = new ReflectionBenchmarkRequest(Guid.NewGuid());
        _generatedRequest = new GeneratedBenchmarkRequest(Guid.NewGuid());
        _mediatrRequest = new MediatRBenchmarkRequest(Guid.NewGuid());

        _reflectionContainer = new MicrosoftDiContainer();
        _reflectionContainer.RegisterTransient<GFramework.Cqrs.Abstractions.Cqrs.IRequestHandler<ReflectionBenchmarkRequest, ReflectionBenchmarkResponse>, ReflectionBenchmarkRequestHandler>();
        _reflectionRuntime = GFramework.Cqrs.CqrsRuntimeFactory.CreateRuntime(
            _reflectionContainer,
            LoggerFactoryResolver.Provider.CreateLogger(nameof(RequestInvokerBenchmarks) + ".Reflection"));

        _generatedContainer = new MicrosoftDiContainer();
        _generatedContainer.RegisterCqrsHandlersFromAssembly(typeof(RequestInvokerBenchmarks).Assembly);
        _generatedRuntime = GFramework.Cqrs.CqrsRuntimeFactory.CreateRuntime(
            _generatedContainer,
            LoggerFactoryResolver.Provider.CreateLogger(nameof(RequestInvokerBenchmarks) + ".Generated"));

        var services = new ServiceCollection();
        services.AddSingleton<MediatR.IRequestHandler<MediatRBenchmarkRequest, MediatRBenchmarkResponse>, MediatRBenchmarkRequestHandler>();
        services.AddMediatR(static options => options.RegisterServicesFromAssembly(typeof(RequestInvokerBenchmarks).Assembly));
        _serviceProvider = services.BuildServiceProvider();
        _mediatr = _serviceProvider.GetRequiredService<IMediator>();
    }

    /// <summary>
    ///     释放 MediatR 对照组使用的 DI 宿主，并清理静态 dispatcher 缓存。
    /// </summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        _serviceProvider.Dispose();
        ClearDispatcherCaches();
    }

    /// <summary>
    ///     直接调用最小 request handler，作为 dispatch 额外开销 baseline。
    /// </summary>
    [Benchmark(Baseline = true)]
    public ValueTask<ReflectionBenchmarkResponse> SendRequest_Baseline()
    {
        return _baselineHandler.Handle(_reflectionRequest, CancellationToken.None);
    }

    /// <summary>
    ///     通过 GFramework.CQRS 反射 request binding 路径发送 request。
    /// </summary>
    [Benchmark]
    public ValueTask<ReflectionBenchmarkResponse> SendRequest_GFrameworkReflection()
    {
        return _reflectionRuntime.SendAsync(BenchmarkContext.Instance, _reflectionRequest, CancellationToken.None);
    }

    /// <summary>
    ///     通过 generated request invoker provider 预热后的 GFramework.CQRS runtime 发送 request。
    /// </summary>
    [Benchmark]
    public ValueTask<GeneratedBenchmarkResponse> SendRequest_GFrameworkGenerated()
    {
        return _generatedRuntime.SendAsync(BenchmarkContext.Instance, _generatedRequest, CancellationToken.None);
    }

    /// <summary>
    ///     通过 MediatR 发送 request，作为外部对照。
    /// </summary>
    [Benchmark]
    public Task<MediatRBenchmarkResponse> SendRequest_MediatR()
    {
        return _mediatr.Send(_mediatrRequest, CancellationToken.None);
    }

    /// <summary>
    ///     清空 dispatcher 静态缓存，避免上一轮基准残留的 generated metadata 影响当前对照。
    /// </summary>
    private static void ClearDispatcherCaches()
    {
        ClearDispatcherCache("NotificationDispatchBindings");
        ClearDispatcherCache("RequestDispatchBindings");
        ClearDispatcherCache("StreamDispatchBindings");
        ClearDispatcherCache("GeneratedRequestInvokers");
        ClearDispatcherCache("GeneratedStreamInvokers");
    }

    /// <summary>
    ///     通过反射定位并清空 dispatcher 的指定缓存字段。
    /// </summary>
    /// <param name="fieldName">要清理的静态缓存字段名。</param>
    private static void ClearDispatcherCache(string fieldName)
    {
        var field = typeof(GFramework.Cqrs.CqrsRuntimeFactory).Assembly
            .GetType("GFramework.Cqrs.Internal.CqrsDispatcher", throwOnError: true)!
            .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException($"Missing dispatcher cache field {fieldName}.");
        var cache = field.GetValue(null)
                    ?? throw new InvalidOperationException($"Dispatcher cache field {fieldName} returned null.");
        var clearMethod = cache.GetType().GetMethod("Clear", BindingFlags.Public | BindingFlags.Instance)
                          ?? throw new InvalidOperationException(
                              $"Dispatcher cache field {fieldName} does not expose a Clear method.");
        _ = clearMethod.Invoke(cache, null);
    }

    /// <summary>
    ///     Reflection runtime request。
    /// </summary>
    /// <param name="Id">请求标识。</param>
    public sealed record ReflectionBenchmarkRequest(Guid Id) :
        GFramework.Cqrs.Abstractions.Cqrs.IRequest<ReflectionBenchmarkResponse>;

    /// <summary>
    ///     Reflection runtime response。
    /// </summary>
    /// <param name="Id">响应标识。</param>
    public sealed record ReflectionBenchmarkResponse(Guid Id);

    /// <summary>
    ///     Generated runtime request。
    /// </summary>
    /// <param name="Id">请求标识。</param>
    public sealed record GeneratedBenchmarkRequest(Guid Id) :
        GFramework.Cqrs.Abstractions.Cqrs.IRequest<GeneratedBenchmarkResponse>;

    /// <summary>
    ///     Generated runtime response。
    /// </summary>
    /// <param name="Id">响应标识。</param>
    public sealed record GeneratedBenchmarkResponse(Guid Id);

    /// <summary>
    ///     MediatR request。
    /// </summary>
    /// <param name="Id">请求标识。</param>
    public sealed record MediatRBenchmarkRequest(Guid Id) : MediatR.IRequest<MediatRBenchmarkResponse>;

    /// <summary>
    ///     MediatR response。
    /// </summary>
    /// <param name="Id">响应标识。</param>
    public sealed record MediatRBenchmarkResponse(Guid Id);

    /// <summary>
    ///     Reflection runtime 的最小 request handler。
    /// </summary>
    public sealed class ReflectionBenchmarkRequestHandler :
        GFramework.Cqrs.Abstractions.Cqrs.IRequestHandler<ReflectionBenchmarkRequest, ReflectionBenchmarkResponse>
    {
        /// <summary>
        ///     处理 reflection benchmark request。
        /// </summary>
        public ValueTask<ReflectionBenchmarkResponse> Handle(
            ReflectionBenchmarkRequest request,
            CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(new ReflectionBenchmarkResponse(request.Id));
        }
    }

    /// <summary>
    ///     Generated runtime 的最小 request handler。
    /// </summary>
    public sealed class GeneratedBenchmarkRequestHandler :
        GFramework.Cqrs.Abstractions.Cqrs.IRequestHandler<GeneratedBenchmarkRequest, GeneratedBenchmarkResponse>
    {
        /// <summary>
        ///     处理 generated benchmark request。
        /// </summary>
        public ValueTask<GeneratedBenchmarkResponse> Handle(
            GeneratedBenchmarkRequest request,
            CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(new GeneratedBenchmarkResponse(request.Id));
        }
    }

    /// <summary>
    ///     MediatR 对照组的最小 request handler。
    /// </summary>
    public sealed class MediatRBenchmarkRequestHandler :
        MediatR.IRequestHandler<MediatRBenchmarkRequest, MediatRBenchmarkResponse>
    {
        /// <summary>
        ///     处理 MediatR benchmark request。
        /// </summary>
        public Task<MediatRBenchmarkResponse> Handle(
            MediatRBenchmarkRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new MediatRBenchmarkResponse(request.Id));
        }
    }
}
