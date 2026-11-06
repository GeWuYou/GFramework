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
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Ioc;
using GFramework.Core.Logging;
using GFramework.Cqrs.Abstractions.Cqrs;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

[assembly: GFramework.Cqrs.CqrsHandlerRegistryAttribute(
    typeof(GFramework.Cqrs.Benchmarks.Messaging.StreamPipelineBenchmarks.GeneratedStreamPipelineBenchmarkRegistry))]

namespace GFramework.Cqrs.Benchmarks.Messaging;

/// <summary>
///     对比不同 stream pipeline 行为数量下，单个 stream request 在直接调用、GFramework.CQRS runtime 与 MediatR 之间的 steady-state dispatch 开销。
/// </summary>
/// <remarks>
///     当前矩阵同时覆盖 <c>0 / 1 / 4</c> 个 stream pipeline 行为，以及
///     <see cref="StreamObservation.FirstItem" /> 与 <see cref="StreamObservation.DrainAll" /> 两种观测口径，
///     以便把建流固定成本与完整枚举成本拆开观察。
/// </remarks>
[Config(typeof(Config))]
public class StreamPipelineBenchmarks
{
    private MicrosoftDiContainer _container = null!;
    private ICqrsRuntime _runtime = null!;
    private ServiceProvider _serviceProvider = null!;
    private IMediator _mediatr = null!;
    private BenchmarkStreamHandler _baselineHandler = null!;
    private BenchmarkStreamRequest _request = null!;

    /// <summary>
    ///     控制当前场景注册的 stream pipeline 行为数量，保持与 request pipeline benchmark 相同的 <c>0 / 1 / 4</c> 矩阵。
    /// </summary>
    [Params(0, 1, 4)]
    public int PipelineCount { get; set; }

    /// <summary>
    ///     控制当前 benchmark 观察“只推进首个元素”还是“完整枚举整个 stream”。
    /// </summary>
    [Params(StreamObservation.FirstItem, StreamObservation.DrainAll)]
    public StreamObservation Observation { get; set; }

