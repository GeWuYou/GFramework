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
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using GFramework.Cqrs.Abstractions.Cqrs;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GFramework.Cqrs.Benchmarks.Messaging;

/// <summary>
///     对比 stream 在不同 handler 生命周期与观测方式下的额外开销。
/// </summary>
/// <remarks>
///     当前矩阵覆盖 `Singleton`、`Scoped` 与 `Transient`。
///     其中 `Scoped` 会在每次建流与枚举期间显式创建并持有真实的 DI 作用域，
///     避免把 scoped handler 错误地下沉到根容器解析，或在异步枚举尚未结束时提前释放作用域。
///     <see cref="StreamObservation" /> 当前只保留 <see cref="StreamObservation.FirstItem" /> 与
///     <see cref="StreamObservation.DrainAll" /> 两种模式，分别用于观察建流到首个元素的固定成本与完整枚举的总成本，
///     以避免把更多观测策略与 <see cref="StreamLifetimeBenchmarks" /> 的生命周期对照目标混在一起。
/// </remarks>
[Config(typeof(Config))]
public class StreamLifetimeBenchmarks
{
    private BenchmarkServiceHost _reflectionContainer = null!;
    private ICqrsRuntime _reflectionRuntime = null!;
    private ScopedBenchmarkContainer? _scopedReflectionContainer;
    private ICqrsRuntime? _scopedReflectionRuntime;
    private BenchmarkServiceHost _generatedContainer = null!;
    private ICqrsRuntime _generatedRuntime = null!;
    private ScopedBenchmarkContainer? _scopedGeneratedContainer;
    private ICqrsRuntime? _scopedGeneratedRuntime;
    private ServiceProvider _serviceProvider = null!;
    private IMediator _mediatr = null!;
    private ReflectionBenchmarkStreamHandler _baselineHandler = null!;
    private ReflectionBenchmarkStreamRequest _reflectionRequest = null!;
    private GeneratedBenchmarkStreamRequest _generatedRequest = null!;
    private MediatRBenchmarkStreamRequest _mediatrRequest = null!;
    private ILogger _reflectionRuntimeLogger = null!;
    private ILogger _generatedRuntimeLogger = null!;

    /// <summary>
    ///     控制当前 benchmark 使用的 handler 生命周期。
    /// </summary>
    [Params(HandlerLifetime.Singleton, HandlerLifetime.Scoped, HandlerLifetime.Transient)]
    public HandlerLifetime Lifetime
    {
        get; set;
    }

    /// <summary>
    ///     控制当前 benchmark 观察“只推进首个元素”还是“完整枚举整个 stream”。
    /// </summary>
    [Params(StreamObservation.FirstItem, StreamObservation.DrainAll)]
    public StreamObservation Observation
    {
        get; set;
    }

    /// <summary>
    ///     可公平比较的 benchmark handler 生命周期集合。
    /// </summary>
    public enum HandlerLifetime
    {
        /// <summary>
        ///     复用单个 handler 实例。
        /// </summary>
        Singleton,

        /// <summary>
        ///     每次建流在显式作用域内解析并复用 handler 实例，且作用域会覆盖整个枚举周期。
        /// </summary>
        Scoped,

        /// <summary>
        ///     每次建流都重新解析新的 handler 实例。
        /// </summary>
        Transient,
    }

    /// <summary>
    ///     用于拆分 stream dispatch 与后续枚举成本的观测模式。
    /// </summary>
    public enum StreamObservation
    {
        /// <summary>
        ///     只推进到首个元素后立即释放枚举器。
        /// </summary>
        FirstItem,

        /// <summary>
        ///     完整枚举整个 stream，保留原有 benchmark 语义。
        /// </summary>
        DrainAll,
    }

