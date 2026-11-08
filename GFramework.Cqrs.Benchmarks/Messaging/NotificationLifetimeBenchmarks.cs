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
///     对比单处理器 notification publish 在不同 handler 生命周期下的额外开销。
/// </summary>
/// <remarks>
///     当前矩阵覆盖 <c>Singleton</c>、<c>Scoped</c> 与 <c>Transient</c>。
///     其中 <c>Scoped</c> 会在每次 notification publish 前显式创建并释放真实的 DI 作用域，
///     避免把 scoped handler 错误地压到根容器解析而扭曲生命周期对照。
/// </remarks>
[Config(typeof(Config))]
public class NotificationLifetimeBenchmarks
{
    private MicrosoftDiContainer _container = null!;
    private ICqrsRuntime? _runtime;
    private ScopedBenchmarkContainer? _scopedContainer;
    private ICqrsRuntime? _scopedRuntime;
    private ServiceProvider _serviceProvider = null!;
    private IPublisher? _publisher;
    private BenchmarkNotificationHandler _baselineHandler = null!;
    private BenchmarkNotification _notification = null!;
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
        ///     每次 publish 在显式作用域内解析并复用 handler 实例。
        /// </summary>
        Scoped,

        /// <summary>
        ///     每次 publish 都重新解析新的 handler 实例。
        /// </summary>
        Transient,
    }

    /// <summary>
    ///     配置 notification lifetime benchmark 的公共输出格式。
    /// </summary>
    private sealed class Config : ManualConfig
    {
        public Config()
        {
            AddJob(Job.Default);
            AddColumnProvider(DefaultColumnProviders.Instance);
            AddColumn(new CustomColumn("Scenario", static (_, _) => "NotificationLifetime"));
            AddDiagnoser(MemoryDiagnoser.Default);
            WithOrderer(new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest, MethodOrderPolicy.Declared));
        }
    }

    /// <summary>
    ///     构建当前生命周期下的 GFramework 与 MediatR notification 对照宿主。
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        Fixture.Setup($"NotificationLifetime/{Lifetime}", handlerCount: 1, pipelineCount: 0);
        BenchmarkDispatcherCacheHelper.ClearDispatcherCaches();

        _baselineHandler = new BenchmarkNotificationHandler();
        _notification = new BenchmarkNotification(Guid.NewGuid());
        _runtimeLogger = BenchmarkLogging.CreateLogger(nameof(NotificationLifetimeBenchmarks) + "." + Lifetime);

        _container = BenchmarkHostFactory.CreateFrozenGFrameworkContainer(container =>
        {
            RegisterGFrameworkHandler(container, Lifetime);
        });

        if (Lifetime != HandlerLifetime.Scoped)
        {
            _runtime = GFramework.Cqrs.CqrsRuntimeFactory.CreateRuntime(_container, _runtimeLogger);
        }
        else
        {
            _scopedContainer = new ScopedBenchmarkContainer(_container);
            _scopedRuntime = GFramework.Cqrs.CqrsRuntimeFactory.CreateRuntime(_scopedContainer, _runtimeLogger);
        }

        _serviceProvider = BenchmarkHostFactory.CreateMediatRServiceProvider(
            configure: null,
            typeof(NotificationLifetimeBenchmarks),
            static candidateType => candidateType == typeof(BenchmarkNotificationHandler),
            ResolveMediatRLifetime(Lifetime));
        if (Lifetime != HandlerLifetime.Scoped)
        {
            _publisher = _serviceProvider.GetRequiredService<IPublisher>();
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
            BenchmarkCleanupHelper.DisposeAll(_scopedContainer, _container, _serviceProvider);
        }
        finally
        {
            BenchmarkDispatcherCacheHelper.ClearDispatcherCaches();
        }
    }

    /// <summary>
    ///     直接调用 handler，作为不同生命周期矩阵下的 publish 额外开销 baseline。
    /// </summary>
    /// <returns>代表基线 handler 完成当前 notification 处理的值任务。</returns>
    [Benchmark(Baseline = true)]
    public ValueTask PublishNotification_Baseline()
    {
        return _baselineHandler.Handle(_notification, CancellationToken.None);
    }

    /// <summary>
    ///     通过 GFramework.CQRS runtime 发布 notification。
    /// </summary>
    /// <returns>代表当前 GFramework.CQRS publish 完成的值任务。</returns>
    [Benchmark]
    public ValueTask PublishNotification_GFrameworkCqrs()
    {
        if (Lifetime == HandlerLifetime.Scoped)
        {
            return PublishScopedGFrameworkNotificationAsync(
                _scopedRuntime!,
                _scopedContainer!,
                _notification,
                CancellationToken.None);
        }

        return _runtime!.PublishAsync(_notification, CancellationToken.None);
    }

    /// <summary>
    ///     通过 MediatR 发布 notification，作为外部对照。
    /// </summary>
    /// <returns>代表当前 MediatR publish 完成的任务。</returns>
    [Benchmark]
    public Task PublishNotification_MediatR()
    {
        if (Lifetime == HandlerLifetime.Scoped)
        {
            return PublishScopedMediatRNotificationAsync(_serviceProvider, _notification, CancellationToken.None);
        }

        return _publisher!.Publish(_notification, CancellationToken.None);
    }

    /// <summary>
    ///     按生命周期把 benchmark notification handler 注册到 GFramework 容器。
    /// </summary>
    /// <param name="container">当前 benchmark 拥有并负责释放的容器。</param>
    /// <param name="lifetime">待比较的 handler 生命周期。</param>
    private static void RegisterGFrameworkHandler(MicrosoftDiContainer container, HandlerLifetime lifetime)
    {
        ArgumentNullException.ThrowIfNull(container);

        switch (lifetime)
        {
            case HandlerLifetime.Singleton:
                container.RegisterSingleton<GFramework.Cqrs.Abstractions.Cqrs.INotificationHandler<BenchmarkNotification>, BenchmarkNotificationHandler>();
                return;

            case HandlerLifetime.Scoped:
                container.RegisterScoped<GFramework.Cqrs.Abstractions.Cqrs.INotificationHandler<BenchmarkNotification>, BenchmarkNotificationHandler>();
                return;

            case HandlerLifetime.Transient:
                container.RegisterTransient<GFramework.Cqrs.Abstractions.Cqrs.INotificationHandler<BenchmarkNotification>, BenchmarkNotificationHandler>();
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
    ///     在真实的 publish 级作用域内执行一次 GFramework.CQRS notification 分发。
    /// </summary>
    /// <param name="runtime">复用的 scoped benchmark runtime。</param>
    /// <param name="scopedContainer">负责为每次 publish 激活独立作用域的只读容器适配层。</param>
    /// <param name="notification">要发布的 notification。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>代表当前 publish 完成的值任务。</returns>
    /// <remarks>
    ///     notification lifetime benchmark 只关心 handler 解析和 publish 本身的热路径，
    ///     因此这里复用同一个 runtime，但在每次调用前后显式创建并释放新的 DI 作用域，
    ///     让 scoped handler 真正绑定到 publish 边界。
    /// </remarks>
    private static async ValueTask PublishScopedGFrameworkNotificationAsync(
        ICqrsRuntime runtime,
        ScopedBenchmarkContainer scopedContainer,
        BenchmarkNotification notification,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(scopedContainer);
        ArgumentNullException.ThrowIfNull(notification);

        using var scopeLease = scopedContainer.EnterScope();
        await runtime.PublishAsync(notification, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     在真实的 publish 级作用域内执行一次 MediatR notification 分发。
    /// </summary>
    /// <param name="rootServiceProvider">当前 benchmark 的根 <see cref="ServiceProvider" />。</param>
    /// <param name="notification">要发布的 notification。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>代表当前 publish 完成的任务。</returns>
    /// <remarks>
    ///     这里显式从新的 scope 解析 <see cref="IPublisher" />，确保 <c>Scoped</c> handler 与依赖绑定到 publish 边界。
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "MA0042",
        Justification = "This matched benchmark workload deliberately measures synchronous scope disposal on both frameworks; changing only the baseline to async disposal would alter the comparison.")]
    private static async Task PublishScopedMediatRNotificationAsync(
        ServiceProvider rootServiceProvider,
        BenchmarkNotification notification,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(rootServiceProvider);
        ArgumentNullException.ThrowIfNull(notification);

        using var scope = rootServiceProvider.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();
        await publisher.Publish(notification, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Benchmark notification。
    /// </summary>
    /// <param name="Id">通知标识。</param>
    public sealed record BenchmarkNotification(Guid Id) :
        GFramework.Cqrs.Abstractions.Cqrs.INotification,
        MediatR.INotification;

    /// <summary>
    ///     同时实现 GFramework.CQRS 与 MediatR 契约的最小 notification handler。
    /// </summary>
    public sealed class BenchmarkNotificationHandler :
        GFramework.Cqrs.Abstractions.Cqrs.INotificationHandler<BenchmarkNotification>,
        MediatR.INotificationHandler<BenchmarkNotification>
    {
        /// <summary>
        ///     处理 GFramework.CQRS notification。
        /// </summary>
        /// <param name="notification">当前要处理的 notification。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>代表当前 notification 处理完成的值任务。</returns>
        public ValueTask Handle(BenchmarkNotification notification, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(notification);
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.CompletedTask;
        }

        /// <summary>
        ///     处理 MediatR notification。
        /// </summary>
        Task MediatR.INotificationHandler<BenchmarkNotification>.Handle(
            BenchmarkNotification notification,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(notification);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }
}
