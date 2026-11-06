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
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Ioc;
using GFramework.Core.Logging;
using GFramework.Cqrs.Abstractions.Cqrs;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using GeneratedMediator = Mediator.Mediator;

[assembly: GFramework.Cqrs.CqrsHandlerRegistryAttribute(
    typeof(GFramework.Cqrs.Benchmarks.Messaging.GeneratedDefaultStreamingBenchmarkRegistry))]

namespace GFramework.Cqrs.Benchmarks.Messaging;

/// <summary>
///     对比单个 stream request 在直接调用、GFramework.CQRS runtime、NuGet `Mediator` 与 MediatR 之间的 steady-state stream 开销。
/// </summary>
/// <remarks>
///     默认 generated-provider stream 宿主同时暴露 <see cref="StreamObservation.FirstItem" /> 与
///     <see cref="StreamObservation.DrainAll" /> 两种观测口径，
///     以便把“建流到首个元素”的固定成本与“完整枚举整个 stream”的总成本拆开观察。
/// </remarks>
[Config(typeof(Config))]
public class StreamingBenchmarks
{
    private MicrosoftDiContainer _container = null!;
    private ICqrsRuntime _runtime = null!;
    private ServiceProvider _mediatrServiceProvider = null!;
    private ServiceProvider _mediatorServiceProvider = null!;
    private IMediator _mediatr = null!;
    private GeneratedMediator _mediator = null!;
    private BenchmarkStreamHandler _baselineHandler = null!;
    private BenchmarkStreamRequest _request = null!;

