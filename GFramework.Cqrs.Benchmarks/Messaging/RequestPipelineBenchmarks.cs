// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using System;
using System.Threading;
using System.Threading.Tasks;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Ioc;
using GFramework.Core.Logging;
using GFramework.Cqrs.Abstractions.Cqrs;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace GFramework.Cqrs.Benchmarks.Messaging;

/// <summary>
///     对比不同 pipeline 行为数量下，单个 request 在直接调用、GFramework.CQRS runtime 与 MediatR 之间的 steady-state dispatch 开销。
/// </summary>
[Config(typeof(Config))]
public class RequestPipelineBenchmarks
{
    private MicrosoftDiContainer _container = null!;
    private ICqrsRuntime _runtime = null!;
    private ServiceProvider _serviceProvider = null!;
    private IMediator _mediatr = null!;
    private BenchmarkRequestHandler _baselineHandler = null!;
    private BenchmarkRequest _request = null!;

    /// <summary>
    ///     控制当前场景注册的 pipeline 行为数量，保持与 `Mediator` benchmark 常见的“无行为 / 少量行为 / 多行为”矩阵一致。
    /// </summary>
    [Params(0, 1, 4)]
    public int PipelineCount { get; set; }

    /// <summary>
    ///     配置 request pipeline benchmark 的公共输出格式。
    /// </summary>
    private sealed class Config : ManualConfig
    {
        public Config()
        {
            AddJob(Job.Default);
            AddColumnProvider(DefaultColumnProviders.Instance);
            AddColumn(new CustomColumn("Scenario", static (_, _) => "RequestPipeline"));
            AddDiagnoser(MemoryDiagnoser.Default);
            WithOrderer(new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest, MethodOrderPolicy.Declared));
        }
    }

    /// <summary>
    ///     构建 request pipeline dispatch 所需的最小 runtime 宿主和对照对象。
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        LoggerFactoryResolver.Provider = new ConsoleLoggerFactoryProvider
        {
            MinLevel = LogLevel.Fatal
        };
        Fixture.Setup("RequestPipeline", handlerCount: 1, pipelineCount: PipelineCount);
        BenchmarkDispatcherCacheHelper.ClearDispatcherCaches();

        _baselineHandler = new BenchmarkRequestHandler();
        _container = BenchmarkHostFactory.CreateFrozenGFrameworkContainer(container =>
        {
            container.RegisterSingleton<GFramework.Cqrs.Abstractions.Cqrs.IRequestHandler<BenchmarkRequest, BenchmarkResponse>>(
                _baselineHandler);
            RegisterGFrameworkPipelineBehaviors(container, PipelineCount);
        });
        _runtime = GFramework.Cqrs.CqrsRuntimeFactory.CreateRuntime(
            _container,
            LoggerFactoryResolver.Provider.CreateLogger(nameof(RequestPipelineBenchmarks)));

        _serviceProvider = BenchmarkHostFactory.CreateMediatRServiceProvider(
            services =>
            {
                RegisterMediatRPipelineBehaviors(services, PipelineCount);
            },
            typeof(RequestPipelineBenchmarks),
            static candidateType =>
                candidateType == typeof(BenchmarkRequestHandler) ||
                candidateType == typeof(BenchmarkPipelineBehavior1) ||
                candidateType == typeof(BenchmarkPipelineBehavior2) ||
                candidateType == typeof(BenchmarkPipelineBehavior3) ||
                candidateType == typeof(BenchmarkPipelineBehavior4),
            ServiceLifetime.Singleton);
        _mediatr = _serviceProvider.GetRequiredService<IMediator>();

        _request = new BenchmarkRequest(Guid.NewGuid());
    }

    /// <summary>
    ///     释放 MediatR 对照组使用的 DI 宿主。
    /// </summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        _container.Dispose();
        _serviceProvider.Dispose();
        BenchmarkDispatcherCacheHelper.ClearDispatcherCaches();
    }

    /// <summary>
    ///     直接调用 handler，作为 pipeline 编排之外的基线。
    /// </summary>
    [Benchmark(Baseline = true)]
    public ValueTask<BenchmarkResponse> SendRequest_Baseline()
    {
        return _baselineHandler.Handle(_request, CancellationToken.None);
    }

    /// <summary>
    ///     通过 GFramework.CQRS runtime 发送 request，并按当前矩阵配置执行 pipeline。
    /// </summary>
    [Benchmark]
    public ValueTask<BenchmarkResponse> SendRequest_GFrameworkCqrs()
    {
        return _runtime.SendAsync(BenchmarkContext.Instance, _request, CancellationToken.None);
    }

    /// <summary>
    ///     通过 MediatR 发送 request，并按当前矩阵配置执行 pipeline，作为外部设计对照。
    /// </summary>
    [Benchmark]
    public Task<BenchmarkResponse> SendRequest_MediatR()
    {
        return _mediatr.Send(_request, CancellationToken.None);
    }

    /// <summary>
    ///     按指定数量向 GFramework.CQRS 宿主注册最小 no-op pipeline 行为。
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
                container.RegisterCqrsPipelineBehavior<BenchmarkPipelineBehavior1>();
                return;
            case 4:
                container.RegisterCqrsPipelineBehavior<BenchmarkPipelineBehavior1>();
                container.RegisterCqrsPipelineBehavior<BenchmarkPipelineBehavior2>();
                container.RegisterCqrsPipelineBehavior<BenchmarkPipelineBehavior3>();
                container.RegisterCqrsPipelineBehavior<BenchmarkPipelineBehavior4>();
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(pipelineCount), pipelineCount,
                    "Only the 0/1/4 pipeline matrix is supported.");
        }
    }

    /// <summary>
    ///     按指定数量向 MediatR 宿主注册最小 no-op pipeline 行为。
    /// </summary>
    /// <param name="services">当前 benchmark 使用的服务集合。</param>
    /// <param name="pipelineCount">要注册的行为数量。</param>
    /// <exception cref="ArgumentOutOfRangeException">行为数量不在支持的矩阵内时抛出。</exception>
    private static void RegisterMediatRPipelineBehaviors(IServiceCollection services, int pipelineCount)
    {
        ArgumentNullException.ThrowIfNull(services);

        switch (pipelineCount)
        {
            case 0:
                return;
            case 1:
                services.AddSingleton<MediatR.IPipelineBehavior<BenchmarkRequest, BenchmarkResponse>, BenchmarkPipelineBehavior1>();
                return;
            case 4:
                services.AddSingleton<MediatR.IPipelineBehavior<BenchmarkRequest, BenchmarkResponse>, BenchmarkPipelineBehavior1>();
                services.AddSingleton<MediatR.IPipelineBehavior<BenchmarkRequest, BenchmarkResponse>, BenchmarkPipelineBehavior2>();
                services.AddSingleton<MediatR.IPipelineBehavior<BenchmarkRequest, BenchmarkResponse>, BenchmarkPipelineBehavior3>();
                services.AddSingleton<MediatR.IPipelineBehavior<BenchmarkRequest, BenchmarkResponse>, BenchmarkPipelineBehavior4>();
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(pipelineCount), pipelineCount,
                    "Only the 0/1/4 pipeline matrix is supported.");
        }
    }

    /// <summary>
    ///     Benchmark request。
    /// </summary>
    /// <param name="Id">请求标识。</param>
    public sealed record BenchmarkRequest(Guid Id) :
        GFramework.Cqrs.Abstractions.Cqrs.IRequest<BenchmarkResponse>,
        MediatR.IRequest<BenchmarkResponse>;

    /// <summary>
    ///     Benchmark response。
    /// </summary>
    /// <param name="Id">响应标识。</param>
    public sealed record BenchmarkResponse(Guid Id);

    /// <summary>
    ///     同时实现 GFramework.CQRS 与 MediatR 契约的最小 request handler。
    /// </summary>
    public sealed class BenchmarkRequestHandler :
        GFramework.Cqrs.Abstractions.Cqrs.IRequestHandler<BenchmarkRequest, BenchmarkResponse>,
        MediatR.IRequestHandler<BenchmarkRequest, BenchmarkResponse>
    {
        /// <summary>
        ///     处理 GFramework.CQRS request。
        /// </summary>
        public ValueTask<BenchmarkResponse> Handle(BenchmarkRequest request, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(new BenchmarkResponse(request.Id));
        }

        /// <summary>
        ///     处理 MediatR request。
        /// </summary>
        Task<BenchmarkResponse> MediatR.IRequestHandler<BenchmarkRequest, BenchmarkResponse>.Handle(
            BenchmarkRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new BenchmarkResponse(request.Id));
        }
    }

    /// <summary>
    ///     为 benchmark 提供统一的 no-op pipeline 行为实现，尽量把测量焦点保持在调度器与行为编排本身。
    /// </summary>
    public abstract class BenchmarkPipelineBehaviorBase :
        GFramework.Cqrs.Abstractions.Cqrs.IPipelineBehavior<BenchmarkRequest, BenchmarkResponse>,
        MediatR.IPipelineBehavior<BenchmarkRequest, BenchmarkResponse>
    {
        /// <summary>
        ///     透传 GFramework.CQRS pipeline，避免引入额外业务逻辑噪音。
        /// </summary>
        public ValueTask<BenchmarkResponse> Handle(
            BenchmarkRequest message,
            GFramework.Cqrs.Abstractions.Cqrs.MessageHandlerDelegate<BenchmarkRequest, BenchmarkResponse> next,
            CancellationToken cancellationToken)
        {
            return next(message, cancellationToken);
        }

        /// <summary>
        ///     透传 MediatR pipeline，保持与 GFramework.CQRS 相同的 no-op 语义。
        /// </summary>
        Task<BenchmarkResponse> MediatR.IPipelineBehavior<BenchmarkRequest, BenchmarkResponse>.Handle(
            BenchmarkRequest request,
            RequestHandlerDelegate<BenchmarkResponse> next,
            CancellationToken cancellationToken)
        {
            return next();
        }
    }

    /// <summary>
    ///     pipeline 行为槽位 1。
    /// </summary>
    public sealed class BenchmarkPipelineBehavior1 : BenchmarkPipelineBehaviorBase
    {
    }

    /// <summary>
    ///     pipeline 行为槽位 2。
    /// </summary>
    public sealed class BenchmarkPipelineBehavior2 : BenchmarkPipelineBehaviorBase
    {
    }

    /// <summary>
    ///     pipeline 行为槽位 3。
    /// </summary>
    public sealed class BenchmarkPipelineBehavior3 : BenchmarkPipelineBehaviorBase
    {
    }

    /// <summary>
    ///     pipeline 行为槽位 4。
    /// </summary>
    public sealed class BenchmarkPipelineBehavior4 : BenchmarkPipelineBehaviorBase
    {
    }
}
