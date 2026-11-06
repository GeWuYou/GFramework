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
using GFramework.Cqrs.Notification;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using GeneratedMediator = Mediator.Mediator;

namespace GFramework.Cqrs.Benchmarks.Messaging;

/// <summary>
///     对比固定 4 个处理器的 notification fan-out publish 在 baseline、GFramework.CQRS 默认顺序发布器、
///     GFramework.CQRS 内置 <c>TaskWhenAllNotificationPublisher</c>、NuGet `Mediator` 与 MediatR 之间的开销。
/// </summary>
[Config(typeof(Config))]
public class NotificationFanOutBenchmarks
{
    private MicrosoftDiContainer _container = null!;
    private ICqrsRuntime _sequentialRuntime = null!;
    private ICqrsRuntime _taskWhenAllRuntime = null!;
    private ServiceProvider _mediatrServiceProvider = null!;
    private ServiceProvider _mediatorServiceProvider = null!;
    private IPublisher _mediatrPublisher = null!;
    private GeneratedMediator _mediator = null!;
    private BenchmarkNotification _notification = null!;
    private BenchmarkNotificationHandler1 _baselineHandler1 = null!;
    private BenchmarkNotificationHandler2 _baselineHandler2 = null!;
    private BenchmarkNotificationHandler3 _baselineHandler3 = null!;
    private BenchmarkNotificationHandler4 _baselineHandler4 = null!;

