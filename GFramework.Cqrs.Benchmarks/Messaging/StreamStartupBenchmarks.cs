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
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using GFramework.Core.Ioc;
using GFramework.Cqrs.Abstractions.Cqrs;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using GeneratedMediator = Mediator.Mediator;

[assembly: GFramework.Cqrs.CqrsHandlerRegistryAttribute(
    typeof(GFramework.Cqrs.Benchmarks.Messaging.StreamStartupBenchmarks.GeneratedRegistry))]

namespace GFramework.Cqrs.Benchmarks.Messaging;

/// <summary>
///     对比 stream 宿主在 GFramework.CQRS reflection / generated、NuGet `Mediator` 与 MediatR 之间的初始化与首次建流命中成本。
/// </summary>
/// <remarks>
///     该场景与 <see cref="RequestStartupBenchmarks" /> 保持相同的 `Initialization + ColdStart` 结构，
///     但 cold-start 边界改为“新宿主 + 首个元素命中”，因为 stream 的首个 <c>MoveNextAsync</c>
///     才会真正覆盖建流后的首次处理链路。
/// </remarks>
[Config(typeof(Config))]
public class StreamStartupBenchmarks
{
    private static readonly ILogger ReflectionRuntimeLogger = CreateLogger(nameof(StreamStartupBenchmarks) + ".Reflection");
    private static readonly ILogger GeneratedRuntimeLogger = CreateLogger(nameof(StreamStartupBenchmarks) + ".Generated");
    private static readonly BenchmarkStreamRequest Request = new(Guid.NewGuid(), 3);

    private MicrosoftDiContainer _reflectionContainer = null!;
    private ICqrsRuntime _reflectionRuntime = null!;
    private MicrosoftDiContainer _generatedContainer = null!;
    private ICqrsRuntime _generatedRuntime = null!;
    private ServiceProvider _serviceProvider = null!;
    private ServiceProvider _mediatorServiceProvider = null!;
    private IMediator _mediatr = null!;
    private GeneratedMediator _mediator = null!;

