// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Rule;
using GFramework.Core.Cqrs;
using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Core.Tests.Command;

/// <summary>
///     记录 bridge 执行线程与收到请求的最小 CQRS runtime 测试替身。
/// </summary>
internal sealed class RecordingCqrsRuntime(Func<object?, object?>? responseFactory = null) : ICqrsRuntime
{
    private static readonly Func<object?, object?> DefaultResponseFactory = _ => null;

    private readonly Func<object?, object?> _responseFactory = responseFactory ?? DefaultResponseFactory;

    /// <summary>
    ///     获取最近一次 <see cref="SendAsync{TResponse}" /> 观察到的同步上下文类型。
    /// </summary>
    public Type? ObservedSynchronizationContextType { get; private set; }

    /// <summary>
    ///     获取最近一次收到的请求实例。
    /// </summary>
    public object? LastRequest { get; private set; }

    /// <inheritdoc />
    public async ValueTask<TResponse> SendAsync<TResponse>(
        ICqrsContext context,
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        ObservedSynchronizationContextType = SynchronizationContext.Current?.GetType();
        LastRequest = request;

        object? response = request switch
        {
            LegacyCommandDispatchRequest legacyCommandDispatchRequest => ExecuteLegacyCommand(context, legacyCommandDispatchRequest),
            LegacyCommandResultDispatchRequest legacyCommandResultDispatchRequest => ExecuteContextAwareRequest(
                context,
                legacyCommandResultDispatchRequest.Target,
                legacyCommandResultDispatchRequest.Execute),
            LegacyQueryDispatchRequest legacyQueryDispatchRequest => ExecuteContextAwareRequest(
                context,
                legacyQueryDispatchRequest.Target,
                legacyQueryDispatchRequest.Execute),
            LegacyAsyncCommandDispatchRequest legacyAsyncCommandDispatchRequest => await ExecuteLegacyAsyncCommandAsync(
                context,
                legacyAsyncCommandDispatchRequest,
                cancellationToken).ConfigureAwait(false),
            LegacyAsyncCommandResultDispatchRequest legacyAsyncCommandResultDispatchRequest => await ExecuteContextAwareRequestAsync(
                context,
                legacyAsyncCommandResultDispatchRequest.Target,
                legacyAsyncCommandResultDispatchRequest.ExecuteAsync,
                cancellationToken).ConfigureAwait(false),
            LegacyAsyncQueryDispatchRequest legacyAsyncQueryDispatchRequest => await ExecuteContextAwareRequestAsync(
                context,
                legacyAsyncQueryDispatchRequest.Target,
                legacyAsyncQueryDispatchRequest.ExecuteAsync,
                cancellationToken).ConfigureAwait(false),
            IRequest<Unit> => Unit.Value,
            _ => _responseFactory(request)
        };

        return ConvertResponse<TResponse>(request, response);
    }

    /// <inheritdoc />
    public ValueTask PublishAsync<TNotification>(
        ICqrsContext context,
        TNotification notification,
        CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
        ICqrsContext context,
        IStreamRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    ///     将测试替身工厂返回的装箱结果显式还原为目标类型，并在类型不匹配时给出可诊断异常。
    /// </summary>
    /// <typeparam name="TResponse">当前请求声明的响应类型。</typeparam>
    /// <param name="request">触发响应工厂的请求实例。</param>
    /// <param name="response">响应工厂返回的装箱结果。</param>
    /// <returns>还原后的目标类型响应。</returns>
    /// <exception cref="InvalidOperationException">
    ///     响应工厂返回 <see langword="null" /> 或错误类型，导致无法还原为 <typeparamref name="TResponse" />。
    /// </exception>
    private static TResponse ConvertResponse<TResponse>(IRequest<TResponse> request, object? response)
    {
        if (response is TResponse typedResponse)
        {
            return typedResponse;
        }

        if (response is null && !typeof(TResponse).IsValueType)
        {
            return (TResponse)response!;
        }

        string actualType = response?.GetType().FullName ?? "null";
        throw new InvalidOperationException(
            $"RecordingCqrsRuntime 无法将响应类型从 '{actualType}' 转换为 '{typeof(TResponse).FullName}'。"
            + $" 请求类型：'{request.GetType().FullName}'。");
    }

    /// <summary>
    ///     按 bridge handler 语义为 legacy 无返回值命令注入上下文并执行。
    /// </summary>
    /// <param name="context">当前运行时接收到的架构上下文。</param>
    /// <param name="request">待执行的 legacy 命令桥接请求。</param>
    /// <returns>桥接后的 <see cref="Unit" /> 响应。</returns>
    private static Unit ExecuteLegacyCommand(
        ICqrsContext context,
        LegacyCommandDispatchRequest request)
    {
        PrepareTarget(context, request.Command);
        request.Command.Execute();
        return Unit.Value;
    }

    /// <summary>
    ///     按 bridge handler 语义为 legacy 异步无返回值命令注入上下文并执行。
    /// </summary>
    /// <param name="context">当前运行时接收到的架构上下文。</param>
    /// <param name="request">待执行的 legacy 异步命令桥接请求。</param>
    /// <param name="cancellationToken">调用方传入的取消令牌。</param>
    /// <returns>表示 bridge 执行完成的异步结果。</returns>
    private static async Task<Unit> ExecuteLegacyAsyncCommandAsync(
        ICqrsContext context,
        LegacyAsyncCommandDispatchRequest request,
        CancellationToken cancellationToken)
    {
        PrepareTarget(context, request.Command);
        await request.Command.ExecuteAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }

    /// <summary>
    ///     按 bridge handler 语义为带返回值 legacy 请求注入上下文并执行同步委托。
    /// </summary>
    /// <param name="context">当前运行时接收到的架构上下文。</param>
    /// <param name="target">需要接收上下文注入的 legacy 目标对象。</param>
    /// <param name="execute">实际执行 legacy 目标逻辑的同步委托。</param>
    /// <returns>同步执行结果。</returns>
    private static object? ExecuteContextAwareRequest(
        ICqrsContext context,
        object target,
        Func<object?> execute)
    {
        PrepareTarget(context, target);
        return execute();
    }

    /// <summary>
    ///     按 bridge handler 语义为带返回值 legacy 请求注入上下文并执行异步委托。
    /// </summary>
    /// <param name="context">当前运行时接收到的架构上下文。</param>
    /// <param name="target">需要接收上下文注入的 legacy 目标对象。</param>
    /// <param name="executeAsync">实际执行 legacy 目标逻辑的异步委托。</param>
    /// <param name="cancellationToken">调用方传入的取消令牌。</param>
    /// <returns>异步执行结果。</returns>
    private static async Task<object?> ExecuteContextAwareRequestAsync(
        ICqrsContext context,
        object target,
        Func<Task<object?>> executeAsync,
        CancellationToken cancellationToken)
    {
        PrepareTarget(context, target);
        return await executeAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     模拟 legacy bridge handler 的上下文注入语义，使测试替身与生产桥接行为保持一致。
    /// </summary>
    /// <param name="context">当前运行时接收到的架构上下文。</param>
    /// <param name="target">即将执行的 legacy 目标对象。</param>
    private static void PrepareTarget(ICqrsContext context, object target)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(target);

        if (context is not GFramework.Core.Abstractions.Architectures.IArchitectureContext architectureContext)
        {
            throw new InvalidOperationException(
                $"RecordingCqrsRuntime 期望收到 IArchitectureContext，但实际为 '{context.GetType().FullName}'。");
        }

        if (target is IContextAware contextAware)
        {
            contextAware.SetContext(architectureContext);
        }
    }
}