    /// <summary>
    ///     配置 notification fan-out benchmark 的公共输出格式。
    /// </summary>
    private sealed class Config : ManualConfig
    {
        public Config()
        {
            AddJob(Job.Default);
            AddColumnProvider(DefaultColumnProviders.Instance);
            AddColumn(new CustomColumn("Scenario", static (_, _) => "NotificationFanOut"));
            AddDiagnoser(MemoryDiagnoser.Default);
            WithOrderer(new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest, MethodOrderPolicy.Declared));
        }
    }

    /// <summary>
    ///     构建固定 4 处理器 notification publish 所需的最小 runtime 宿主和对照对象。
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        Fixture.Setup("NotificationFanOut", handlerCount: 4, pipelineCount: 0);

        _baselineHandler1 = new BenchmarkNotificationHandler1();
        _baselineHandler2 = new BenchmarkNotificationHandler2();
        _baselineHandler3 = new BenchmarkNotificationHandler3();
        _baselineHandler4 = new BenchmarkNotificationHandler4();

        _container = BenchmarkHostFactory.CreateFrozenGFrameworkContainer(container =>
        {
            container.RegisterSingleton<GFramework.Cqrs.Abstractions.Cqrs.INotificationHandler<BenchmarkNotification>, BenchmarkNotificationHandler1>();
            container.RegisterSingleton<GFramework.Cqrs.Abstractions.Cqrs.INotificationHandler<BenchmarkNotification>, BenchmarkNotificationHandler2>();
            container.RegisterSingleton<GFramework.Cqrs.Abstractions.Cqrs.INotificationHandler<BenchmarkNotification>, BenchmarkNotificationHandler3>();
            container.RegisterSingleton<GFramework.Cqrs.Abstractions.Cqrs.INotificationHandler<BenchmarkNotification>, BenchmarkNotificationHandler4>();
        });
        _sequentialRuntime = GFramework.Cqrs.CqrsRuntimeFactory.CreateRuntime(
            _container,
            BenchmarkLogging.CreateLogger(nameof(NotificationFanOutBenchmarks)));
        _taskWhenAllRuntime = GFramework.Cqrs.CqrsRuntimeFactory.CreateRuntime(
            _container,
            BenchmarkLogging.CreateLogger($"{nameof(NotificationFanOutBenchmarks)}.{nameof(TaskWhenAllNotificationPublisher)}"),
            new TaskWhenAllNotificationPublisher());

        _mediatrServiceProvider = BenchmarkHostFactory.CreateMediatRServiceProvider(
            services =>
            {
                services.AddSingleton<MediatR.INotificationHandler<BenchmarkNotification>, BenchmarkNotificationHandler1>();
                services.AddSingleton<MediatR.INotificationHandler<BenchmarkNotification>, BenchmarkNotificationHandler2>();
                services.AddSingleton<MediatR.INotificationHandler<BenchmarkNotification>, BenchmarkNotificationHandler3>();
                services.AddSingleton<MediatR.INotificationHandler<BenchmarkNotification>, BenchmarkNotificationHandler4>();
            },
            typeof(NotificationFanOutBenchmarks),
            static candidateType =>
                candidateType == typeof(BenchmarkNotificationHandler1) ||
                candidateType == typeof(BenchmarkNotificationHandler2) ||
                candidateType == typeof(BenchmarkNotificationHandler3) ||
                candidateType == typeof(BenchmarkNotificationHandler4),
            ServiceLifetime.Singleton);
        _mediatrPublisher = _mediatrServiceProvider.GetRequiredService<IPublisher>();

        _mediatorServiceProvider = BenchmarkHostFactory.CreateMediatorServiceProvider(configure: null);
        _mediator = _mediatorServiceProvider.GetRequiredService<GeneratedMediator>();

        _notification = new BenchmarkNotification(Guid.NewGuid());
    }

    /// <summary>
    ///     释放 MediatR 与 `Mediator` 对照组使用的 DI 宿主。
    /// </summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        BenchmarkCleanupHelper.DisposeAll(_container, _mediatrServiceProvider, _mediatorServiceProvider);
    }

    /// <summary>
    ///     直接依次调用 4 个处理器，作为 fan-out dispatch 额外开销的 baseline。
    /// </summary>
    /// <returns>代表基线顺序调用 4 个处理器完成当前 notification 处理的值任务。</returns>
    [Benchmark(Baseline = true)]
    public async ValueTask PublishNotification_Baseline()
    {
        await _baselineHandler1.Handle(_notification, CancellationToken.None).ConfigureAwait(false);
        await _baselineHandler2.Handle(_notification, CancellationToken.None).ConfigureAwait(false);
        await _baselineHandler3.Handle(_notification, CancellationToken.None).ConfigureAwait(false);
        await _baselineHandler4.Handle(_notification, CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>
    ///     通过默认顺序发布器的 GFramework.CQRS runtime 发布固定 4 处理器的 notification。
    /// </summary>
    /// <returns>代表当前默认顺序发布器 publish 完成的值任务。</returns>
    [Benchmark]
    public ValueTask PublishNotification_GFrameworkCqrsSequential()
    {
        return _sequentialRuntime.PublishAsync(_notification, CancellationToken.None);
    }

    /// <summary>
    ///     通过内置 <c>Task.WhenAll(...)</c> 发布器的 GFramework.CQRS runtime 发布固定 4 处理器的 notification。
    /// </summary>
    /// <returns>代表当前 <c>Task.WhenAll(...)</c> 发布器 publish 完成的值任务。</returns>
    [Benchmark]
    public ValueTask PublishNotification_GFrameworkCqrsTaskWhenAll()
    {
        return _taskWhenAllRuntime.PublishAsync(_notification, CancellationToken.None);
    }

    /// <summary>
    ///     通过 MediatR 发布固定 4 处理器的 notification，作为外部设计对照。
    /// </summary>
    /// <returns>代表当前 MediatR publish 完成的任务。</returns>
    [Benchmark]
    public Task PublishNotification_MediatR()
    {
        return _mediatrPublisher.Publish(_notification, CancellationToken.None);
    }

    /// <summary>
    ///     通过 `Mediator` source-generated concrete mediator 发布固定 4 处理器的 notification，作为高性能对照组。
    /// </summary>
    /// <returns>代表当前 `Mediator` publish 完成的值任务。</returns>
    [Benchmark]
    public ValueTask PublishNotification_Mediator()
    {
        return _mediator.Publish(_notification, CancellationToken.None);
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
    ///     为 fan-out benchmark 提供统一的 no-op 处理逻辑。
    /// </summary>
    public abstract class BenchmarkNotificationHandlerBase
    {
        /// <summary>
        ///     执行 benchmark 使用的最小处理逻辑。
        /// </summary>
        /// <param name="notification">当前 notification。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>已完成的值任务。</returns>
        protected static ValueTask HandleCore(BenchmarkNotification notification, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(notification);
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    ///     fan-out benchmark 的第 1 个 notification handler。
    /// </summary>
    public sealed class BenchmarkNotificationHandler1 :
        BenchmarkNotificationHandlerBase,
        GFramework.Cqrs.Abstractions.Cqrs.INotificationHandler<BenchmarkNotification>,
        Mediator.INotificationHandler<BenchmarkNotification>,
        MediatR.INotificationHandler<BenchmarkNotification>
    {
        /// <summary>
        ///     处理 GFramework.CQRS notification。
        /// </summary>
        public ValueTask Handle(BenchmarkNotification notification, CancellationToken cancellationToken)
        {
            return HandleCore(notification, cancellationToken);
        }

        /// <summary>
        ///     处理 NuGet `Mediator` notification。
        /// </summary>
        ValueTask Mediator.INotificationHandler<BenchmarkNotification>.Handle(
            BenchmarkNotification notification,
            CancellationToken cancellationToken)
        {
            return HandleCore(notification, cancellationToken);
        }

        /// <summary>
        ///     处理 MediatR notification。
        /// </summary>
        Task MediatR.INotificationHandler<BenchmarkNotification>.Handle(
            BenchmarkNotification notification,
            CancellationToken cancellationToken)
        {
            return HandleCore(notification, cancellationToken).AsTask();
        }
    }

    /// <summary>
    ///     fan-out benchmark 的第 2 个 notification handler。
    /// </summary>
    public sealed class BenchmarkNotificationHandler2 :
        BenchmarkNotificationHandlerBase,
        GFramework.Cqrs.Abstractions.Cqrs.INotificationHandler<BenchmarkNotification>,
        Mediator.INotificationHandler<BenchmarkNotification>,
        MediatR.INotificationHandler<BenchmarkNotification>
    {
        /// <summary>
        ///     处理 GFramework.CQRS notification。
        /// </summary>
        public ValueTask Handle(BenchmarkNotification notification, CancellationToken cancellationToken)
        {
            return HandleCore(notification, cancellationToken);
        }

        /// <summary>
        ///     处理 NuGet `Mediator` notification。
        /// </summary>
        ValueTask Mediator.INotificationHandler<BenchmarkNotification>.Handle(
            BenchmarkNotification notification,
            CancellationToken cancellationToken)
        {
            return HandleCore(notification, cancellationToken);
        }

        /// <summary>
        ///     处理 MediatR notification。
        /// </summary>
        Task MediatR.INotificationHandler<BenchmarkNotification>.Handle(
            BenchmarkNotification notification,
            CancellationToken cancellationToken)
        {
            return HandleCore(notification, cancellationToken).AsTask();
        }
    }

    /// <summary>
    ///     fan-out benchmark 的第 3 个 notification handler。
    /// </summary>
    public sealed class BenchmarkNotificationHandler3 :
        BenchmarkNotificationHandlerBase,
        GFramework.Cqrs.Abstractions.Cqrs.INotificationHandler<BenchmarkNotification>,
        Mediator.INotificationHandler<BenchmarkNotification>,
        MediatR.INotificationHandler<BenchmarkNotification>
    {
        /// <summary>
        ///     处理 GFramework.CQRS notification。
        /// </summary>
        public ValueTask Handle(BenchmarkNotification notification, CancellationToken cancellationToken)
        {
            return HandleCore(notification, cancellationToken);
        }

        /// <summary>
        ///     处理 NuGet `Mediator` notification。
        /// </summary>
        ValueTask Mediator.INotificationHandler<BenchmarkNotification>.Handle(
            BenchmarkNotification notification,
            CancellationToken cancellationToken)
        {
            return HandleCore(notification, cancellationToken);
        }

        /// <summary>
        ///     处理 MediatR notification。
        /// </summary>
        Task MediatR.INotificationHandler<BenchmarkNotification>.Handle(
            BenchmarkNotification notification,
            CancellationToken cancellationToken)
        {
            return HandleCore(notification, cancellationToken).AsTask();
        }
    }

    /// <summary>
    ///     fan-out benchmark 的第 4 个 notification handler。
    /// </summary>
    public sealed class BenchmarkNotificationHandler4 :
        BenchmarkNotificationHandlerBase,
        GFramework.Cqrs.Abstractions.Cqrs.INotificationHandler<BenchmarkNotification>,
        Mediator.INotificationHandler<BenchmarkNotification>,
        MediatR.INotificationHandler<BenchmarkNotification>
    {
        /// <summary>
        ///     处理 GFramework.CQRS notification。
        /// </summary>
        public ValueTask Handle(BenchmarkNotification notification, CancellationToken cancellationToken)
        {
            return HandleCore(notification, cancellationToken);
        }

        /// <summary>
        ///     处理 NuGet `Mediator` notification。
        /// </summary>
        ValueTask Mediator.INotificationHandler<BenchmarkNotification>.Handle(
            BenchmarkNotification notification,
            CancellationToken cancellationToken)
        {
            return HandleCore(notification, cancellationToken);
        }

        /// <summary>
        ///     处理 MediatR notification。
        /// </summary>
        Task MediatR.INotificationHandler<BenchmarkNotification>.Handle(
            BenchmarkNotification notification,
            CancellationToken cancellationToken)
        {
            return HandleCore(notification, cancellationToken).AsTask();
        }
    }
}
