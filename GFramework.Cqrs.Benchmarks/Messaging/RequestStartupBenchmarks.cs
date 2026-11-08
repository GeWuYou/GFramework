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
using Microsoft.Extensions.Logging;
using GeneratedMediator = Mediator.Mediator;

namespace GFramework.Cqrs.Benchmarks.Messaging;

/// <summary>
///     对比 request 宿主在 GFramework.CQRS、NuGet `Mediator` 与 MediatR 之间的初始化与首次分发成本。
/// </summary>
[Config(typeof(Config))]
public class RequestStartupBenchmarks
{
    private static readonly ILogger RuntimeLogger = CreateLogger(nameof(RequestStartupBenchmarks));
    private static readonly BenchmarkRequest Request = new(Guid.NewGuid());

    private BenchmarkServiceHost _container = null!;
    private ServiceProvider _serviceProvider = null!;
    private ServiceProvider _mediatorServiceProvider = null!;
    private IMediator _mediatr = null!;
    private GeneratedMediator _mediator = null!;
    private ICqrsRuntime _runtime = null!;

    /// <summary>
    ///     配置 request startup benchmark 的公共输出格式。
    /// </summary>
    private sealed class Config : ManualConfig
    {
        public Config()
        {
            AddJob(Job.Default
                .WithId("ColdStart")
                .WithInvocationCount(1)
                .WithUnrollFactor(1));
            AddColumnProvider(DefaultColumnProviders.Instance);
            AddColumn(new CustomColumn("Scenario", static (_, _) => "RequestStartup"), TargetMethodColumn.Method, CategoriesColumn.Default);
            AddDiagnoser(MemoryDiagnoser.Default);
            AddLogicalGroupRules(BenchmarkLogicalGroupRule.ByCategory);
            WithOrderer(new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest, MethodOrderPolicy.Declared));
        }
    }

    /// <summary>
    ///     构建 steady-state 初始化 benchmark 复用的宿主对象。
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        Fixture.Setup("RequestStartup", handlerCount: 1, pipelineCount: 0);

        _serviceProvider = CreateMediatRServiceProvider();
        _mediatr = _serviceProvider.GetRequiredService<IMediator>();
        _mediatorServiceProvider = CreateMediatorServiceProvider();
        _mediator = _mediatorServiceProvider.GetRequiredService<GeneratedMediator>();
        _container = CreateGFrameworkContainer();
        _runtime = CreateGFrameworkRuntime(_container);
    }

    /// <summary>
    ///     在每次 cold-start 迭代前清空 dispatcher 静态缓存，确保两组 benchmark 都重新命中首次绑定路径。
    /// </summary>
    /// <remarks>
    ///     使用 `IterationSetup` 而不是把缓存清理写在 benchmark 方法主体中，
    ///     可以把“清理静态缓存”留在测量边界之外，只保留宿主构建与首次发送本身。
    /// </remarks>
    [IterationSetup]
    public static void ResetColdStartCaches()
    {
        BenchmarkDispatcherCacheHelper.ClearDispatcherCaches();
    }

    /// <summary>
    ///     释放 startup benchmark 复用的宿主对象。
    /// </summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        BenchmarkCleanupHelper.DisposeAll(_container, _serviceProvider, _mediatorServiceProvider);
    }

    /// <summary>
    ///     返回已构建宿主中的 MediatR mediator，作为 initialization 组的句柄解析 baseline。
    /// </summary>
    /// <returns>当前 benchmark 复用的 MediatR mediator。</returns>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Initialization")]
    public IMediator Initialization_MediatR()
    {
        return _mediatr;
    }

    /// <summary>
    ///     返回已构建宿主中的 GFramework.CQRS runtime，确保与 MediatR baseline 处于相同初始化阶段。
    /// </summary>
    /// <returns>当前 benchmark 复用的 GFramework.CQRS runtime。</returns>
    [Benchmark]
    [BenchmarkCategory("Initialization")]
    public ICqrsRuntime Initialization_GFrameworkCqrs()
    {
        return _runtime;
    }

    /// <summary>
    ///     返回已构建宿主中的 `Mediator` concrete mediator，作为 source-generated 对照组的初始化句柄。
    /// </summary>
    /// <returns>当前 benchmark 复用的 `Mediator` concrete mediator。</returns>
    [Benchmark]
    [BenchmarkCategory("Initialization")]
    public GeneratedMediator Initialization_Mediator()
    {
        return _mediator;
    }

    /// <summary>
    ///     在新宿主上首次发送 request，作为 MediatR 的 cold-start baseline。
    /// </summary>
    /// <returns>当前 request 的响应结果。</returns>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("ColdStart")]
    public static async Task<BenchmarkResponse> ColdStart_MediatR()
    {
        var serviceProvider = CreateMediatRServiceProvider();
        await using (serviceProvider.ConfigureAwait(false))
        {
            var mediator = serviceProvider.GetRequiredService<IMediator>();
            return await mediator.Send(Request, CancellationToken.None).ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     在新 runtime 上首次发送 request，量化 GFramework.CQRS 的 first-hit 成本。
    /// </summary>
    /// <returns>当前 request 的响应结果。</returns>
    [Benchmark]
    [BenchmarkCategory("ColdStart")]
    public static async ValueTask<BenchmarkResponse> ColdStart_GFrameworkCqrs()
    {
        var container = CreateGFrameworkContainer();
        await using (container.ConfigureAwait(false))
        {
            var runtime = CreateGFrameworkRuntime(container);
            return await runtime.SendAsync(Request, CancellationToken.None).ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     在新的 `Mediator` 宿主上首次发送 request，量化 source-generated concrete path 的 cold-start 成本。
    /// </summary>
    /// <returns>当前 request 的响应结果。</returns>
    [Benchmark]
    [BenchmarkCategory("ColdStart")]
    public static async ValueTask<BenchmarkResponse> ColdStart_Mediator()
    {
        var serviceProvider = CreateMediatorServiceProvider();
        await using (serviceProvider.ConfigureAwait(false))
        {
            var mediator = serviceProvider.GetRequiredService<GeneratedMediator>();
            return await mediator.Send(Request, CancellationToken.None).ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     构建只承载当前 benchmark request 的最小 GFramework.CQRS runtime。
    /// </summary>
    /// <remarks>
    ///     该 benchmark 故意保持与 MediatR 对照组同样的“单 handler 最小宿主”模型，
    ///     因此这里继续使用单点手工注册，而不引入依赖完整 CQRS 注册协调器的程序集扫描路径。
    /// </remarks>
    private static BenchmarkServiceHost CreateGFrameworkContainer()
    {
        return BenchmarkHostFactory.CreateFrozenGFrameworkContainer(static currentContainer =>
        {
            currentContainer.RegisterTransient<GFramework.Cqrs.Abstractions.Cqrs.IRequestHandler<BenchmarkRequest, BenchmarkResponse>, BenchmarkRequestHandler>();
        });
    }

    /// <summary>
    ///     基于已冻结的 benchmark 容器构建最小 GFramework.CQRS runtime。
    /// </summary>
    /// <param name="container">当前 benchmark 拥有并负责释放的容器。</param>
    private static ICqrsRuntime CreateGFrameworkRuntime(BenchmarkServiceHost container)
    {
        ArgumentNullException.ThrowIfNull(container);
        return GFramework.Cqrs.CqrsRuntimeFactory.CreateRuntime(container, RuntimeLogger);
    }

    /// <summary>
    ///     构建只承载当前 benchmark request 的最小 MediatR 对照宿主。
    /// </summary>
    private static ServiceProvider CreateMediatRServiceProvider()
    {
        return BenchmarkHostFactory.CreateMediatRServiceProvider(
            configure: null,
            typeof(RequestStartupBenchmarks),
            static candidateType => candidateType == typeof(BenchmarkRequestHandler),
            ServiceLifetime.Transient);
    }

    /// <summary>
    ///     构建只承载当前 benchmark request 的最小 `Mediator` 对照宿主。
    /// </summary>
    private static ServiceProvider CreateMediatorServiceProvider()
    {
        return BenchmarkHostFactory.CreateMediatorServiceProvider(configure: null);
    }

    /// <summary>
    ///     为 benchmark 创建安静的标准诊断 logger，避免把日志成本混入 startup 测量。
    /// </summary>
    private static ILogger CreateLogger(string categoryName)
    {
        return BenchmarkLogging.CreateLogger(categoryName);
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