    /// <summary>
    ///     用于拆分 stream dispatch 固定成本与后续枚举成本的观测模式。
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
    ///     配置 stream pipeline benchmark 的公共输出格式。
    /// </summary>
    private sealed class Config : ManualConfig
    {
        public Config()
        {
            AddJob(Job.Default);
            AddColumnProvider(DefaultColumnProviders.Instance);
            AddColumn(new CustomColumn("Scenario", static (_, _) => "StreamPipeline"));
            AddDiagnoser(MemoryDiagnoser.Default);
            WithOrderer(new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest, MethodOrderPolicy.Declared));
        }
    }

    /// <summary>
    ///     构建 stream pipeline dispatch 所需的最小 runtime 宿主和对照对象。
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        LoggerFactoryResolver.Provider = new ConsoleLoggerFactoryProvider
        {
            MinLevel = LogLevel.Fatal
        };
        Fixture.Setup("StreamPipeline", handlerCount: 1, pipelineCount: PipelineCount);
        BenchmarkDispatcherCacheHelper.ClearDispatcherCaches();

        _baselineHandler = new BenchmarkStreamHandler();
        _container = BenchmarkHostFactory.CreateFrozenGFrameworkContainer(container =>
        {
            BenchmarkHostFactory.RegisterGeneratedBenchmarkRegistry<GeneratedStreamPipelineBenchmarkRegistry>(container);
            RegisterGFrameworkPipelineBehaviors(container, PipelineCount);
        });
        _runtime = GFramework.Cqrs.CqrsRuntimeFactory.CreateRuntime(
            _container,
            LoggerFactoryResolver.Provider.CreateLogger(nameof(StreamPipelineBenchmarks)));

        _serviceProvider = BenchmarkHostFactory.CreateMediatRServiceProvider(
            services =>
            {
                RegisterMediatRStreamPipelineBehaviors(services, PipelineCount);
            },
            typeof(StreamPipelineBenchmarks),
            static candidateType =>
                candidateType == typeof(BenchmarkStreamHandler) ||
                candidateType == typeof(BenchmarkStreamPipelineBehavior1) ||
                candidateType == typeof(BenchmarkStreamPipelineBehavior2) ||
                candidateType == typeof(BenchmarkStreamPipelineBehavior3) ||
                candidateType == typeof(BenchmarkStreamPipelineBehavior4),
            ServiceLifetime.Singleton);
        _mediatr = _serviceProvider.GetRequiredService<IMediator>();

        _request = new BenchmarkStreamRequest(Guid.NewGuid(), 3);
    }

    /// <summary>
    ///     释放 MediatR 对照组使用的 DI 宿主。
    /// </summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        try
        {
            BenchmarkCleanupHelper.DisposeAll(_container, _serviceProvider);
        }
        finally
        {
            BenchmarkDispatcherCacheHelper.ClearDispatcherCaches();
        }
    }

    /// <summary>
    ///     直接调用 handler，并按当前观测模式消费响应序列，作为 stream pipeline 编排之外的基线。
    /// </summary>
    /// <returns>按当前观测模式完成 stream 消费后的等待句柄。</returns>
    [Benchmark(Baseline = true)]
    public ValueTask Stream_Baseline()
    {
        return ObserveAsync(_baselineHandler.Handle(_request, CancellationToken.None), Observation);
    }

    /// <summary>
    ///     通过 GFramework.CQRS runtime 创建 stream，并按当前矩阵配置执行 stream pipeline。
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
    ///     通过 MediatR 创建 stream，并按当前矩阵配置执行 stream pipeline，作为外部设计对照。
    /// </summary>
    /// <returns>按当前观测模式完成 stream 消费后的等待句柄。</returns>
    [Benchmark]
    public ValueTask Stream_MediatR()
    {
        return ObserveAsync(_mediatr.CreateStream(_request, CancellationToken.None), Observation);
    }

    /// <summary>
    ///     按指定数量向 GFramework.CQRS 宿主注册最小 no-op stream pipeline 行为。
    /// </summary>
    /// <param name="container">当前 benchmark 使用的容器。</param>
    /// <param name="pipelineCount">要注册的行为数量。</param>
    /// <exception cref="ArgumentOutOfRangeException">行为数量不在支持的矩阵内时抛出。</exception>
    private static void RegisterGFrameworkPipelineBehaviors(MicrosoftDiContainer container, int pipelineCount)
    {
        ArgumentNullException.ThrowIfNull(container);

        switch (pipelineCount)
        {
            case 0:
                return;
            case 1:
                container.GetServicesUnsafe.AddCqrsStreamPipelineBehavior<BenchmarkStreamPipelineBehavior1>();
                return;
            case 4:
                container.GetServicesUnsafe.AddCqrsStreamPipelineBehavior<BenchmarkStreamPipelineBehavior1>();
                container.GetServicesUnsafe.AddCqrsStreamPipelineBehavior<BenchmarkStreamPipelineBehavior2>();
                container.GetServicesUnsafe.AddCqrsStreamPipelineBehavior<BenchmarkStreamPipelineBehavior3>();
                container.GetServicesUnsafe.AddCqrsStreamPipelineBehavior<BenchmarkStreamPipelineBehavior4>();
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(pipelineCount), pipelineCount,
                    "Only the 0/1/4 pipeline matrix is supported.");
        }
    }

    /// <summary>
    ///     按指定数量向 MediatR 宿主注册最小 no-op stream pipeline 行为。
    /// </summary>
    /// <param name="services">当前 benchmark 使用的服务集合。</param>
    /// <param name="pipelineCount">要注册的行为数量。</param>
    /// <exception cref="ArgumentOutOfRangeException">行为数量不在支持的矩阵内时抛出。</exception>
    private static void RegisterMediatRStreamPipelineBehaviors(IServiceCollection services, int pipelineCount)
    {
        ArgumentNullException.ThrowIfNull(services);

        switch (pipelineCount)
        {
            case 0:
                return;
            case 1:
                services.AddSingleton<MediatR.IStreamPipelineBehavior<BenchmarkStreamRequest, BenchmarkResponse>, BenchmarkStreamPipelineBehavior1>();
                return;
            case 4:
                services.AddSingleton<MediatR.IStreamPipelineBehavior<BenchmarkStreamRequest, BenchmarkResponse>, BenchmarkStreamPipelineBehavior1>();
                services.AddSingleton<MediatR.IStreamPipelineBehavior<BenchmarkStreamRequest, BenchmarkResponse>, BenchmarkStreamPipelineBehavior2>();
                services.AddSingleton<MediatR.IStreamPipelineBehavior<BenchmarkStreamRequest, BenchmarkResponse>, BenchmarkStreamPipelineBehavior3>();
                services.AddSingleton<MediatR.IStreamPipelineBehavior<BenchmarkStreamRequest, BenchmarkResponse>, BenchmarkStreamPipelineBehavior4>();
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(pipelineCount), pipelineCount,
                    "Only the 0/1/4 pipeline matrix is supported.");
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
                "Unsupported stream observation mode.")
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
        MediatR.IStreamRequest<BenchmarkResponse>;

    /// <summary>
    ///     复用 stream benchmark 的响应结构，保持跨场景可比性。
    /// </summary>
    /// <param name="Id">响应标识。</param>
    public sealed record BenchmarkResponse(Guid Id);

    /// <summary>
    ///     同时实现 GFramework.CQRS 与 MediatR 契约的最小 stream handler。
    /// </summary>
    public sealed class BenchmarkStreamHandler :
        GFramework.Cqrs.Abstractions.Cqrs.IStreamRequestHandler<BenchmarkStreamRequest, BenchmarkResponse>,
        MediatR.IStreamRequestHandler<BenchmarkStreamRequest, BenchmarkResponse>
    {
        /// <summary>
        ///     处理 GFramework.CQRS stream request。
        /// </summary>
        /// <param name="request">当前 benchmark stream 请求。</param>
        /// <param name="cancellationToken">用于中断异步枚举的取消令牌。</param>
        /// <returns>低噪声、可重复的异步响应序列。</returns>
        public IAsyncEnumerable<BenchmarkResponse> Handle(
            BenchmarkStreamRequest request,
            CancellationToken cancellationToken)
        {
            return EnumerateAsync(request, cancellationToken);
        }

        /// <summary>
        ///     处理 MediatR stream request。
        /// </summary>
        /// <param name="request">当前 benchmark stream 请求。</param>
        /// <param name="cancellationToken">用于中断异步枚举的取消令牌。</param>
        /// <returns>低噪声、可重复的异步响应序列。</returns>
        IAsyncEnumerable<BenchmarkResponse> MediatR.IStreamRequestHandler<BenchmarkStreamRequest, BenchmarkResponse>.Handle(
            BenchmarkStreamRequest request,
            CancellationToken cancellationToken)
        {
            return EnumerateAsync(request, cancellationToken);
        }

        /// <summary>
        ///     为 benchmark 构造稳定、低噪声的异步响应序列。
        /// </summary>
        /// <param name="request">决定元素数量和标识的 benchmark 请求。</param>
        /// <param name="cancellationToken">用于中断异步枚举的取消令牌。</param>
        /// <returns>按请求数量生成的响应序列。</returns>
        private static async IAsyncEnumerable<BenchmarkResponse> EnumerateAsync(
            BenchmarkStreamRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            for (int index = 0; index < request.ItemCount; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return new BenchmarkResponse(request.Id);
                await Task.CompletedTask.ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    ///     为 benchmark 提供统一的 no-op stream pipeline 行为实现，尽量把测量焦点保持在调度器与行为编排本身。
    /// </summary>
    public abstract class BenchmarkStreamPipelineBehaviorBase :
        GFramework.Cqrs.Abstractions.Cqrs.IStreamPipelineBehavior<BenchmarkStreamRequest, BenchmarkResponse>,
        MediatR.IStreamPipelineBehavior<BenchmarkStreamRequest, BenchmarkResponse>
    {
        /// <summary>
        ///     透传 GFramework.CQRS stream pipeline，避免引入额外业务逻辑噪音。
        /// </summary>
        /// <param name="message">当前 benchmark stream 请求。</param>
        /// <param name="next">继续向下执行的 stream pipeline 委托。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>下游 handler 产出的异步响应序列。</returns>
        public IAsyncEnumerable<BenchmarkResponse> Handle(
            BenchmarkStreamRequest message,
            GFramework.Cqrs.Abstractions.Cqrs.StreamMessageHandlerDelegate<BenchmarkStreamRequest, BenchmarkResponse> next,
            CancellationToken cancellationToken)
        {
            return next(message, cancellationToken);
        }

        /// <summary>
        ///     透传 MediatR stream pipeline，保持与 GFramework.CQRS 相同的 no-op 语义。
        /// </summary>
        /// <param name="request">当前 benchmark stream 请求。</param>
        /// <param name="next">继续向下执行的 MediatR stream pipeline 委托。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>下游 handler 产出的异步响应序列。</returns>
        IAsyncEnumerable<BenchmarkResponse> MediatR.IStreamPipelineBehavior<BenchmarkStreamRequest, BenchmarkResponse>.Handle(
            BenchmarkStreamRequest request,
            MediatR.StreamHandlerDelegate<BenchmarkResponse> next,
            CancellationToken cancellationToken)
        {
            _ = request;
            _ = cancellationToken;
            return next();
        }
    }

    /// <summary>
    ///     pipeline 矩阵中的第一个 no-op stream 行为。
    /// </summary>
    public sealed class BenchmarkStreamPipelineBehavior1 : BenchmarkStreamPipelineBehaviorBase
    {
    }

    /// <summary>
    ///     pipeline 矩阵中的第二个 no-op stream 行为。
    /// </summary>
    public sealed class BenchmarkStreamPipelineBehavior2 : BenchmarkStreamPipelineBehaviorBase
    {
    }

    /// <summary>
    ///     pipeline 矩阵中的第三个 no-op stream 行为。
    /// </summary>
    public sealed class BenchmarkStreamPipelineBehavior3 : BenchmarkStreamPipelineBehaviorBase
    {
    }

    /// <summary>
    ///     pipeline 矩阵中的第四个 no-op stream 行为。
    /// </summary>
    public sealed class BenchmarkStreamPipelineBehavior4 : BenchmarkStreamPipelineBehaviorBase
    {
    }

    /// <summary>
    ///     为 stream pipeline benchmark 提供 handwritten generated registry，
    ///     让默认 pipeline 宿主也能走真实的 generated stream invoker provider 接线路径。
    /// </summary>
    public sealed class GeneratedStreamPipelineBenchmarkRegistry :
        GFramework.Cqrs.ICqrsHandlerRegistry,
        GFramework.Cqrs.ICqrsStreamInvokerProvider,
        GFramework.Cqrs.IEnumeratesCqrsStreamInvokerDescriptors
    {
        private static readonly GFramework.Cqrs.CqrsStreamInvokerDescriptor Descriptor =
            new(
                typeof(GFramework.Cqrs.Abstractions.Cqrs.IStreamRequestHandler<
                    BenchmarkStreamRequest,
                    BenchmarkResponse>),
                typeof(GeneratedStreamPipelineBenchmarkRegistry).GetMethod(
                    nameof(InvokeBenchmarkStreamHandler),
                    BindingFlags.Public | BindingFlags.Static)
                ?? throw new InvalidOperationException("Missing generated stream pipeline benchmark method."));

        private static readonly IReadOnlyList<GFramework.Cqrs.CqrsStreamInvokerDescriptorEntry> Descriptors =
        [
            new GFramework.Cqrs.CqrsStreamInvokerDescriptorEntry(
                typeof(BenchmarkStreamRequest),
                typeof(BenchmarkResponse),
                Descriptor)
        ];

        /// <summary>
        ///     把 stream pipeline benchmark handler 注册为单例，保持与当前矩阵宿主一致的生命周期语义。
        /// </summary>
        /// <param name="services">用于承载 generated handler 注册的服务集合。</param>
        /// <param name="logger">记录 generated registry 接线结果的日志器。</param>
        public void Register(IServiceCollection services, ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(logger);

            services.AddSingleton(
                typeof(GFramework.Cqrs.Abstractions.Cqrs.IStreamRequestHandler<BenchmarkStreamRequest, BenchmarkResponse>),
                typeof(BenchmarkStreamHandler));
            logger.Debug("Registered generated stream pipeline benchmark handler.");
        }

        /// <summary>
        ///     返回当前 provider 暴露的全部 generated stream invoker 描述符。
        /// </summary>
        /// <returns>当前 benchmark 的 generated stream invoker 描述符集合。</returns>
        public IReadOnlyList<GFramework.Cqrs.CqrsStreamInvokerDescriptorEntry> GetDescriptors()
        {
            return Descriptors;
        }

        /// <summary>
        ///     为目标流式请求/响应类型对返回 generated stream invoker 描述符。
        /// </summary>
        /// <param name="requestType">要匹配的 stream 请求类型。</param>
        /// <param name="responseType">要匹配的 stream 响应类型。</param>
        /// <param name="descriptor">命中时返回的 generated stream invoker 描述符。</param>
        /// <returns>是否命中了当前 benchmark 的 stream 请求/响应类型对。</returns>
        public bool TryGetDescriptor(
            Type requestType,
            Type responseType,
            out GFramework.Cqrs.CqrsStreamInvokerDescriptor? descriptor)
        {
            if (requestType == typeof(BenchmarkStreamRequest) &&
                responseType == typeof(BenchmarkResponse))
            {
                descriptor = Descriptor;
                return true;
            }

            descriptor = null;
            return false;
        }

        /// <summary>
        ///     模拟 generated stream invoker provider 为 stream pipeline benchmark 产出的开放静态调用入口。
        /// </summary>
        /// <param name="handler">当前要调用的 stream handler 实例。</param>
        /// <param name="request">当前要分发的 stream 请求实例。</param>
        /// <param name="cancellationToken">用于向 handler 传播的取消令牌。</param>
        /// <returns>handler 产出的异步响应序列。</returns>
        public static object InvokeBenchmarkStreamHandler(
            object handler,
            object request,
            CancellationToken cancellationToken)
        {
            var typedHandler = (GFramework.Cqrs.Abstractions.Cqrs.IStreamRequestHandler<
                BenchmarkStreamRequest,
                BenchmarkResponse>)handler;
            var typedRequest = (BenchmarkStreamRequest)request;
            return typedHandler.Handle(typedRequest, cancellationToken);
        }
    }
}