    /// <summary>
    ///     控制当前 benchmark 观察“只推进首个元素”还是“完整枚举整个 stream”。
    /// </summary>
    [Params(StreamObservation.FirstItem, StreamObservation.DrainAll)]
    public StreamObservation Observation { get; set; }

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
        DrainAll
    }

    /// <summary>
    ///     配置 stream benchmark 的公共输出格式。
    /// </summary>
    private sealed class Config : ManualConfig
    {
        public Config()
        {
            AddJob(Job.Default);
            AddColumnProvider(DefaultColumnProviders.Instance);
            AddColumn(new CustomColumn("Scenario", static (_, _) => "StreamRequest"));
            AddDiagnoser(MemoryDiagnoser.Default);
            WithOrderer(new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest, MethodOrderPolicy.Declared));
        }
    }

    /// <summary>
    ///     构建 stream dispatch 所需的最小 runtime 宿主和对照对象。
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        LoggerFactoryResolver.Provider = new ConsoleLoggerFactoryProvider
        {
            MinLevel = LogLevel.Fatal
        };
        Fixture.Setup("StreamRequest", handlerCount: 1, pipelineCount: 0);
        BenchmarkDispatcherCacheHelper.ClearDispatcherCaches();

        _baselineHandler = new BenchmarkStreamHandler();
        _container = BenchmarkHostFactory.CreateFrozenGFrameworkContainer(container =>
        {
            BenchmarkHostFactory.RegisterGeneratedBenchmarkRegistry<GeneratedDefaultStreamingBenchmarkRegistry>(container);
        });
        _runtime = GFramework.Cqrs.CqrsRuntimeFactory.CreateRuntime(
            _container,
            LoggerFactoryResolver.Provider.CreateLogger(nameof(StreamingBenchmarks)));

        _mediatrServiceProvider = BenchmarkHostFactory.CreateMediatRServiceProvider(
            configure: null,
            typeof(StreamingBenchmarks),
            static candidateType => candidateType == typeof(BenchmarkStreamHandler),
            ServiceLifetime.Singleton);
        _mediatr = _mediatrServiceProvider.GetRequiredService<IMediator>();

        _mediatorServiceProvider = BenchmarkHostFactory.CreateMediatorServiceProvider(configure: null);
        _mediator = _mediatorServiceProvider.GetRequiredService<GeneratedMediator>();

        _request = new BenchmarkStreamRequest(Guid.NewGuid(), 3);
    }

    /// <summary>
    ///     释放 MediatR 与 `Mediator` 对照组使用的 DI 宿主。
    /// </summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        try
        {
            BenchmarkCleanupHelper.DisposeAll(_container, _mediatrServiceProvider, _mediatorServiceProvider);
        }
        finally
        {
            BenchmarkDispatcherCacheHelper.ClearDispatcherCaches();
        }
    }

    /// <summary>
    ///     直接调用 handler，并按当前观测模式消费响应序列，作为 stream dispatch 额外开销的 baseline。
    /// </summary>
    /// <returns>按当前观测模式完成 stream 消费后的等待句柄。</returns>
    [Benchmark(Baseline = true)]
    public ValueTask Stream_Baseline()
    {
        return ObserveAsync(_baselineHandler.Handle(_request, CancellationToken.None), Observation);
    }

    /// <summary>
    ///     通过 GFramework.CQRS runtime 创建 stream，并按当前观测模式消费。
    /// </summary>
    /// <returns>按当前观测模式完成 stream 消费后的等待句柄。</returns>
    [Benchmark]
    public ValueTask Stream_GFrameworkCqrs()
    {
        return ObserveAsync(
            _runtime.CreateStream(
                _request,
                CancellationToken.None),
            Observation);
    }

    /// <summary>
    ///     通过 MediatR 创建 stream，并按当前观测模式消费，作为外部设计对照。
    /// </summary>
    /// <returns>按当前观测模式完成 stream 消费后的等待句柄。</returns>
    [Benchmark]
    public ValueTask Stream_MediatR()
    {
        return ObserveAsync(_mediatr.CreateStream(_request, CancellationToken.None), Observation);
    }

    /// <summary>
    ///     通过 NuGet `Mediator` 的 source-generated concrete mediator 创建 stream，并按当前观测模式消费。
    /// </summary>
    /// <returns>按当前观测模式完成 stream 消费后的等待句柄。</returns>
    [Benchmark]
    public ValueTask Stream_Mediator()
    {
        return ObserveAsync(_mediator.CreateStream(_request, CancellationToken.None), Observation);
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
                "Unsupported stream observation mode.")
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
    ///     Benchmark stream request。
    /// </summary>
    /// <param name="Id">请求标识。</param>
    /// <param name="ItemCount">返回元素数量。</param>
    public sealed record BenchmarkStreamRequest(Guid Id, int ItemCount) :
        GFramework.Cqrs.Abstractions.Cqrs.IStreamRequest<BenchmarkResponse>,
        Mediator.IStreamRequest<BenchmarkResponse>,
        MediatR.IStreamRequest<BenchmarkResponse>;

    /// <summary>
    ///     复用 request benchmark 的响应结构，保持跨场景可比性。
    /// </summary>
    /// <param name="Id">响应标识。</param>
    public sealed record BenchmarkResponse(Guid Id);

    /// <summary>
    ///     同时实现 GFramework.CQRS、NuGet `Mediator` 与 MediatR 契约的最小 stream handler。
    /// </summary>
    public sealed class BenchmarkStreamHandler :
        GFramework.Cqrs.Abstractions.Cqrs.IStreamRequestHandler<BenchmarkStreamRequest, BenchmarkResponse>,
        Mediator.IStreamRequestHandler<BenchmarkStreamRequest, BenchmarkResponse>,
        MediatR.IStreamRequestHandler<BenchmarkStreamRequest, BenchmarkResponse>
    {
        /// <summary>
        ///     处理 GFramework.CQRS stream request。
        /// </summary>
        public IAsyncEnumerable<BenchmarkResponse> Handle(
            BenchmarkStreamRequest request,
            CancellationToken cancellationToken)
        {
            return EnumerateAsync(request, cancellationToken);
        }

        /// <summary>
        ///     处理 NuGet `Mediator` stream request。
        /// </summary>
        IAsyncEnumerable<BenchmarkResponse> Mediator.IStreamRequestHandler<BenchmarkStreamRequest, BenchmarkResponse>.Handle(
            BenchmarkStreamRequest request,
            CancellationToken cancellationToken)
        {
            return Handle(request, cancellationToken);
        }

        /// <summary>
        ///     处理 MediatR stream request。
        /// </summary>
        IAsyncEnumerable<BenchmarkResponse> MediatR.IStreamRequestHandler<BenchmarkStreamRequest, BenchmarkResponse>.Handle(
            BenchmarkStreamRequest request,
            CancellationToken cancellationToken)
        {
            return EnumerateAsync(request, cancellationToken);
        }

        /// <summary>
        ///     为 benchmark 构造稳定、低噪声的异步响应序列。
        /// </summary>
        private static async IAsyncEnumerable<BenchmarkResponse> EnumerateAsync(
            BenchmarkStreamRequest request,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            for (int index = 0; index < request.ItemCount; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return new BenchmarkResponse(request.Id);
                await Task.CompletedTask.ConfigureAwait(false);
            }
        }
    }
}
