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
///     对比 request steady-state dispatch 在不同 handler 生命周期下的额外开销。
/// </summary>
/// <remarks>
///     当前矩阵只覆盖 `Singleton` 与 `Transient`。
///     `Scoped` 在两个 runtime 中都依赖显式作用域边界，而当前 benchmark 宿主故意保持“单根容器最小宿主”模型，
///     直接把 scoped 解析压到根作用域会让对照语义失真，因此留到未来有真实 scoped host 基线时再扩展。
/// </remarks>
[Config(typeof(Config))]
public class RequestLifetimeBenchmarks
{
    private MicrosoftDiContainer _container = null!;
    private ICqrsRuntime _runtime = null!;
    private ServiceProvider _serviceProvider = null!;
    private IMediator _mediatr = null!;
    private BenchmarkRequestHandler _baselineHandler = null!;
    private BenchmarkRequest _request = null!;

    /// <summary>
    ///     控制当前 benchmark 使用的 handler 生命周期。
    /// </summary>
    [Params(HandlerLifetime.Singleton, HandlerLifetime.Transient)]
    public HandlerLifetime Lifetime { get; set; }

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
        ///     每次分发都重新解析新的 handler 实例。
        /// </summary>
        Transient
    }

    /// <summary>
    ///     配置 request lifetime benchmark 的公共输出格式。
    /// </summary>
    private sealed class Config : ManualConfig
    {
        public Config()
        {
            AddJob(Job.Default);
            AddColumnProvider(DefaultColumnProviders.Instance);
            AddColumn(new CustomColumn("Scenario", static (_, _) => "RequestLifetime"));
            AddDiagnoser(MemoryDiagnoser.Default);
            WithOrderer(new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest, MethodOrderPolicy.Declared));
        }
    }

    /// <summary>
    ///     构建当前生命周期下的 GFramework 与 MediatR request 对照宿主。
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        LoggerFactoryResolver.Provider = new ConsoleLoggerFactoryProvider
        {
            MinLevel = LogLevel.Fatal
        };
        Fixture.Setup($"RequestLifetime/{Lifetime}", handlerCount: 1, pipelineCount: 0);

        _baselineHandler = new BenchmarkRequestHandler();
        _request = new BenchmarkRequest(Guid.NewGuid());

        _container = BenchmarkHostFactory.CreateFrozenGFrameworkContainer(container =>
        {
            RegisterGFrameworkHandler(container, Lifetime);
        });
        _runtime = GFramework.Cqrs.CqrsRuntimeFactory.CreateRuntime(
            _container,
            LoggerFactoryResolver.Provider.CreateLogger(nameof(RequestLifetimeBenchmarks) + "." + Lifetime));

        _serviceProvider = BenchmarkHostFactory.CreateMediatRServiceProvider(
            configure: null,
            typeof(RequestLifetimeBenchmarks),
            static candidateType => candidateType == typeof(BenchmarkRequestHandler),
            ResolveMediatRLifetime(Lifetime));
        _mediatr = _serviceProvider.GetRequiredService<IMediator>();
    }

    /// <summary>
    ///     释放当前生命周期矩阵持有的 benchmark 宿主资源。
    /// </summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        BenchmarkCleanupHelper.DisposeAll(_container, _serviceProvider);
    }

    /// <summary>
    ///     直接调用 handler，作为不同生命周期矩阵下的 dispatch 额外开销 baseline。
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
    ///     通过 MediatR 发送 request，作为外部对照。
    /// </summary>
    [Benchmark]
    public Task<BenchmarkResponse> SendRequest_MediatR()
    {
        return _mediatr.Send(_request, CancellationToken.None);
    }

    /// <summary>
    ///     按生命周期把 benchmark request handler 注册到 GFramework 容器。
    /// </summary>
    /// <param name="container">当前 benchmark 拥有并负责释放的容器。</param>
    /// <param name="lifetime">待比较的 handler 生命周期。</param>
    private static void RegisterGFrameworkHandler(MicrosoftDiContainer container, HandlerLifetime lifetime)
    {
        ArgumentNullException.ThrowIfNull(container);

        switch (lifetime)
        {
            case HandlerLifetime.Singleton:
                container.RegisterSingleton<GFramework.Cqrs.Abstractions.Cqrs.IRequestHandler<BenchmarkRequest, BenchmarkResponse>, BenchmarkRequestHandler>();
                return;

            case HandlerLifetime.Transient:
                container.RegisterTransient<GFramework.Cqrs.Abstractions.Cqrs.IRequestHandler<BenchmarkRequest, BenchmarkResponse>, BenchmarkRequestHandler>();
                return;

            default:
                throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, "Unsupported benchmark handler lifetime.");
        }
    }

    /// <summary>
    ///     将 benchmark 生命周期映射为 MediatR 组装所需的 <see cref="ServiceLifetime" />。
    /// </summary>
    /// <param name="lifetime">待比较的 handler 生命周期。</param>
    private static ServiceLifetime ResolveMediatRLifetime(HandlerLifetime lifetime)
    {
        return lifetime switch
        {
            HandlerLifetime.Singleton => ServiceLifetime.Singleton,
            HandlerLifetime.Transient => ServiceLifetime.Transient,
            _ => throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, "Unsupported benchmark handler lifetime.")
        };
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
