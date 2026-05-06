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
///     对比单处理器 notification 在 GFramework.CQRS 与 MediatR 之间的 publish 开销。
/// </summary>
[Config(typeof(Config))]
public class NotificationBenchmarks
{
    private MicrosoftDiContainer _container = null!;
    private ICqrsRuntime _runtime = null!;
    private ServiceProvider _serviceProvider = null!;
    private IPublisher _publisher = null!;
    private BenchmarkNotification _notification = null!;

    /// <summary>
    ///     配置 notification benchmark 的公共输出格式。
    /// </summary>
    private sealed class Config : ManualConfig
    {
        public Config()
        {
            AddJob(Job.Default);
            AddColumnProvider(DefaultColumnProviders.Instance);
            AddColumn(new CustomColumn("Scenario", static (_, _) => "Notification"));
            AddDiagnoser(MemoryDiagnoser.Default);
            WithOrderer(new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest, MethodOrderPolicy.Declared));
        }
    }

    /// <summary>
    ///     构建 notification publish 所需的最小 runtime 宿主和对照对象。
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        LoggerFactoryResolver.Provider = new ConsoleLoggerFactoryProvider
        {
            MinLevel = LogLevel.Fatal
        };
        Fixture.Setup("Notification", handlerCount: 1, pipelineCount: 0);

        _container = BenchmarkHostFactory.CreateFrozenGFrameworkContainer(container =>
        {
            container.RegisterSingleton<GFramework.Cqrs.Abstractions.Cqrs.INotificationHandler<BenchmarkNotification>>(
                new BenchmarkNotificationHandler());
        });
        _runtime = GFramework.Cqrs.CqrsRuntimeFactory.CreateRuntime(
            _container,
            LoggerFactoryResolver.Provider.CreateLogger(nameof(NotificationBenchmarks)));

        _serviceProvider = BenchmarkHostFactory.CreateMediatRServiceProvider(
            services => services.AddSingleton<MediatR.INotificationHandler<BenchmarkNotification>, BenchmarkNotificationHandler>(),
            typeof(NotificationBenchmarks),
            static candidateType => candidateType == typeof(BenchmarkNotificationHandler),
            ServiceLifetime.Singleton);
        _publisher = _serviceProvider.GetRequiredService<IPublisher>();

        _notification = new BenchmarkNotification(Guid.NewGuid());
    }

    /// <summary>
    ///     释放 MediatR 对照组使用的 DI 宿主。
    /// </summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        BenchmarkCleanupHelper.DisposeAll(_container, _serviceProvider);
    }

    /// <summary>
    ///     通过 GFramework.CQRS runtime 发布 notification。
    /// </summary>
    [Benchmark(Baseline = true)]
    public ValueTask PublishNotification_GFrameworkCqrs()
    {
        return _runtime.PublishAsync(BenchmarkContext.Instance, _notification, CancellationToken.None);
    }

    /// <summary>
    ///     通过 MediatR 发布 notification，作为外部设计对照。
    /// </summary>
    [Benchmark]
    public Task PublishNotification_MediatR()
    {
        return _publisher.Publish(_notification, CancellationToken.None);
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
        public ValueTask Handle(BenchmarkNotification notification, CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }

        /// <summary>
        ///     处理 MediatR notification。
        /// </summary>
        Task MediatR.INotificationHandler<BenchmarkNotification>.Handle(
            BenchmarkNotification notification,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
