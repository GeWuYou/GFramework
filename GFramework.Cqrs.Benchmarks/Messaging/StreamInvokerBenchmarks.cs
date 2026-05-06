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
    typeof(GFramework.Cqrs.Benchmarks.Messaging.GeneratedStreamInvokerBenchmarkRegistry))]

namespace GFramework.Cqrs.Benchmarks.Messaging;

/// <summary>
///     对比 stream 完整枚举在 direct handler、GFramework 反射路径、GFramework generated invoker 路径与 MediatR 之间的开销差异。
/// </summary>
[Config(typeof(Config))]
public class StreamInvokerBenchmarks
{
    private MicrosoftDiContainer _reflectionContainer = null!;
    private ICqrsRuntime _reflectionRuntime = null!;
    private MicrosoftDiContainer _generatedContainer = null!;
    private ICqrsRuntime _generatedRuntime = null!;
    private ServiceProvider _serviceProvider = null!;
    private IMediator _mediatr = null!;
    private ReflectionBenchmarkStreamHandler _baselineHandler = null!;
    private ReflectionBenchmarkStreamRequest _reflectionRequest = null!;
    private GeneratedBenchmarkStreamRequest _generatedRequest = null!;
    private MediatRBenchmarkStreamRequest _mediatrRequest = null!;

