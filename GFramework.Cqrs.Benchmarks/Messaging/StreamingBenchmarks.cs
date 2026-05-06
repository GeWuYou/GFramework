// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using System;
using System.Collections.Generic;
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
///     对比单个 stream request 在直接调用、GFramework.CQRS runtime 与 MediatR 之间的完整枚举开销。
/// </summary>
[Config(typeof(Config))]
public class StreamingBenchmarks
{
    private MicrosoftDiContainer _container = null!;
    private ICqrsRuntime _runtime = null!;
    private ServiceProvider _serviceProvider = null!;
    private IMediator _mediatr = null!;
    private BenchmarkStreamHandler _baselineHandler = null!;
    private BenchmarkStreamRequest _request = null!;

    /// <summary>
    ///     配置 stream benchmark 的公共输出格式。
    /// </summary>
    private sealed class Config : ManualConfig
    {
        public Config()
        {
            AddJob(Job.Default);
            AddColumnProvider(DefaultColumnProviders.Instance);
            AddColumn(new CustomColumn("Scenario", static (_, _) => "StreamRequest"));
            AddDiagnoser(MemoryDiagnoser.Default);
            WithOrderer(new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest, MethodOrderPolicy.Declared));
        }
    }

    /// <summary>
    ///     构建 stream dispatch 所需的最小 runtime 宿主和对照对象。
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        LoggerFactoryResolver.Provider = new ConsoleLoggerFactoryProvider
        {
            MinLevel = LogLevel.Fatal
        };
        Fixture.Setup("StreamRequest", handlerCount: 1, pipelineCount: 0);

        _baselineHandler = new BenchmarkStreamHandler();
        _container = BenchmarkHostFactory.CreateFrozenGFrameworkContainer(container =>
        {
            container.RegisterSingleton<GFramework.Cqrs.Abstractions.Cqrs.IStreamRequestHandler<BenchmarkStreamRequest, BenchmarkResponse>>(
                _baselineHandler);
        });
        _runtime = GFramework.Cqrs.CqrsRuntimeFactory.CreateRuntime(
            _container,
            LoggerFactoryResolver.Provider.CreateLogger(nameof(StreamingBenchmarks)));

        _serviceProvider = BenchmarkHostFactory.CreateMediatRServiceProvider(
            configure: null,
            typeof(StreamingBenchmarks),
            static candidateType => candidateType == typeof(BenchmarkStreamHandler),
            ServiceLifetime.Singleton);
        _mediatr = _serviceProvider.GetRequiredService<IMediator>();

        _request = new BenchmarkStreamRequest(Guid.NewGuid(), 3);
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
    ///     直接调用 handler 并完整枚举响应序列，作为 stream dispatch 额外开销的 baseline。
    /// </summary>
    [Benchmark(Baseline = true)]
    public async ValueTask Stream_Baseline()
    {
        await foreach (var response in _baselineHandler.Handle(_request, CancellationToken.None).ConfigureAwait(false))
        {
            _ = response;
        }
    }

    /// <summary>
    ///     通过 GFramework.CQRS runtime 创建并完整枚举 stream。
    /// </summary>
    [Benchmark]
    public async ValueTask Stream_GFrameworkCqrs()
    {
        await foreach (var response in _runtime.CreateStream(BenchmarkContext.Instance, _request, CancellationToken.None)
                           .ConfigureAwait(false))
        {
            _ = response;
        }
    }

    /// <summary>
    ///     通过 MediatR 创建并完整枚举 stream，作为外部设计对照。
    /// </summary>
    [Benchmark]
    public async ValueTask Stream_MediatR()
    {
        await foreach (var response in _mediatr.CreateStream(_request, CancellationToken.None).ConfigureAwait(false))
        {
            _ = response;
        }
    }

    /// <summary>
    ///     Benchmark stream request。
    /// </summary>
    /// <param name="Id">请求标识。</param>
    /// <param name="ItemCount">返回元素数量。</param>
    public sealed record BenchmarkStreamRequest(Guid Id, int ItemCount) :
        GFramework.Cqrs.Abstractions.Cqrs.IStreamRequest<BenchmarkResponse>,
        MediatR.IStreamRequest<BenchmarkResponse>;

    /// <summary>
    ///     复用 request benchmark 的响应结构，保持跨场景可比性。
    /// </summary>
    /// <param name="Id">响应标识。</param>
    public sealed record BenchmarkResponse(Guid Id);

    /// <summary>
    ///     同时实现 GFramework.CQRS 与 MediatR 契约的最小 stream handler。
    /// </summary>
    public sealed class BenchmarkStreamHandler :
        GFramework.Cqrs.Abstractions.Cqrs.IStreamRequestHandler<BenchmarkStreamRequest, BenchmarkResponse>,
        MediatR.IStreamRequestHandler<BenchmarkStreamRequest, BenchmarkResponse>
    {
        /// <summary>
        ///     处理 GFramework.CQRS stream request。
        /// </summary>
        public IAsyncEnumerable<BenchmarkResponse> Handle(
            BenchmarkStreamRequest request,
            CancellationToken cancellationToken)
        {
            return EnumerateAsync(request, cancellationToken);
        }

        /// <summary>
        ///     处理 MediatR stream request。
        /// </summary>
        IAsyncEnumerable<BenchmarkResponse> MediatR.IStreamRequestHandler<BenchmarkStreamRequest, BenchmarkResponse>.Handle(
            BenchmarkStreamRequest request,
            CancellationToken cancellationToken)
        {
            return EnumerateAsync(request, cancellationToken);
        }

        /// <summary>
        ///     为 benchmark 构造稳定、低噪声的异步响应序列。
        /// </summary>
        private static async IAsyncEnumerable<BenchmarkResponse> EnumerateAsync(
            BenchmarkStreamRequest request,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            for (int index = 0; index < request.ItemCount; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return new BenchmarkResponse(request.Id);
                await Task.CompletedTask.ConfigureAwait(false);
            }
        }
    }
}
