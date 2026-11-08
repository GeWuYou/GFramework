// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GFramework.Cqrs.Abstractions.Cqrs;
using GFramework.Cqrs.Internal;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GFramework.Cqrs.Benchmarks.Messaging;

/// <summary>
///     为 benchmark 场景构建最小且可重复的 GFramework / MediatR 对照宿主。
/// </summary>
/// <remarks>
///     基准工程里的对照目标是“相同消息合同下的调度差异”，而不是程序集扫描量或容器生命周期差异。
///     因此这里统一封装两类宿主的最小注册形状，确保：
///     1. GFramework 宿主基于标准 <see cref="IServiceProvider" /> 契约：容器冻结后即构建根 provider，
///        CQRS runtime 直接以标准服务解析语义复用该 provider；
///     2. MediatR 只扫描当前 benchmark 明确拥有的 handler / behavior 类型，避免整个程序集的额外注册污染结果。
/// </remarks>
internal static class BenchmarkHostFactory
{
    /// <summary>
    ///     创建一个已经冻结的 GFramework benchmark 容器。
    /// </summary>
    /// <param name="configure">向容器写入 benchmark 所需 handler / pipeline 的注册动作。</param>
    /// <returns>已冻结、可立即以标准 <see cref="IServiceProvider" /> 语义用于 runtime 分发的容器。</returns>
    internal static BenchmarkServiceHost CreateFrozenGFrameworkContainer(Action<BenchmarkServiceHost> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var container = new BenchmarkServiceHost();
        configure(container);
        container.Freeze();
        return container;
    }

    /// <summary>
    ///     只激活当前 benchmark 场景明确拥有的 generated registry，避免同一程序集里的其他 benchmark registry
    ///     扩大冻结后服务索引与 dispatcher descriptor 基线。
    /// </summary>
    /// <typeparam name="TRegistry">当前 benchmark 需要接入的 generated registry 类型。</typeparam>
    /// <param name="container">承载 generated registry 注册结果的 GFramework benchmark 容器。</param>
    /// <remarks>
    ///     generated registry 注册发生在标准 <see cref="IServiceCollection" /> 上，
    ///     因此这里直接复用 runtime 内部的单 registry 激活入口，而不触发整程序集扫描。
    /// </remarks>
    internal static void RegisterGeneratedBenchmarkRegistry<TRegistry>(BenchmarkServiceHost container)
        where TRegistry : class, GFramework.Cqrs.ICqrsHandlerRegistry
    {
        ArgumentNullException.ThrowIfNull(container);

        var registrarLogger = BenchmarkLogging.CreateLogger("DefaultCqrsHandlerRegistrar");
        CqrsHandlerRegistrar.RegisterGeneratedRegistry(container.Services, typeof(TRegistry), registrarLogger);
    }

    /// <summary>
    ///     创建只承载当前 benchmark handler 集合的最小 MediatR 宿主。
    /// </summary>
    /// <param name="configure">补充当前场景的显式服务注册，例如手工单例 handler 或 pipeline 行为。</param>
    /// <param name="handlerAssemblyMarkerType">用于限定扫描程序集的标记类型。</param>
    /// <param name="handlerTypeFilter">
    ///     仅允许当前 benchmark 场景需要的 handler / behavior 类型通过扫描；
    ///     这样可保留 `AddMediatR` 的正常装配路径，同时避免整个基准程序集里的其他 handler 被一并注册。
    /// </param>
    /// <param name="lifetime">当前 benchmark 希望 MediatR 使用的默认注册生命周期。</param>
    /// <returns>只承载当前 benchmark 场景所需服务的 DI 宿主。</returns>
    internal static ServiceProvider CreateMediatRServiceProvider(
        Action<IServiceCollection>? configure,
        Type handlerAssemblyMarkerType,
        Func<Type, bool> handlerTypeFilter,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        ArgumentNullException.ThrowIfNull(handlerAssemblyMarkerType);
        ArgumentNullException.ThrowIfNull(handlerTypeFilter);

        var services = new ServiceCollection();
        services.AddLogging(static builder =>
            Microsoft.Extensions.Logging.FilterLoggingBuilderExtensions.AddFilter(
                builder,
                "LuckyPennySoftware.MediatR.License",
                Microsoft.Extensions.Logging.LogLevel.None));

        configure?.Invoke(services);

        services.AddMediatR(options =>
        {
            options.Lifetime = lifetime;
            options.TypeEvaluator = handlerTypeFilter;
            options.RegisterServicesFromAssembly(handlerAssemblyMarkerType.Assembly);
        });

        return services.BuildServiceProvider();
    }

