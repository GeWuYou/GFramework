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
///     对比单个 request 在直接调用、GFramework.CQRS runtime 与 MediatR 之间的 steady-state dispatch 开销。
/// </summary>
[Config(typeof(Config))]
public class RequestBenchmarks
{
    private MicrosoftDiContainer _container = null!;
    private ICqrsRuntime _runtime = null!;
    private ServiceProvider _serviceProvider = null!;
    private IMediator _mediatr = null!;
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
        LoggerFactoryResolver.Provider = new ConsoleLoggerFactoryProvider
        {
            MinLevel = LogLevel.Fatal
        };
        Fixture.Setup("Request", handlerCount: 1, pipelineCount: 0);

        _baselineHandler = new BenchmarkRequestHandler();
        _container = BenchmarkHostFactory.CreateFrozenGFrameworkContainer(container =>
        {
            container.RegisterSingleton<GFramework.Cqrs.Abstractions.Cqrs.IRequestHandler<BenchmarkRequest, BenchmarkResponse>>(
                _baselineHandler);
        });
        _runtime = GFramework.Cqrs.CqrsRuntimeFactory.CreateRuntime(
            _container,
            LoggerFactoryResolver.Provider.CreateLogger(nameof(RequestBenchmarks)));

        _serviceProvider = BenchmarkHostFactory.CreateMediatRServiceProvider(
            configure: null,
            typeof(RequestBenchmarks),
            static candidateType => candidateType == typeof(BenchmarkRequestHandler),
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
    }

    /// <summary>
    ///     直接调用 handler，作为 dispatch 额外开销的 baseline。
    /// </summary>
    [Benchmark(Baseline = true)]
    public ValueTask<BenchmarkResponse> SendRequest_Baseline()
    {
        return _baselineHandler.Handle(_request, CancellationToken.None);
    }

    /// <summary>
    ///     通过 GFramework.CQRS runtime 发送 request。
    /// </summary>
    [Benchmark]
    public ValueTask<BenchmarkResponse> SendRequest_GFrameworkCqrs()
    {
        return _runtime.SendAsync(BenchmarkContext.Instance, _request, CancellationToken.None);
    }

    /// <summary>
    ///     通过 MediatR 发送 request，作为外部设计对照。
    /// </summary>
    [Benchmark]
    public Task<BenchmarkResponse> SendRequest_MediatR()
    {
        return _mediatr.Send(_request, CancellationToken.None);
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
}