    /// <summary>
    ///     配置 stream 生命周期 benchmark 的公共输出格式。
    /// </summary>
    private sealed class Config : ManualConfig
    {
        public Config()
        {
            AddJob(Job.Default);
            AddColumnProvider(DefaultColumnProviders.Instance);
            AddColumn(new CustomColumn("Scenario", static (_, _) => "StreamLifetime"));
            AddDiagnoser(MemoryDiagnoser.Default);
            WithOrderer(new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest, MethodOrderPolicy.Declared));
        }
    }

    /// <summary>
    ///     构建当前生命周期下的 GFramework reflection、GFramework generated 与 MediatR stream 对照宿主。
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        ConfigureBenchmarkInfrastructure();
        InitializeRequestsAndLoggers();
        InitializeReflectionRuntime();
        InitializeGeneratedRuntime();
        InitializeMediatRRuntime();
    }

    /// <summary>
    ///     释放当前生命周期矩阵持有的 benchmark 宿主资源，并清理 dispatcher 缓存。
    /// </summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        try
        {
            BenchmarkCleanupHelper.DisposeAll(_reflectionContainer, _generatedContainer, _serviceProvider);
        }
        finally
        {
            BenchmarkDispatcherCacheHelper.ClearDispatcherCaches();
        }
    }

    /// <summary>
    ///     直接调用 handler，并按当前观测模式消费 stream，作为不同生命周期矩阵下的 dispatch 额外开销 baseline。
    /// </summary>
    /// <returns>代表基线 handler stream 按当前观测模式消费完成的值任务。</returns>
    [Benchmark(Baseline = true)]
    public ValueTask Stream_Baseline()
    {
        return ObserveAsync(_baselineHandler.Handle(_reflectionRequest, CancellationToken.None), Observation);
    }

    /// <summary>
    ///     通过 GFramework.CQRS reflection stream binding 路径创建 stream，并按当前观测模式消费。
    /// </summary>
    /// <returns>代表当前 reflection stream 按当前观测模式消费完成的值任务。</returns>
    [Benchmark]
    public ValueTask Stream_GFrameworkReflection()
    {
        if (Lifetime == HandlerLifetime.Scoped)
        {
            return ObserveAsync(
                BenchmarkHostFactory.CreateScopedGFrameworkStream(
                    _scopedReflectionRuntime!,
                    _scopedReflectionContainer!,
                    _reflectionRequest,
                    CancellationToken.None),
                Observation);
        }

        return ObserveAsync(
            _reflectionRuntime.CreateStream(
                _reflectionRequest,
                CancellationToken.None),
            Observation);
    }

    /// <summary>
    ///     通过 generated stream invoker provider 预热后的 GFramework.CQRS runtime 创建 stream，并按当前观测模式消费。
    /// </summary>
    /// <returns>代表当前 generated stream 按当前观测模式消费完成的值任务。</returns>
    [Benchmark]
    public ValueTask Stream_GFrameworkGenerated()
    {
        if (Lifetime == HandlerLifetime.Scoped)
        {
            return ObserveAsync(
                BenchmarkHostFactory.CreateScopedGFrameworkStream(
                    _scopedGeneratedRuntime!,
                    _scopedGeneratedContainer!,
                    _generatedRequest,
                    CancellationToken.None),
                Observation);
        }

        return ObserveAsync(
            _generatedRuntime.CreateStream(
                _generatedRequest,
                CancellationToken.None),
            Observation);
    }

    /// <summary>
    ///     通过 MediatR 创建 stream，并按当前观测模式消费，作为外部对照。
    /// </summary>
    /// <returns>代表当前 MediatR stream 按当前观测模式消费完成的值任务。</returns>
    [Benchmark]
    public ValueTask Stream_MediatR()
    {
        if (Lifetime == HandlerLifetime.Scoped)
        {
            return ObserveAsync(
                BenchmarkHostFactory.CreateScopedMediatRStream(
                    _serviceProvider,
                    _mediatrRequest,
                    CancellationToken.None),
                Observation);
        }

        return ObserveAsync(_mediatr.CreateStream(_mediatrRequest, CancellationToken.None), Observation);
    }

    /// <summary>
    ///     按生命周期把 reflection benchmark stream handler 注册到 GFramework 容器。
    /// </summary>
    /// <param name="container">当前 benchmark 拥有并负责释放的容器。</param>
    /// <param name="lifetime">待比较的 handler 生命周期。</param>
    private static void RegisterReflectionHandler(BenchmarkServiceHost container, HandlerLifetime lifetime)
    {
        ArgumentNullException.ThrowIfNull(container);

        switch (lifetime)
        {
            case HandlerLifetime.Singleton:
                container.RegisterSingleton<
                    GFramework.Cqrs.Abstractions.Cqrs.IStreamRequestHandler<ReflectionBenchmarkStreamRequest, ReflectionBenchmarkResponse>,
                    ReflectionBenchmarkStreamHandler>();
                return;

            case HandlerLifetime.Scoped:
                container.RegisterScoped<
                    GFramework.Cqrs.Abstractions.Cqrs.IStreamRequestHandler<ReflectionBenchmarkStreamRequest, ReflectionBenchmarkResponse>,
                    ReflectionBenchmarkStreamHandler>();
                return;

            case HandlerLifetime.Transient:
                container.RegisterTransient<
                    GFramework.Cqrs.Abstractions.Cqrs.IStreamRequestHandler<ReflectionBenchmarkStreamRequest, ReflectionBenchmarkResponse>,
                    ReflectionBenchmarkStreamHandler>();
                return;

            default:
                throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, "Unsupported benchmark handler lifetime.");
        }
    }

    /// <summary>
    ///     按生命周期把 generated benchmark stream handler 注册到 GFramework 容器。
    /// </summary>
    /// <param name="container">当前 benchmark 拥有并负责释放的容器。</param>
    /// <param name="lifetime">待比较的 handler 生命周期。</param>
    /// <remarks>
    ///     generated registry 只负责暴露静态 descriptor；
    ///     生命周期矩阵仍由 benchmark 主体显式覆盖 handler 注册，避免把 descriptor 发现与实例解析混在一起。
    /// </remarks>
    private static void RegisterGeneratedHandler(BenchmarkServiceHost container, HandlerLifetime lifetime)
    {
        ArgumentNullException.ThrowIfNull(container);

        switch (lifetime)
        {
            case HandlerLifetime.Singleton:
                container.RegisterSingleton<
                    GFramework.Cqrs.Abstractions.Cqrs.IStreamRequestHandler<GeneratedBenchmarkStreamRequest, GeneratedBenchmarkResponse>,
                    GeneratedBenchmarkStreamHandler>();
                return;

            case HandlerLifetime.Scoped:
                container.RegisterScoped<
                    GFramework.Cqrs.Abstractions.Cqrs.IStreamRequestHandler<GeneratedBenchmarkStreamRequest, GeneratedBenchmarkResponse>,
                    GeneratedBenchmarkStreamHandler>();
                return;

            case HandlerLifetime.Transient:
                container.RegisterTransient<
                    GFramework.Cqrs.Abstractions.Cqrs.IStreamRequestHandler<GeneratedBenchmarkStreamRequest, GeneratedBenchmarkResponse>,
                    GeneratedBenchmarkStreamHandler>();
                return;

            default:
                throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, "Unsupported benchmark handler lifetime.");
        }
    }

    /// <summary>
    ///     将 benchmark 生命周期映射为 MediatR 组装所需的 <see cref="ServiceLifetime" />。
    /// </summary>
    /// <param name="lifetime">待比较的 handler 生命周期。</param>
    /// <returns>当前生命周期对应的 MediatR 注册方式。</returns>
    private static ServiceLifetime ResolveMediatRLifetime(HandlerLifetime lifetime)
    {
        return lifetime switch
        {
            HandlerLifetime.Singleton => ServiceLifetime.Singleton,
            HandlerLifetime.Scoped => ServiceLifetime.Scoped,
            HandlerLifetime.Transient => ServiceLifetime.Transient,
            _ => throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, "Unsupported benchmark handler lifetime."),
        };
    }

    /// <summary>
    ///     初始化当前 benchmark 所需的全局日志与夹具基础设施。
    /// </summary>
    private void ConfigureBenchmarkInfrastructure()
    {
        Fixture.Setup($"StreamLifetime/{Lifetime}", handlerCount: 1, pipelineCount: 0);
        BenchmarkDispatcherCacheHelper.ClearDispatcherCaches();
    }

    /// <summary>
    ///     初始化当前 benchmark 会复用的请求对象、baseline handler 与日志器。
    /// </summary>
    private void InitializeRequestsAndLoggers()
    {
        _baselineHandler = new ReflectionBenchmarkStreamHandler();
        _reflectionRequest = new ReflectionBenchmarkStreamRequest(Guid.NewGuid(), 3);
        _generatedRequest = new GeneratedBenchmarkStreamRequest(Guid.NewGuid(), 3);
        _mediatrRequest = new MediatRBenchmarkStreamRequest(Guid.NewGuid(), 3);
        _reflectionRuntimeLogger =
            BenchmarkLogging.CreateLogger(nameof(StreamLifetimeBenchmarks) + ".Reflection." + Lifetime);
        _generatedRuntimeLogger =
            BenchmarkLogging.CreateLogger(nameof(StreamLifetimeBenchmarks) + ".Generated." + Lifetime);
    }

    /// <summary>
    ///     初始化 reflection 路径的 GFramework runtime。
    /// </summary>
    private void InitializeReflectionRuntime()
    {
        _reflectionContainer = BenchmarkHostFactory.CreateFrozenGFrameworkContainer(container =>
        {
            RegisterReflectionHandler(container, Lifetime);
        });

        if (Lifetime != HandlerLifetime.Scoped)
        {
            _reflectionRuntime = GFramework.Cqrs.CqrsRuntimeFactory.CreateRuntime(
                _reflectionContainer,
                _reflectionRuntimeLogger);
            return;
        }

        _scopedReflectionContainer = new ScopedBenchmarkContainer(_reflectionContainer);
        _scopedReflectionRuntime = GFramework.Cqrs.CqrsRuntimeFactory.CreateRuntime(
            _scopedReflectionContainer,
            _reflectionRuntimeLogger);
    }

    /// <summary>
    ///     初始化 generated registry 路径的 GFramework runtime。
    /// </summary>
    private void InitializeGeneratedRuntime()
    {
        _generatedContainer = BenchmarkHostFactory.CreateFrozenGFrameworkContainer(container =>
        {
            BenchmarkHostFactory.RegisterGeneratedBenchmarkRegistry<GeneratedStreamLifetimeBenchmarkRegistry>(container);
            RegisterGeneratedHandler(container, Lifetime);
        });

        // runtime 在 provider 构建后按当前矩阵惰性解析；
        // 这里显式创建带生命周期后缀的 runtime，只是为了区分不同 benchmark 矩阵的 dispatcher 日志。
        if (Lifetime != HandlerLifetime.Scoped)
        {
            _generatedRuntime = GFramework.Cqrs.CqrsRuntimeFactory.CreateRuntime(
                _generatedContainer,
                _generatedRuntimeLogger);
            return;
        }

        _scopedGeneratedContainer = new ScopedBenchmarkContainer(_generatedContainer);
        _scopedGeneratedRuntime = GFramework.Cqrs.CqrsRuntimeFactory.CreateRuntime(
            _scopedGeneratedContainer,
            _generatedRuntimeLogger);
    }

    /// <summary>
    ///     初始化 MediatR 对照宿主。
    /// </summary>
    private void InitializeMediatRRuntime()
    {
        _serviceProvider = BenchmarkHostFactory.CreateMediatRServiceProvider(
            configure: null,
            typeof(StreamLifetimeBenchmarks),
            static candidateType => candidateType == typeof(MediatRBenchmarkStreamHandler),
            ResolveMediatRLifetime(Lifetime));

        if (Lifetime != HandlerLifetime.Scoped)
        {
            _mediatr = _serviceProvider.GetRequiredService<IMediator>();
        }
    }

    /// <summary>
    ///     按观测模式消费 stream，便于把“建流/首个元素”和“完整枚举”分开观察。
    /// </summary>
    /// <typeparam name="TResponse">当前 stream 的响应类型。</typeparam>
    /// <param name="responses">待观察的异步响应序列。</param>
    /// <param name="observation">当前 benchmark 选定的观测模式。</param>
    /// <returns>异步消费完成后的等待句柄。</returns>
    private static ValueTask ObserveAsync<TResponse>(
        IAsyncEnumerable<TResponse> responses,
        StreamObservation observation)
    {
        ArgumentNullException.ThrowIfNull(responses);

        return observation switch
        {
            StreamObservation.FirstItem => ConsumeFirstItemAsync(responses, CancellationToken.None),
            StreamObservation.DrainAll => DrainAsync(responses),
            _ => throw new ArgumentOutOfRangeException(
                nameof(observation),
                observation,
                "Unsupported stream observation mode."),
        };
    }

    /// <summary>
    ///     只推进到首个元素后立即释放枚举器，用来近似隔离建流与首个 `MoveNextAsync` 的固定成本。
    /// </summary>
    /// <typeparam name="TResponse">当前 stream 的响应类型。</typeparam>
    /// <param name="responses">待观察的异步响应序列。</param>
    /// <param name="cancellationToken">用于向异步枚举器传播取消的令牌。</param>
    /// <returns>消费首个元素后的等待句柄。</returns>
    private static async ValueTask ConsumeFirstItemAsync<TResponse>(
        IAsyncEnumerable<TResponse> responses,
        CancellationToken cancellationToken)
    {
        var enumerator = responses.GetAsyncEnumerator(cancellationToken);
        await using (enumerator.ConfigureAwait(false))
        {
            if (await enumerator.MoveNextAsync().ConfigureAwait(false))
            {
                _ = enumerator.Current;
            }
        }
    }

    /// <summary>
    ///     完整枚举整个 stream，保留原 benchmark 的总成本观测口径。
    /// </summary>
    /// <typeparam name="TResponse">当前 stream 的响应类型。</typeparam>
    /// <param name="responses">待完整枚举的异步响应序列。</param>
    /// <returns>完整枚举结束后的等待句柄。</returns>
    private static async ValueTask DrainAsync<TResponse>(IAsyncEnumerable<TResponse> responses)
    {
        await foreach (var response in responses.ConfigureAwait(false))
        {
            _ = response;
        }
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
        /// <param name="request">当前 reflection benchmark stream 请求。</param>
        /// <param name="cancellationToken">用于中断异步枚举的取消令牌。</param>
        /// <returns>完整枚举所需的低噪声异步响应序列。</returns>
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
        /// <param name="request">当前 generated benchmark stream 请求。</param>
        /// <param name="cancellationToken">用于中断异步枚举的取消令牌。</param>
        /// <returns>完整枚举所需的低噪声异步响应序列。</returns>
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
        /// <param name="request">当前 MediatR benchmark stream 请求。</param>
        /// <param name="cancellationToken">用于中断异步枚举的取消令牌。</param>
        /// <returns>完整枚举所需的低噪声异步响应序列。</returns>
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
    ///     为生命周期矩阵构造相同形状的低噪声异步枚举，避免不同口径的枚举体差异干扰 dispatch 对照。
    /// </summary>
    private static async IAsyncEnumerable<TResponse> EnumerateAsync<TResponse>(
        Guid id,
        int itemCount,
        Func<Guid, TResponse> responseFactory,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (var index = 0; index < itemCount; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return responseFactory(id);
            await Task.CompletedTask.ConfigureAwait(false);
        }
    }
}
