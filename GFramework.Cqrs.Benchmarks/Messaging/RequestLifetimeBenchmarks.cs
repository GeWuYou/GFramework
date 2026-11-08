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
using GFramework.Core.Ioc;
using GFramework.Cqrs.Abstractions.Cqrs;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GFramework.Cqrs.Benchmarks.Messaging;

/// <summary>
///     对比 request steady-state dispatch 在不同 handler 生命周期下的额外开销。
/// </summary>
/// <remarks>
///     当前矩阵覆盖 `Singleton`、`Scoped` 与 `Transient`。
///     其中 `Scoped` 会在每次 request 分发时显式创建并释放真实的 DI 作用域，
///     避免把 scoped handler 错误地压到根容器解析而扭曲生命周期对照。
///     NuGet `Mediator` 的 DI lifetime 由 source generator 在编译期固定到整个 benchmark 项目，
///     不能在同一份生成产物里同时切换 `Singleton`、`Scoped` 与 `Transient`。
///     因此该矩阵当前只比较 `GFramework.Cqrs` 与 `MediatR` 的生命周期开销；`Mediator` 仍保留在其他
///     steady-state / startup benchmark 中作为单一 compile-time 形状对照。
/// </remarks>
[Config(typeof(Config))]
public class RequestLifetimeBenchmarks
{
    private MicrosoftDiContainer _container = null!;
    private ICqrsRuntime? _runtime;
    private ScopedBenchmarkContainer? _scopedContainer;
    private ICqrsRuntime? _scopedRuntime;
    private ServiceProvider _serviceProvider = null!;
    private IMediator? _mediatr;
    private BenchmarkRequestHandler _baselineHandler = null!;
    private BenchmarkRequest _request = null!;
    private ILogger _runtimeLogger = null!;

    /// <summary>
    ///     控制当前 benchmark 使用的 handler 生命周期。
    /// </summary>
    [Params(HandlerLifetime.Singleton, HandlerLifetime.Scoped, HandlerLifetime.Transient)]
    public HandlerLifetime Lifetime
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
        ///     每次 request 在显式作用域内解析并复用 handler 实例。
        /// </summary>
        Scoped,

        /// <summary>
        ///     每次分发都重新解析新的 handler 实例。
        /// </summary>
        Transient,
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
        Fixture.Setup($"RequestLifetime/{Lifetime}", handlerCount: 1, pipelineCount: 0);
        BenchmarkDispatcherCacheHelper.ClearDispatcherCaches();

        _baselineHandler = new BenchmarkRequestHandler();
        _request = new BenchmarkRequest(Guid.NewGuid());

        _runtimeLogger = BenchmarkLogging.CreateLogger(nameof(RequestLifetimeBenchmarks) + "." + Lifetime);

        _container = BenchmarkHostFactory.CreateFrozenGFrameworkContainer(container =>
        {
            BenchmarkHostFactory.RegisterGeneratedBenchmarkRegistry<GeneratedRequestLifetimeBenchmarkRegistry>(container);
            RegisterGFrameworkHandler(container, Lifetime);
        });
        // runtime 在 provider 构建后按当前矩阵惰性解析；
        // 这里显式创建带生命周期后缀的 runtime，只是为了区分不同 benchmark 矩阵的 dispatcher 日志。
        if (Lifetime != HandlerLifetime.Scoped)
        {
            _runtime = GFramework.Cqrs.CqrsRuntimeFactory.CreateRuntime(
                _container,
                _runtimeLogger);
        }
        else
        {
            _scopedContainer = new ScopedBenchmarkContainer(_container);
            _scopedRuntime = GFramework.Cqrs.CqrsRuntimeFactory.CreateRuntime(
                _scopedContainer,
                _runtimeLogger);
        }

        _serviceProvider = BenchmarkHostFactory.CreateMediatRServiceProvider(
            configure: null,
            typeof(RequestLifetimeBenchmarks),
            static candidateType => candidateType == typeof(BenchmarkRequestHandler),
            ResolveMediatRLifetime(Lifetime));
        if (Lifetime != HandlerLifetime.Scoped)
        {
            _mediatr = _serviceProvider.GetRequiredService<IMediator>();
        }
    }

    /// <summary>
    ///     释放当前生命周期矩阵持有的 benchmark 宿主资源。
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
    ///     直接调用 handler，作为不同生命周期矩阵下的 dispatch 额外开销 baseline。
    /// </summary>
    /// <returns>代表基线 request handler 完成当前 request 处理的值任务。</returns>
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
        if (Lifetime == HandlerLifetime.Scoped)
        {
            return BenchmarkHostFactory.SendScopedGFrameworkRequestAsync(
                _scopedRuntime!,
                _scopedContainer!,
                _request,
                CancellationToken.None);
        }

        return _runtime!.SendAsync(_request, CancellationToken.None);
    }

    /// <summary>
    ///     通过 MediatR 发送 request，作为外部对照。
    /// </summary>
    /// <returns>代表当前 MediatR request dispatch 完成的任务。</returns>
    [Benchmark]
    public Task<BenchmarkResponse> SendRequest_MediatR()
    {
        if (Lifetime == HandlerLifetime.Scoped)
        {
            return BenchmarkHostFactory.SendScopedMediatRRequestAsync(
                _serviceProvider,
                _request,
                CancellationToken.None);
        }

        return _mediatr!.Send(_request, CancellationToken.None);
    }

    /// <summary>
    ///     按生命周期把 benchmark request handler 注册到 GFramework 容器。
    /// </summary>
    /// <param name="container">当前 benchmark 拥有并负责释放的容器。</param>
    /// <param name="lifetime">待比较的 handler 生命周期。</param>
    /// <remarks>
    ///     先通过 generated registry 提供静态 descriptor，再显式覆盖 handler 生命周期，
    ///     可以把比较变量收敛到 handler 解析成本，而不是 descriptor 发现路径本身。
    /// </remarks>
    private static void RegisterGFrameworkHandler(MicrosoftDiContainer container, HandlerLifetime lifetime)
    {
        ArgumentNullException.ThrowIfNull(container);

        switch (lifetime)
        {
            case HandlerLifetime.Singleton:
                container.RegisterSingleton<GFramework.Cqrs.Abstractions.Cqrs.IRequestHandler<BenchmarkRequest, BenchmarkResponse>, BenchmarkRequestHandler>();
                return;

            case HandlerLifetime.Scoped:
                container.RegisterScoped<GFramework.Cqrs.Abstractions.Cqrs.IRequestHandler<BenchmarkRequest, BenchmarkResponse>, BenchmarkRequestHandler>();
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
            HandlerLifetime.Scoped => ServiceLifetime.Scoped,
            HandlerLifetime.Transient => ServiceLifetime.Transient,
            _ => throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, "Unsupported benchmark handler lifetime."),
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
