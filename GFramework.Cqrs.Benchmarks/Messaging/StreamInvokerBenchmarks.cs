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
using System.Threading;
using System.Threading.Tasks;
using GFramework.Cqrs.Abstractions.Cqrs;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

[assembly: GFramework.Cqrs.CqrsHandlerRegistryAttribute(
    typeof(GFramework.Cqrs.Benchmarks.Messaging.GeneratedStreamInvokerBenchmarkRegistry))]

namespace GFramework.Cqrs.Benchmarks.Messaging;

/// <summary>
///     对比 stream invoker 在 direct handler、GFramework 反射路径、GFramework generated invoker 路径与 MediatR 之间的开销差异。
/// </summary>
/// <remarks>
///     该矩阵只保留单一 handler 生命周期，避免把 invoker 路径差异与生命周期解析成本混在一起。
///     <see cref="StreamObservation.FirstItem" /> 用于近似观察建流到首个元素的瞬时成本，
///     <see cref="StreamObservation.DrainAll" /> 则保留原有完整枚举口径。
/// </remarks>
[Config(typeof(Config))]
public class StreamInvokerBenchmarks
{
    private BenchmarkServiceHost _reflectionContainer = null!;
    private ICqrsRuntime _reflectionRuntime = null!;
    private BenchmarkServiceHost _generatedContainer = null!;
    private ICqrsRuntime _generatedRuntime = null!;
    private ServiceProvider _serviceProvider = null!;
    private IMediator _mediatr = null!;
    private ReflectionBenchmarkStreamHandler _baselineHandler = null!;
    private ReflectionBenchmarkStreamRequest _reflectionRequest = null!;
    private GeneratedBenchmarkStreamRequest _generatedRequest = null!;
    private MediatRBenchmarkStreamRequest _mediatrRequest = null!;

    /// <summary>
    ///     控制当前 benchmark 观察“只推进首个元素”还是“完整枚举整个 stream”。
    /// </summary>
    [Params(StreamObservation.FirstItem, StreamObservation.DrainAll)]
    public StreamObservation Observation
    {
        get; set;
    }

    /// <summary>
    ///     用于拆分 stream invoker 固定成本与后续枚举成本的观测模式。
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
        Fixture.Setup("StreamInvoker", handlerCount: 1, pipelineCount: 0);
        BenchmarkDispatcherCacheHelper.ClearDispatcherCaches();

        _baselineHandler = new ReflectionBenchmarkStreamHandler();
        _reflectionRequest = new ReflectionBenchmarkStreamRequest(Guid.NewGuid(), 3);
        _generatedRequest = new GeneratedBenchmarkStreamRequest(Guid.NewGuid(), 3);
        _mediatrRequest = new MediatRBenchmarkStreamRequest(Guid.NewGuid(), 3);

        _reflectionContainer = BenchmarkHostFactory.CreateFrozenGFrameworkContainer(static container =>
        {
            container.RegisterTransient<GFramework.Cqrs.Abstractions.Cqrs.IStreamRequestHandler<ReflectionBenchmarkStreamRequest, ReflectionBenchmarkResponse>, ReflectionBenchmarkStreamHandler>();
        });
        _reflectionRuntime = GFramework.Cqrs.CqrsRuntimeFactory.CreateRuntime(
            _reflectionContainer,
            BenchmarkLogging.CreateLogger(nameof(StreamInvokerBenchmarks) + ".Reflection"));

        _generatedContainer = BenchmarkHostFactory.CreateFrozenGFrameworkContainer(container =>
        {
            BenchmarkHostFactory.RegisterGeneratedBenchmarkRegistry<GeneratedStreamInvokerBenchmarkRegistry>(container);
        });
        _generatedRuntime = GFramework.Cqrs.CqrsRuntimeFactory.CreateRuntime(
            _generatedContainer,
            BenchmarkLogging.CreateLogger(nameof(StreamInvokerBenchmarks) + ".Generated"));

        _serviceProvider = BenchmarkHostFactory.CreateMediatRServiceProvider(
            configure: null,
            typeof(StreamInvokerBenchmarks),
            static candidateType => candidateType == typeof(MediatRBenchmarkStreamHandler),
            ServiceLifetime.Transient);
        _mediatr = _serviceProvider.GetRequiredService<IMediator>();
    }

    /// <summary>
    ///     释放 MediatR 对照组使用的 DI 宿主，并清理静态 dispatcher 缓存。
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
    ///     直接调用最小 stream handler，并按当前观测模式消费 stream，作为 dispatch 额外开销 baseline。
    /// </summary>
    /// <returns>代表基线 stream 按当前观测模式消费完成的值任务。</returns>
    [Benchmark(Baseline = true)]
    public ValueTask Stream_Baseline()
    {
        return ObserveAsync(_baselineHandler.Handle(_reflectionRequest, CancellationToken.None), Observation);
    }

    /// <summary>
    ///     通过 GFramework.CQRS 反射 stream binding 路径创建 stream，并按当前观测模式消费。
    /// </summary>
    /// <returns>代表当前 GFramework.CQRS 反射 stream 按观测模式消费完成的值任务。</returns>
    [Benchmark]
    public ValueTask Stream_GFrameworkReflection()
    {
        return ObserveAsync(
            _reflectionRuntime.CreateStream(
                _reflectionRequest,
                CancellationToken.None),
            Observation);
    }

    /// <summary>
    ///     通过 generated stream invoker provider 预热后的 GFramework.CQRS runtime 创建 stream，并按当前观测模式消费。
    /// </summary>
    /// <returns>代表当前 GFramework.CQRS generated stream 按观测模式消费完成的值任务。</returns>
    [Benchmark]
    public ValueTask Stream_GFrameworkGenerated()
    {
        return ObserveAsync(
            _generatedRuntime.CreateStream(
                _generatedRequest,
                CancellationToken.None),
            Observation);
    }

    /// <summary>
    ///     通过 MediatR 创建 stream，并按当前观测模式消费，作为外部对照。
    /// </summary>
    /// <returns>代表当前 MediatR stream 按观测模式消费完成的值任务。</returns>
    [Benchmark]
    public ValueTask Stream_MediatR()
    {
        return ObserveAsync(_mediatr.CreateStream(_mediatrRequest, CancellationToken.None), Observation);
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
    ///     只推进到首个元素后立即释放枚举器，用来近似隔离建流与首个 <c>MoveNextAsync</c> 的固定成本。
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
            // 这里显式读取 Current，只为了让所有路径都完成首个元素的同等消费。
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
    ///     为三组 stream benchmark 构造相同形状的低噪声异步枚举，避免枚举体差异干扰 invoker 对照。
    /// </summary>
    /// <typeparam name="TResponse">当前 stream 的响应类型。</typeparam>
    /// <param name="id">每个响应复用的稳定标识。</param>
    /// <param name="itemCount">待返回的响应元素数量。</param>
    /// <param name="responseFactory">将稳定标识映射为响应对象的工厂。</param>
    /// <param name="cancellationToken">用于中断异步枚举的取消令牌。</param>
    /// <returns>供各对照路径共享的低噪声异步响应序列。</returns>
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
