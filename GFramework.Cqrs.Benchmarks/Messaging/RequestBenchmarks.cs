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
using GFramework.Cqrs.Abstractions.Cqrs;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using GeneratedMediator = Mediator.Mediator;

namespace GFramework.Cqrs.Benchmarks.Messaging;

/// <summary>
///     对比单个 request 在直接调用、GFramework.CQRS runtime、NuGet `Mediator` 与 MediatR 之间的 steady-state dispatch 开销。
/// </summary>
[Config(typeof(Config))]
public class RequestBenchmarks
{
    private BenchmarkServiceHost _container = null!;
    private ICqrsRuntime _runtime = null!;
    private ServiceProvider _mediatrServiceProvider = null!;
    private ServiceProvider _mediatorServiceProvider = null!;
    private IMediator _mediatr = null!;
    private GeneratedMediator _mediator = null!;
    private BenchmarkRequestHandler _baselineHandler = null!;
    private BenchmarkRequest _request = null!;

    /// <summary>
    ///     配置 request benchmark 的公共输出格式。
    /// </summary>
    private sealed class Config : ManualConfig
    {
        public Config()
        {
            AddJob(Job.Default);
            AddColumnProvider(DefaultColumnProviders.Instance);
            AddColumn(new CustomColumn("Scenario", static (_, _) => "Request"));
            AddDiagnoser(MemoryDiagnoser.Default);
            WithOrderer(new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest, MethodOrderPolicy.Declared));
        }
    }

    /// <summary>
    ///     构建 request dispatch 所需的最小 runtime 宿主和对照对象。
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        Fixture.Setup("Request", handlerCount: 1, pipelineCount: 0);
        BenchmarkDispatcherCacheHelper.ClearDispatcherCaches();

        _baselineHandler = new BenchmarkRequestHandler();
        _container = BenchmarkHostFactory.CreateFrozenGFrameworkContainer(container =>
        {
            BenchmarkHostFactory.RegisterGeneratedBenchmarkRegistry<GeneratedDefaultRequestBenchmarkRegistry>(container);
        });
        _runtime = GFramework.Cqrs.CqrsRuntimeFactory.CreateRuntime(
            _container,
            BenchmarkLogging.CreateLogger(nameof(RequestBenchmarks)));

        _mediatrServiceProvider = BenchmarkHostFactory.CreateMediatRServiceProvider(
            configure: null,
            typeof(RequestBenchmarks),
            static candidateType => candidateType == typeof(BenchmarkRequestHandler),
            ServiceLifetime.Singleton);
        _mediatr = _mediatrServiceProvider.GetRequiredService<IMediator>();

        _mediatorServiceProvider = BenchmarkHostFactory.CreateMediatorServiceProvider(configure: null);
        _mediator = _mediatorServiceProvider.GetRequiredService<GeneratedMediator>();

        _request = new BenchmarkRequest(Guid.NewGuid());
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
    ///     直接调用 handler，作为 dispatch 额外开销的 baseline。
    /// </summary>
    /// <returns>代表基线 handler 完成当前 request 处理的值任务。</returns>
    [Benchmark(Baseline = true)]
    public ValueTask<BenchmarkResponse> SendRequest_Baseline()
    {
        return _baselineHandler.Handle(_request, CancellationToken.None);
    }

    /// <summary>
    ///     通过 GFramework.CQRS runtime 发送 request。
    /// </summary>
    /// <returns>代表当前 GFramework.CQRS request dispatch 完成的值任务。</returns>
    [Benchmark]
    public ValueTask<BenchmarkResponse> SendRequest_GFrameworkCqrs()
    {
        return _runtime.SendAsync(_request, CancellationToken.None);
    }

    /// <summary>
    ///     通过 MediatR 发送 request，作为外部设计对照。
    /// </summary>
    /// <returns>代表当前 MediatR request dispatch 完成的任务。</returns>
    [Benchmark]
    public Task<BenchmarkResponse> SendRequest_MediatR()
    {
        return _mediatr.Send(_request, CancellationToken.None);
    }

    /// <summary>
    ///     通过 NuGet `Mediator` 的 source-generated concrete mediator 发送 request，作为高性能对照组。
    /// </summary>
    /// <returns>代表当前 `Mediator` request dispatch 完成的值任务。</returns>
    [Benchmark]
    public ValueTask<BenchmarkResponse> SendRequest_Mediator()
    {
        return _mediator.Send(_request, CancellationToken.None);
    }

    /// <summary>
    ///     Benchmark request。
    /// </summary>
    /// <param name="Id">请求标识。</param>
    public sealed record BenchmarkRequest(Guid Id) :
        GFramework.Cqrs.Abstractions.Cqrs.IRequest<BenchmarkResponse>,
        Mediator.IRequest<BenchmarkResponse>,
        MediatR.IRequest<BenchmarkResponse>;

    /// <summary>
    ///     Benchmark response。
    /// </summary>
    /// <param name="Id">响应标识。</param>
    public sealed record BenchmarkResponse(Guid Id);

    /// <summary>
    ///     同时实现 GFramework.CQRS、NuGet `Mediator` 与 MediatR 契约的最小 request handler。
    /// </summary>
    public sealed class BenchmarkRequestHandler :
        GFramework.Cqrs.Abstractions.Cqrs.IRequestHandler<BenchmarkRequest, BenchmarkResponse>,
        Mediator.IRequestHandler<BenchmarkRequest, BenchmarkResponse>,
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
        ///     处理 NuGet `Mediator` request。
        /// </summary>
        ValueTask<BenchmarkResponse> Mediator.IRequestHandler<BenchmarkRequest, BenchmarkResponse>.Handle(
            BenchmarkRequest request,
            CancellationToken cancellationToken)
        {
            return Handle(request, cancellationToken);
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
}