    /// <summary>
    ///     在真实的 request 级作用域内执行一次 GFramework.CQRS request 分发。
    /// </summary>
    /// <typeparam name="TResponse">请求响应类型。</typeparam>
    /// <param name="runtime">复用的 scoped benchmark runtime。</param>
    /// <param name="scopedContainer">负责为每次 request 激活独立作用域的标准 provider 适配层。</param>
    /// <param name="request">要发送的 request。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>当前 request 的响应结果。</returns>
    /// <remarks>
    ///     该入口只服务 request lifetime benchmark：它会复用同一个 dispatcher/runtime 实例，
    ///     但在每次调用前后显式创建并释放新的 DI 作用域，
    ///     让 `Scoped` handler 在真实 request 边界内解析，而不是退化为根 provider 解析或额外计入 runtime 构造成本。
    /// </remarks>
    internal static async ValueTask<TResponse> SendScopedGFrameworkRequestAsync<TResponse>(
        ICqrsRuntime runtime,
        ScopedBenchmarkContainer scopedContainer,
        GFramework.Cqrs.Abstractions.Cqrs.IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(scopedContainer);
        ArgumentNullException.ThrowIfNull(request);

        using var scopeLease = scopedContainer.EnterScope();
        return await runtime.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     在真实的 request 级作用域内执行一次 MediatR request 分发。
    /// </summary>
    /// <typeparam name="TResponse">请求响应类型。</typeparam>
    /// <param name="rootServiceProvider">当前 benchmark 的根 <see cref="ServiceProvider" />。</param>
    /// <param name="request">要发送的 request。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>当前 request 的响应结果。</returns>
    /// <remarks>
    ///     这里显式从新的 scope 解析 <see cref="IMediator" />，确保 `Scoped` handler 与其依赖绑定到 request 边界。
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "MA0042",
        Justification = "This matched benchmark workload deliberately measures synchronous scope disposal on both frameworks; changing only the baseline to async disposal would alter the comparison.")]
    internal static async Task<TResponse> SendScopedMediatRRequestAsync<TResponse>(
        ServiceProvider rootServiceProvider,
        MediatR.IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rootServiceProvider);
        ArgumentNullException.ThrowIfNull(request);

        using var scope = rootServiceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        return await mediator.Send(request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     在真实的 request 级作用域内创建一次 GFramework.CQRS stream，并让该作用域覆盖整个异步枚举周期。
    /// </summary>
    /// <typeparam name="TResponse">stream 响应元素类型。</typeparam>
    /// <param name="runtime">复用的 scoped benchmark runtime。</param>
    /// <param name="scopedContainer">负责为每次 stream 激活独立作用域的标准 provider 适配层。</param>
    /// <param name="request">要创建 stream 的 request。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>绑定到单次显式作用域的异步响应序列。</returns>
    /// <remarks>
    ///     stream 与 request 的区别在于：handler 解析发生在建流时，但 scoped 依赖必须一直存活到枚举完成。
    ///     因此这里返回一个包装后的 async iterator，把 scope 的释放时机推迟到调用方结束枚举之后，
    ///     避免 `Scoped` handler 退化成“建流后立刻释放 scope，再在根 provider 语义下继续枚举”的错误模型。
    /// </remarks>
    internal static IAsyncEnumerable<TResponse> CreateScopedGFrameworkStream<TResponse>(
        ICqrsRuntime runtime,
        ScopedBenchmarkContainer scopedContainer,
        GFramework.Cqrs.Abstractions.Cqrs.IStreamRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(scopedContainer);
        ArgumentNullException.ThrowIfNull(request);

        return EnumerateScopedGFrameworkStreamAsync(runtime, scopedContainer, request, cancellationToken);
    }

    /// <summary>
    ///     在真实的 request 级作用域内创建一次 MediatR stream，并让该作用域覆盖整个异步枚举周期。
    /// </summary>
    /// <typeparam name="TResponse">stream 响应元素类型。</typeparam>
    /// <param name="rootServiceProvider">当前 benchmark 的根 <see cref="ServiceProvider" />。</param>
    /// <param name="request">要创建 stream 的 request。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>绑定到单次显式作用域的异步响应序列。</returns>
    /// <remarks>
    ///     这里与 scoped request helper 保持同一组边界约束，但把 scope 生命周期延长到 stream 完整枚举结束，
    ///     确保 `Scoped` handler 与依赖不会在首个元素产出前后被提前释放。
    /// </remarks>
    internal static IAsyncEnumerable<TResponse> CreateScopedMediatRStream<TResponse>(
        ServiceProvider rootServiceProvider,
        MediatR.IStreamRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rootServiceProvider);
        ArgumentNullException.ThrowIfNull(request);

        return EnumerateScopedMediatRStreamAsync(rootServiceProvider, request, cancellationToken);
    }

    /// <summary>
    ///     创建承载 NuGet `Mediator` source-generated concrete mediator 的最小对照宿主。
    /// </summary>
    /// <param name="configure">补充当前场景的显式服务注册。</param>
    /// <returns>可直接解析 generated `Mediator.Mediator` 的 DI 宿主。</returns>
    /// <remarks>
    ///     `Mediator` 的 DI lifetime 由 source generator 在编译期固定到整个位于当前项目中的生成产物上。
    ///     因此 benchmark 工程必须统一使用一套 compile-time 常量配置；这里显式收敛为 `Singleton`，
    ///     避免同一编译产物里混入多个 `AddMediator` lifetime 形状后，在 BenchmarkDotNet 自动生成宿主中触发
    ///     “generated code lifetime 与 runtime options 不一致”的启动失败。
    /// </remarks>
    internal static ServiceProvider CreateMediatorServiceProvider(Action<IServiceCollection>? configure)
    {
        var services = new ServiceCollection();
        configure?.Invoke(services);
        services.AddMediator(static options => options.ServiceLifetime = ServiceLifetime.Singleton);
        return services.BuildServiceProvider();
    }

    /// <summary>
    ///     判断某个类型是否正好实现了指定的闭合或开放 MediatR 合同。
    /// </summary>
    /// <param name="candidateType">待判断类型。</param>
    /// <param name="openGenericContract">目标开放泛型合同，例如 <see cref="MediatR.IRequestHandler{TRequest,TResponse}" />。</param>
    /// <returns>命中任一实现接口时返回 <see langword="true" />；否则返回 <see langword="false" />。</returns>
    internal static bool ImplementsOpenGenericContract(Type candidateType, Type openGenericContract)
    {
        ArgumentNullException.ThrowIfNull(candidateType);
        ArgumentNullException.ThrowIfNull(openGenericContract);

        return candidateType.GetInterfaces().Any(interfaceType =>
            interfaceType.IsGenericType &&
            interfaceType.GetGenericTypeDefinition() == openGenericContract);
    }

    /// <summary>
    ///     在单个显式作用域内创建并枚举 GFramework.CQRS stream。
    /// </summary>
    private static async IAsyncEnumerable<TResponse> EnumerateScopedGFrameworkStreamAsync<TResponse>(
        ICqrsRuntime runtime,
        ScopedBenchmarkContainer scopedContainer,
        GFramework.Cqrs.Abstractions.Cqrs.IStreamRequest<TResponse> request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var scopeLease = scopedContainer.EnterScope();
        var stream = runtime.CreateStream(request, cancellationToken);

        await foreach (var response in stream.ConfigureAwait(false).WithCancellation(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return response;
        }
    }

    /// <summary>
    ///     在单个显式作用域内创建并枚举 MediatR stream。
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "MA0042",
        Justification = "This matched benchmark workload deliberately measures synchronous scope disposal on both frameworks; changing only the baseline to async disposal would alter the comparison.")]
    private static async IAsyncEnumerable<TResponse> EnumerateScopedMediatRStreamAsync<TResponse>(
        ServiceProvider rootServiceProvider,
        MediatR.IStreamRequest<TResponse> request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var scope = rootServiceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var stream = mediator.CreateStream(request, cancellationToken);

        await foreach (var response in stream.ConfigureAwait(false).WithCancellation(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return response;
        }
    }
}