    /// <summary>
    ///     配置 stream invoker benchmark 的公共输出格式。
    /// </summary>
    private sealed class Config : ManualConfig
    {
        public Config()
        {
            AddJob(Job.Default);
            AddColumnProvider(DefaultColumnProviders.Instance);
            AddColumn(new CustomColumn("Scenario", static (_, _) => "StreamInvoker"));
            AddDiagnoser(MemoryDiagnoser.Default);
            WithOrderer(new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest, MethodOrderPolicy.Declared));
        }
    }

    /// <summary>
    ///     构建 reflection / generated / MediatR 三组 stream dispatch 对照宿主。
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        LoggerFactoryResolver.Provider = new ConsoleLoggerFactoryProvider
        {
            MinLevel = LogLevel.Fatal
        };
        Fixture.Setup("StreamInvoker", handlerCount: 1, pipelineCount: 0);
        ClearDispatcherCaches();

        _baselineHandler = new ReflectionBenchmarkStreamHandler();
        _reflectionRequest = new ReflectionBenchmarkStreamRequest(Guid.NewGuid(), 3);
        _generatedRequest = new GeneratedBenchmarkStreamRequest(Guid.NewGuid(), 3);
        _mediatrRequest = new MediatRBenchmarkStreamRequest(Guid.NewGuid(), 3);

        _reflectionContainer = new MicrosoftDiContainer();
        _reflectionContainer.RegisterTransient<GFramework.Cqrs.Abstractions.Cqrs.IStreamRequestHandler<ReflectionBenchmarkStreamRequest, ReflectionBenchmarkResponse>, ReflectionBenchmarkStreamHandler>();
        _reflectionRuntime = GFramework.Cqrs.CqrsRuntimeFactory.CreateRuntime(
            _reflectionContainer,
            LoggerFactoryResolver.Provider.CreateLogger(nameof(StreamInvokerBenchmarks) + ".Reflection"));

        _generatedContainer = new MicrosoftDiContainer();
        _generatedContainer.RegisterCqrsHandlersFromAssembly(typeof(StreamInvokerBenchmarks).Assembly);
        _generatedRuntime = GFramework.Cqrs.CqrsRuntimeFactory.CreateRuntime(
            _generatedContainer,
            LoggerFactoryResolver.Provider.CreateLogger(nameof(StreamInvokerBenchmarks) + ".Generated"));

        var services = new ServiceCollection();
        services.AddSingleton<MediatR.IStreamRequestHandler<MediatRBenchmarkStreamRequest, MediatRBenchmarkResponse>, MediatRBenchmarkStreamHandler>();
        services.AddMediatR(static options => options.RegisterServicesFromAssembly(typeof(StreamInvokerBenchmarks).Assembly));
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
    ///     直接调用最小 stream handler 并完整枚举，作为 dispatch 额外开销 baseline。
    /// </summary>
    [Benchmark(Baseline = true)]
    public async ValueTask Stream_Baseline()
    {
        await foreach (var response in _baselineHandler.Handle(_reflectionRequest, CancellationToken.None).ConfigureAwait(false))
        {
            _ = response;
        }
    }

    /// <summary>
    ///     通过 GFramework.CQRS 反射 stream binding 路径创建并完整枚举 stream。
    /// </summary>
    [Benchmark]
    public async ValueTask Stream_GFrameworkReflection()
    {
        await foreach (var response in _reflectionRuntime.CreateStream(BenchmarkContext.Instance, _reflectionRequest, CancellationToken.None)
                           .ConfigureAwait(false))
        {
            _ = response;
        }
    }

    /// <summary>
    ///     通过 generated stream invoker provider 预热后的 GFramework.CQRS runtime 创建并完整枚举 stream。
    /// </summary>
    [Benchmark]
    public async ValueTask Stream_GFrameworkGenerated()
    {
        await foreach (var response in _generatedRuntime.CreateStream(BenchmarkContext.Instance, _generatedRequest, CancellationToken.None)
                           .ConfigureAwait(false))
        {
            _ = response;
        }
    }

    /// <summary>
    ///     通过 MediatR 创建并完整枚举 stream，作为外部对照。
    /// </summary>
    [Benchmark]
    public async ValueTask Stream_MediatR()
    {
        await foreach (var response in _mediatr.CreateStream(_mediatrRequest, CancellationToken.None).ConfigureAwait(false))
        {
            _ = response;
        }
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
    ///     Reflection runtime stream request。
    /// </summary>
    /// <param name="Id">请求标识。</param>
    /// <param name="ItemCount">返回元素数量。</param>
    public sealed record ReflectionBenchmarkStreamRequest(Guid Id, int ItemCount) :
        GFramework.Cqrs.Abstractions.Cqrs.IStreamRequest<ReflectionBenchmarkResponse>;

    /// <summary>
    ///     Reflection runtime stream response。
    /// </summary>
    /// <param name="Id">响应标识。</param>
    public sealed record ReflectionBenchmarkResponse(Guid Id);

    /// <summary>
    ///     Generated runtime stream request。
    /// </summary>
    /// <param name="Id">请求标识。</param>
    /// <param name="ItemCount">返回元素数量。</param>
    public sealed record GeneratedBenchmarkStreamRequest(Guid Id, int ItemCount) :
        GFramework.Cqrs.Abstractions.Cqrs.IStreamRequest<GeneratedBenchmarkResponse>;

    /// <summary>
    ///     Generated runtime stream response。
    /// </summary>
    /// <param name="Id">响应标识。</param>
    public sealed record GeneratedBenchmarkResponse(Guid Id);

    /// <summary>
    ///     MediatR stream request。
    /// </summary>
    /// <param name="Id">请求标识。</param>
    /// <param name="ItemCount">返回元素数量。</param>
    public sealed record MediatRBenchmarkStreamRequest(Guid Id, int ItemCount) :
        MediatR.IStreamRequest<MediatRBenchmarkResponse>;

    /// <summary>
    ///     MediatR stream response。
    /// </summary>
    /// <param name="Id">响应标识。</param>
    public sealed record MediatRBenchmarkResponse(Guid Id);

    /// <summary>
    ///     Reflection runtime 的最小 stream request handler。
    /// </summary>
    public sealed class ReflectionBenchmarkStreamHandler :
        GFramework.Cqrs.Abstractions.Cqrs.IStreamRequestHandler<ReflectionBenchmarkStreamRequest, ReflectionBenchmarkResponse>
    {
        /// <summary>
        ///     处理 reflection benchmark stream request。
        /// </summary>
        public IAsyncEnumerable<ReflectionBenchmarkResponse> Handle(
            ReflectionBenchmarkStreamRequest request,
            CancellationToken cancellationToken)
        {
            return EnumerateAsync(
                request.Id,
                request.ItemCount,
                static id => new ReflectionBenchmarkResponse(id),
                cancellationToken);
        }
    }

    /// <summary>
    ///     Generated runtime 的最小 stream request handler。
    /// </summary>
    public sealed class GeneratedBenchmarkStreamHandler :
        GFramework.Cqrs.Abstractions.Cqrs.IStreamRequestHandler<GeneratedBenchmarkStreamRequest, GeneratedBenchmarkResponse>
    {
        /// <summary>
        ///     处理 generated benchmark stream request。
        /// </summary>
        public IAsyncEnumerable<GeneratedBenchmarkResponse> Handle(
            GeneratedBenchmarkStreamRequest request,
            CancellationToken cancellationToken)
        {
            return EnumerateAsync(
                request.Id,
                request.ItemCount,
                static id => new GeneratedBenchmarkResponse(id),
                cancellationToken);
        }
    }

    /// <summary>
    ///     MediatR 对照组的最小 stream request handler。
    /// </summary>
    public sealed class MediatRBenchmarkStreamHandler :
        MediatR.IStreamRequestHandler<MediatRBenchmarkStreamRequest, MediatRBenchmarkResponse>
    {
        /// <summary>
        ///     处理 MediatR benchmark stream request。
        /// </summary>
        public IAsyncEnumerable<MediatRBenchmarkResponse> Handle(
            MediatRBenchmarkStreamRequest request,
            CancellationToken cancellationToken)
        {
            return EnumerateAsync(
                request.Id,
                request.ItemCount,
                static id => new MediatRBenchmarkResponse(id),
                cancellationToken);
        }
    }

    /// <summary>
    ///     为三组 stream benchmark 构造相同形状的低噪声异步枚举，避免枚举体差异干扰 invoker 对照。
    /// </summary>
    private static async IAsyncEnumerable<TResponse> EnumerateAsync<TResponse>(
        Guid id,
        int itemCount,
        Func<Guid, TResponse> responseFactory,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (var index = 0; index < itemCount; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return responseFactory(id);
            await Task.CompletedTask.ConfigureAwait(false);
        }
    }
}