    /// <summary>
    ///     配置 stream startup benchmark 的公共输出格式。
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
            AddColumn(new CustomColumn("Scenario", static (_, _) => "StreamStartup"), TargetMethodColumn.Method, CategoriesColumn.Default);
            AddDiagnoser(MemoryDiagnoser.Default);
            AddLogicalGroupRules(BenchmarkLogicalGroupRule.ByCategory);
            WithOrderer(new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest, MethodOrderPolicy.Declared));
        }
    }

    /// <summary>
    ///     构建 startup benchmark 复用的 reflection / generated / `Mediator` / MediatR 宿主对象。
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        Fixture.Setup("StreamStartup", handlerCount: 1, pipelineCount: 0);

        _reflectionContainer = CreateReflectionContainer();
        _reflectionRuntime = CreateRuntime(_reflectionContainer, ReflectionRuntimeLogger);

        _generatedContainer = CreateGeneratedContainer();
        _generatedRuntime = CreateRuntime(_generatedContainer, GeneratedRuntimeLogger);

        _serviceProvider = CreateMediatRServiceProvider();
        _mediatr = _serviceProvider.GetRequiredService<IMediator>();
        _mediatorServiceProvider = CreateMediatorServiceProvider();
        _mediator = _mediatorServiceProvider.GetRequiredService<GeneratedMediator>();
    }

    /// <summary>
    ///     在每次 cold-start 迭代前清空 dispatcher 静态缓存，确保首次绑定路径可重复观察。
    /// </summary>
    [IterationSetup]
    public void ResetColdStartCaches()
    {
        BenchmarkDispatcherCacheHelper.ClearDispatcherCaches();
    }

    /// <summary>
    ///     释放 startup benchmark 复用的宿主对象。
    /// </summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        BenchmarkCleanupHelper.DisposeAll(_reflectionContainer, _generatedContainer, _serviceProvider, _mediatorServiceProvider);
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
    ///     返回已构建宿主中的 GFramework.CQRS reflection runtime，观察默认 stream binding 宿主句柄解析成本。
    /// </summary>
    /// <returns>当前 benchmark 复用的 reflection CQRS runtime。</returns>
    [Benchmark]
    [BenchmarkCategory("Initialization")]
    public ICqrsRuntime Initialization_GFrameworkReflection()
    {
        return _reflectionRuntime;
    }

    /// <summary>
    ///     返回已构建宿主中的 GFramework.CQRS generated runtime，观察 generated stream invoker 宿主句柄解析成本。
    /// </summary>
    /// <returns>当前 benchmark 复用的 generated CQRS runtime。</returns>
    [Benchmark]
    [BenchmarkCategory("Initialization")]
    public ICqrsRuntime Initialization_GFrameworkGenerated()
    {
        return _generatedRuntime;
    }

    /// <summary>
    ///     返回已构建宿主中的 `Mediator` concrete mediator，作为 source-generated concrete path 的初始化句柄。
    /// </summary>
    /// <returns>当前 benchmark 复用的 `Mediator` concrete mediator。</returns>
    [Benchmark]
    [BenchmarkCategory("Initialization")]
    public GeneratedMediator Initialization_Mediator()
    {
        return _mediator;
    }

    /// <summary>
    ///     在新宿主上首次创建并推进 stream，作为 MediatR 的 cold-start baseline。
    /// </summary>
    /// <returns>首个 stream 响应元素。</returns>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("ColdStart")]
    public async Task<BenchmarkResponse> ColdStart_MediatR()
    {
        using var serviceProvider = CreateMediatRServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        return await ConsumeFirstItemAsync(mediator.CreateStream(Request, CancellationToken.None), CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>
    ///     在新的 reflection runtime 上首次创建并推进 stream，量化默认 stream binding 的 first-hit 成本。
    /// </summary>
    /// <returns>首个 stream 响应元素。</returns>
    [Benchmark]
    [BenchmarkCategory("ColdStart")]
    public async ValueTask<BenchmarkResponse> ColdStart_GFrameworkReflection()
    {
        using var container = CreateReflectionContainer();
        var runtime = CreateRuntime(container, ReflectionRuntimeLogger);
        return await ConsumeFirstItemAsync(
                runtime.CreateStream(Request, CancellationToken.None),
                CancellationToken.None)
            .ConfigureAwait(false);
    }

    /// <summary>
    ///     在新的 generated runtime 上首次创建并推进 stream，量化 generated stream invoker 路径的 first-hit 成本。
    /// </summary>
    /// <returns>首个 stream 响应元素。</returns>
    [Benchmark]
    [BenchmarkCategory("ColdStart")]
    public async ValueTask<BenchmarkResponse> ColdStart_GFrameworkGenerated()
    {
        using var container = CreateGeneratedContainer();
        var runtime = CreateRuntime(container, GeneratedRuntimeLogger);
        return await ConsumeFirstItemAsync(
                runtime.CreateStream(Request, CancellationToken.None),
                CancellationToken.None)
            .ConfigureAwait(false);
    }

    /// <summary>
    ///     在新的 `Mediator` 宿主上首次创建并推进 stream，量化 source-generated concrete path 的 first-hit 成本。
    /// </summary>
    /// <returns>首个 stream 响应元素。</returns>
    [Benchmark]
    [BenchmarkCategory("ColdStart")]
    public async ValueTask<BenchmarkResponse> ColdStart_Mediator()
    {
        using var serviceProvider = CreateMediatorServiceProvider();
        var mediator = serviceProvider.GetRequiredService<GeneratedMediator>();
        return await ConsumeFirstItemAsync(mediator.CreateStream(Request, CancellationToken.None), CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>
    ///     构建只承载当前 benchmark handler 的最小 reflection GFramework.CQRS 容器。
    /// </summary>
    private static MicrosoftDiContainer CreateReflectionContainer()
    {
        return BenchmarkHostFactory.CreateFrozenGFrameworkContainer(static container =>
        {
            container.RegisterTransient<
                GFramework.Cqrs.Abstractions.Cqrs.IStreamRequestHandler<BenchmarkStreamRequest, BenchmarkResponse>,
                BenchmarkStreamHandler>();
        });
    }

    /// <summary>
    ///     构建只承载当前 benchmark generated registry 的最小 generated GFramework.CQRS 容器。
    /// </summary>
    private static MicrosoftDiContainer CreateGeneratedContainer()
    {
        return BenchmarkHostFactory.CreateFrozenGFrameworkContainer(static container =>
        {
            BenchmarkHostFactory.RegisterGeneratedBenchmarkRegistry<GeneratedRegistry>(container);
        });
    }

    /// <summary>
    ///     基于已冻结的 benchmark 容器构建最小 GFramework.CQRS runtime。
    /// </summary>
    /// <param name="container">当前 benchmark 拥有并负责释放的容器。</param>
    /// <param name="logger">当前 runtime 使用的 benchmark logger。</param>
    private static ICqrsRuntime CreateRuntime(MicrosoftDiContainer container, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(logger);

        return GFramework.Cqrs.CqrsRuntimeFactory.CreateRuntime(container, logger);
    }

    /// <summary>
    ///     构建只承载当前 benchmark handler 的最小 MediatR 对照宿主。
    /// </summary>
    private static ServiceProvider CreateMediatRServiceProvider()
    {
        return BenchmarkHostFactory.CreateMediatRServiceProvider(
            configure: null,
            typeof(StreamStartupBenchmarks),
            static candidateType => candidateType == typeof(BenchmarkStreamHandler),
            ServiceLifetime.Transient);
    }

    /// <summary>
    ///     构建只承载当前 benchmark handler 的最小 `Mediator` 对照宿主。
    /// </summary>
    private static ServiceProvider CreateMediatorServiceProvider()
    {
        return BenchmarkHostFactory.CreateMediatorServiceProvider(configure: null);
    }

    /// <summary>
    ///     推进 stream 到首个元素，并返回该元素作为 cold-start 结果。
    /// </summary>
    /// <typeparam name="TResponse">当前 stream 的响应类型。</typeparam>
    /// <param name="responses">待推进的异步响应序列。</param>
    /// <param name="cancellationToken">用于向异步枚举器传播取消的令牌。</param>
    /// <returns>首个元素。</returns>
    /// <exception cref="InvalidOperationException">stream 未产生任何元素。</exception>
    private static async ValueTask<TResponse> ConsumeFirstItemAsync<TResponse>(
        IAsyncEnumerable<TResponse> responses,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(responses);

        var enumerator = responses.GetAsyncEnumerator(cancellationToken);
        await using (enumerator.ConfigureAwait(false))
        {
            if (await enumerator.MoveNextAsync().ConfigureAwait(false))
            {
                return enumerator.Current;
            }
        }

        throw new InvalidOperationException("The benchmark stream must yield at least one response.");
    }

    /// <summary>
    ///     为 benchmark 创建安静的标准诊断 logger，避免把日志成本混入 startup 测量。
    /// </summary>
    private static ILogger CreateLogger(string categoryName)
    {
        return BenchmarkLogging.CreateLogger(categoryName);
    }

    /// <summary>
    ///     Benchmark stream request。
    /// </summary>
    /// <param name="Id">请求标识。</param>
    /// <param name="ItemCount">返回元素数量。</param>
    public sealed record BenchmarkStreamRequest(Guid Id, int ItemCount) :
        GFramework.Cqrs.Abstractions.Cqrs.IStreamRequest<BenchmarkResponse>,
        Mediator.IStreamRequest<BenchmarkResponse>,
        MediatR.IStreamRequest<BenchmarkResponse>;

    /// <summary>
    ///     Benchmark stream response。
    /// </summary>
    /// <param name="Id">响应标识。</param>
    public sealed record BenchmarkResponse(Guid Id);

    /// <summary>
    ///     同时实现 GFramework.CQRS、NuGet `Mediator` 与 MediatR 契约的最小 stream handler。
    /// </summary>
    public sealed class BenchmarkStreamHandler :
        GFramework.Cqrs.Abstractions.Cqrs.IStreamRequestHandler<BenchmarkStreamRequest, BenchmarkResponse>,
        Mediator.IStreamRequestHandler<BenchmarkStreamRequest, BenchmarkResponse>,
        MediatR.IStreamRequestHandler<BenchmarkStreamRequest, BenchmarkResponse>
    {
        /// <summary>
        ///     处理 GFramework.CQRS stream request。
        /// </summary>
        /// <param name="request">当前 stream 请求。</param>
        /// <param name="cancellationToken">用于中断异步枚举的取消令牌。</param>
        /// <returns>按请求元素数量延迟生成的异步响应序列。</returns>
        public IAsyncEnumerable<BenchmarkResponse> Handle(
            BenchmarkStreamRequest request,
            CancellationToken cancellationToken)
        {
            return EnumerateAsync(request, cancellationToken);
        }

        /// <summary>
        ///     处理 NuGet `Mediator` stream request。
        /// </summary>
        /// <param name="request">当前 stream 请求。</param>
        /// <param name="cancellationToken">用于中断异步枚举的取消令牌。</param>
        /// <returns>按请求元素数量延迟生成的异步响应序列。</returns>
        IAsyncEnumerable<BenchmarkResponse> Mediator.IStreamRequestHandler<BenchmarkStreamRequest, BenchmarkResponse>.Handle(
            BenchmarkStreamRequest request,
            CancellationToken cancellationToken)
        {
            return Handle(request, cancellationToken);
        }

        /// <summary>
        ///     处理 MediatR stream request。
        /// </summary>
        /// <param name="request">当前 stream 请求。</param>
        /// <param name="cancellationToken">用于中断异步枚举的取消令牌。</param>
        /// <returns>按请求元素数量延迟生成的异步响应序列。</returns>
        IAsyncEnumerable<BenchmarkResponse> MediatR.IStreamRequestHandler<BenchmarkStreamRequest, BenchmarkResponse>.Handle(
            BenchmarkStreamRequest request,
            CancellationToken cancellationToken)
        {
            return EnumerateAsync(request, cancellationToken);
        }

        /// <summary>
        ///     生成固定长度的 benchmark stream，确保 cold-start 与 steady-state 维度共用同一份响应形状。
        /// </summary>
        /// <param name="request">当前 stream 请求。</param>
        /// <param name="cancellationToken">用于向异步枚举器传播取消的令牌。</param>
        /// <returns>按请求数量生成的异步响应序列。</returns>
        private static async IAsyncEnumerable<BenchmarkResponse> EnumerateAsync(
            BenchmarkStreamRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            for (var index = 0; index < request.ItemCount; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return new BenchmarkResponse(request.Id);
                await Task.CompletedTask.ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    ///     为 stream startup benchmark 提供 hand-written generated registry，
    ///     以便独立比较 generated stream invoker 的初始化与首次命中成本。
    /// </summary>
    public sealed class GeneratedRegistry :
        GFramework.Cqrs.ICqrsHandlerRegistry,
        GFramework.Cqrs.ICqrsStreamInvokerProvider,
        GFramework.Cqrs.IEnumeratesCqrsStreamInvokerDescriptors
    {
        private static readonly GFramework.Cqrs.CqrsStreamInvokerDescriptor Descriptor =
            new(
                typeof(GFramework.Cqrs.Abstractions.Cqrs.IStreamRequestHandler<
                    BenchmarkStreamRequest,
                    BenchmarkResponse>),
                typeof(GeneratedRegistry).GetMethod(
                    nameof(InvokeBenchmarkStreamHandler),
                    BindingFlags.Public | BindingFlags.Static)
                ?? throw new InvalidOperationException("Missing generated stream startup benchmark method."));

        private static readonly IReadOnlyList<GFramework.Cqrs.CqrsStreamInvokerDescriptorEntry> Descriptors =
        [
            new GFramework.Cqrs.CqrsStreamInvokerDescriptorEntry(
                typeof(BenchmarkStreamRequest),
                typeof(BenchmarkResponse),
                Descriptor)
        ];

        /// <summary>
        ///     把 startup benchmark handler 注册为 transient，保持与 cold-start 对照宿主一致的 handler 生命周期。
        /// </summary>
        /// <param name="services">承载 generated handler 注册结果的目标服务集合。</param>
        /// <param name="logger">记录 generated registry 注册过程的日志器。</param>
        public void Register(IServiceCollection services, ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(logger);

            services.AddTransient(
                typeof(GFramework.Cqrs.Abstractions.Cqrs.IStreamRequestHandler<
                    BenchmarkStreamRequest,
                    BenchmarkResponse>),
                typeof(BenchmarkStreamHandler));
            logger.LogDebug("Registered generated stream startup benchmark handler.");
        }

        /// <summary>
        ///     返回当前 provider 暴露的全部 generated stream invoker 描述符。
        /// </summary>
        /// <returns>当前 startup benchmark 的 generated stream invoker 描述符集合。</returns>
        public IReadOnlyList<GFramework.Cqrs.CqrsStreamInvokerDescriptorEntry> GetDescriptors()
        {
            return Descriptors;
        }

        /// <summary>
        ///     为目标流式请求/响应类型对返回 generated stream invoker 描述符。
        /// </summary>
        /// <param name="requestType">待匹配的 stream 请求类型。</param>
        /// <param name="responseType">待匹配的 stream 响应类型。</param>
        /// <param name="descriptor">匹配成功时返回的 generated stream invoker 描述符。</param>
        /// <returns>命中当前 benchmark 请求/响应类型对时返回 <see langword="true" />；否则返回 <see langword="false" />。</returns>
        public bool TryGetDescriptor(
            Type requestType,
            Type responseType,
            out GFramework.Cqrs.CqrsStreamInvokerDescriptor? descriptor)
        {
            if (requestType == typeof(BenchmarkStreamRequest) &&
                responseType == typeof(BenchmarkResponse))
            {
                descriptor = Descriptor;
                return true;
            }

            descriptor = null;
            return false;
        }

        /// <summary>
        ///     模拟 generated stream invoker provider 为 startup benchmark 产出的开放静态调用入口。
        /// </summary>
        /// <param name="handler">当前 benchmark 注册的 stream handler 实例。</param>
        /// <param name="request">当前 benchmark 的 stream 请求对象。</param>
        /// <param name="cancellationToken">用于中断异步枚举的取消令牌。</param>
        /// <returns>由 handler 产生的异步响应序列。</returns>
        public static object InvokeBenchmarkStreamHandler(object handler, object request, CancellationToken cancellationToken)
        {
            var typedHandler = (GFramework.Cqrs.Abstractions.Cqrs.IStreamRequestHandler<
                BenchmarkStreamRequest,
                BenchmarkResponse>)handler;
            var typedRequest = (BenchmarkStreamRequest)request;
            return typedHandler.Handle(typedRequest, cancellationToken);
        }
    }
}
