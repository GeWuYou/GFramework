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
///     对比 notification 宿主在 GFramework.CQRS、NuGet `Mediator` 与 MediatR 之间的初始化与首次发布成本。
/// </summary>
/// <remarks>
///     该矩阵刻意保持“单 notification + 单 handler + 最小宿主”的对称形状，
///     只观察宿主构建与首个 publish 命中的额外开销，不把 fan-out 或自定义发布策略混入 startup 结论。
/// </remarks>
[Config(typeof(Config))]
public class NotificationStartupBenchmarks
{
    private static readonly ILogger RuntimeLogger = CreateLogger(nameof(NotificationStartupBenchmarks));
    private static readonly BenchmarkNotification Notification = new(Guid.NewGuid());

    private BenchmarkServiceHost _container = null!;
    private ICqrsRuntime _runtime = null!;
    private ServiceProvider _serviceProvider = null!;
    private IPublisher _publisher = null!;
    private ServiceProvider _mediatorServiceProvider = null!;
    private GeneratedMediator _mediator = null!;

    /// <summary>
    ///     配置 notification startup benchmark 的公共输出格式。
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
            AddColumn(new CustomColumn("Scenario", static (_, _) => "NotificationStartup"), TargetMethodColumn.Method, CategoriesColumn.Default);
            AddDiagnoser(MemoryDiagnoser.Default);
            AddLogicalGroupRules(BenchmarkLogicalGroupRule.ByCategory);
            WithOrderer(new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest, MethodOrderPolicy.Declared));
        }
    }

    /// <summary>
    ///     构建 startup benchmark 复用的最小 notification 宿主对象。
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        Fixture.Setup("NotificationStartup", handlerCount: 1, pipelineCount: 0);

        _serviceProvider = CreateMediatRServiceProvider();
        _publisher = _serviceProvider.GetRequiredService<IPublisher>();

        _mediatorServiceProvider = CreateMediatorServiceProvider();
        _mediator = _mediatorServiceProvider.GetRequiredService<GeneratedMediator>();

        _container = CreateGFrameworkContainer();
        _runtime = CreateGFrameworkRuntime(_container);
    }

    /// <summary>
    ///     在每次 cold-start 迭代前清空 dispatcher 静态缓存，确保每组 benchmark 都重新命中首次绑定路径。
    /// </summary>
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
    ///     返回已构建宿主中的 MediatR publisher，作为 initialization 组的句柄解析 baseline。
    /// </summary>
    /// <returns>当前 benchmark 复用的 MediatR publisher。</returns>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Initialization")]
    public IPublisher Initialization_MediatR()
    {
        return _publisher;
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
    ///     在新宿主上首次发布 notification，作为 MediatR 的 cold-start baseline。
    /// </summary>
    /// <returns>代表首次 publish 完成的任务。</returns>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("ColdStart")]
    public static async Task ColdStart_MediatR()
    {
        var serviceProvider = CreateMediatRServiceProvider();
        await using (serviceProvider.ConfigureAwait(false))
        {
            var publisher = serviceProvider.GetRequiredService<IPublisher>();
            await publisher.Publish(Notification, CancellationToken.None).ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     在新 runtime 上首次发布 notification，量化 GFramework.CQRS 的 first-hit 成本。
    /// </summary>
    /// <returns>代表首次 publish 完成的值任务。</returns>
    [Benchmark]
    [BenchmarkCategory("ColdStart")]
    public static async ValueTask ColdStart_GFrameworkCqrs()
    {
        var container = CreateGFrameworkContainer();
        await using (container.ConfigureAwait(false))
        {
            var runtime = CreateGFrameworkRuntime(container);
            await runtime.PublishAsync(Notification, CancellationToken.None).ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     在新的 `Mediator` 宿主上首次发布 notification，量化 source-generated concrete path 的 cold-start 成本。
    /// </summary>
    /// <returns>代表首次 publish 完成的值任务。</returns>
    [Benchmark]
    [BenchmarkCategory("ColdStart")]
    public static async ValueTask ColdStart_Mediator()
    {
        var serviceProvider = CreateMediatorServiceProvider();
        await using (serviceProvider.ConfigureAwait(false))
        {
            var mediator = serviceProvider.GetRequiredService<GeneratedMediator>();
            await mediator.Publish(Notification, CancellationToken.None).ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     构建只承载当前 benchmark notification 的最小 GFramework.CQRS runtime。
    /// </summary>
    /// <remarks>
    ///     startup benchmark 只需要验证单 handler publish 的首击路径，
    ///     因此这里继续使用单点手工注册，避免把更广泛的注册协调逻辑混入结果。
    /// </remarks>
    private static BenchmarkServiceHost CreateGFrameworkContainer()
    {
        return BenchmarkHostFactory.CreateFrozenGFrameworkContainer(static container =>
        {
            container.RegisterTransient<GFramework.Cqrs.Abstractions.Cqrs.INotificationHandler<BenchmarkNotification>, BenchmarkNotificationHandler>();
        });
    }

    /// <summary>
    ///     基于已冻结的 benchmark 容器构建最小 GFramework.CQRS runtime。
    /// </summary>
    /// <param name="container">当前 benchmark 拥有并负责释放的容器。</param>
    /// <returns>可直接发布 notification 的 runtime。</returns>
    private static ICqrsRuntime CreateGFrameworkRuntime(BenchmarkServiceHost container)
    {
        ArgumentNullException.ThrowIfNull(container);
        return GFramework.Cqrs.CqrsRuntimeFactory.CreateRuntime(container, RuntimeLogger);
    }

    /// <summary>
    ///     构建只承载当前 benchmark notification handler 的最小 MediatR 对照宿主。
    /// </summary>
    /// <returns>可直接解析 <see cref="IPublisher" /> 的 DI 宿主。</returns>
    private static ServiceProvider CreateMediatRServiceProvider()
    {
        return BenchmarkHostFactory.CreateMediatRServiceProvider(
            configure: null,
            typeof(NotificationStartupBenchmarks),
            static candidateType => candidateType == typeof(BenchmarkNotificationHandler),
            ServiceLifetime.Transient);
    }

    /// <summary>
    ///     构建只承载当前 benchmark notification handler 的最小 `Mediator` 对照宿主。
    /// </summary>
    /// <returns>可直接解析 generated `Mediator.Mediator` 的 DI 宿主。</returns>
    private static ServiceProvider CreateMediatorServiceProvider()
    {
        return BenchmarkHostFactory.CreateMediatorServiceProvider(configure: null);
    }

    /// <summary>
    ///     为 benchmark 创建安静的标准诊断 logger，避免把日志成本混入 startup 测量。
    /// </summary>
    /// <param name="categoryName">logger 分类名。</param>
    /// <returns>当前 benchmark 使用的稳定 logger。</returns>
    private static ILogger CreateLogger(string categoryName)
    {
        return BenchmarkLogging.CreateLogger(categoryName);
    }

    /// <summary>
    ///     Benchmark notification。
    /// </summary>
    /// <param name="Id">通知标识。</param>
    public sealed record BenchmarkNotification(Guid Id) :
        GFramework.Cqrs.Abstractions.Cqrs.INotification,
        Mediator.INotification,
        MediatR.INotification;

    /// <summary>
    ///     同时实现 GFramework.CQRS、NuGet `Mediator` 与 MediatR 契约的最小 notification handler。
    /// </summary>
    public sealed class BenchmarkNotificationHandler :
        GFramework.Cqrs.Abstractions.Cqrs.INotificationHandler<BenchmarkNotification>,
        Mediator.INotificationHandler<BenchmarkNotification>,
        MediatR.INotificationHandler<BenchmarkNotification>
    {
        /// <summary>
        ///     处理 GFramework.CQRS notification。
        /// </summary>
        /// <param name="notification">当前 notification。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>表示处理完成的值任务。</returns>
        public ValueTask Handle(BenchmarkNotification notification, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(notification);
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.CompletedTask;
        }

        /// <summary>
        ///     处理 NuGet `Mediator` notification。
        /// </summary>
        /// <param name="notification">当前 notification。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>表示处理完成的值任务。</returns>
        ValueTask Mediator.INotificationHandler<BenchmarkNotification>.Handle(
            BenchmarkNotification notification,
            CancellationToken cancellationToken)
        {
            return Handle(notification, cancellationToken);
        }

        /// <summary>
        ///     处理 MediatR notification。
        /// </summary>
        /// <param name="notification">当前 notification。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>表示处理完成的任务。</returns>
        Task MediatR.INotificationHandler<BenchmarkNotification>.Handle(
            BenchmarkNotification notification,
            CancellationToken cancellationToken)
        {
            return Handle(notification, cancellationToken).AsTask();
        }
    }
}
