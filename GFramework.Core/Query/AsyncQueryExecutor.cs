// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics.CodeAnalysis;
using GFramework.Core.Abstractions.Query;
using GFramework.Core.Abstractions.Rule;
using GFramework.Core.Cqrs;
using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Core.Query;

/// <summary>
///     异步查询总线实现，用于处理异步查询请求
/// </summary>
public sealed class AsyncQueryExecutor(ICqrsRuntime? runtime = null) : IAsyncQueryExecutor
{
    private readonly ICqrsRuntime? _runtime = runtime;

    /// <summary>
    ///     获取当前执行器是否已接入统一 CQRS runtime。
    /// </summary>
    public bool UsesCqrsRuntime => _runtime is not null;

    /// <summary>
    ///     异步发送查询请求并返回结果
    /// </summary>
    /// <typeparam name="TResult">查询结果类型</typeparam>
    /// <param name="query">要执行的异步查询对象</param>
    /// <returns>包含查询结果的异步任务</returns>
    public Task<TResult> SendAsync<TResult>(IAsyncQuery<TResult> query)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (TryResolveDispatchContext(query, out var context))
        {
            return BridgeAsyncQueryAsync(_runtime, context, query);
        }

        return query.DoAsync();
    }

    /// <summary>
    ///     通过统一 CQRS runtime 异步执行 legacy 查询，并把装箱结果还原为目标类型。
    /// </summary>
    /// <typeparam name="TResult">查询结果类型。</typeparam>
    /// <param name="runtime">负责调度当前 bridge request 的统一 CQRS runtime。</param>
    /// <param name="context">当前架构上下文。</param>
    /// <param name="query">要桥接的 legacy 查询。</param>
    /// <returns>查询执行结果。</returns>
    private static async Task<TResult> BridgeAsyncQueryAsync<TResult>(
        ICqrsRuntime runtime,
        GFramework.Core.Abstractions.Architectures.IArchitectureContext context,
        IAsyncQuery<TResult> query)
    {
        var boxedResult = await runtime.SendAsync(
                context,
                new LegacyAsyncQueryDispatchRequest(
                    query,
                    async () => await query.DoAsync().ConfigureAwait(false)))
            .ConfigureAwait(false);
        return (TResult)boxedResult!;
    }

    /// <summary>
    ///     解析当前 legacy 查询应该绑定到哪个架构上下文。
    /// </summary>
    /// <param name="query">即将执行的 legacy 查询对象。</param>
    /// <param name="context">命中时返回可用于 CQRS runtime 的架构上下文。</param>
    /// <returns>如果既接入了 runtime 且查询对象提供了上下文，则返回 <see langword="true" />。</returns>
    [MemberNotNullWhen(true, nameof(_runtime))]
    private bool TryResolveDispatchContext(
        object query,
        out GFramework.Core.Abstractions.Architectures.IArchitectureContext context)
    {
        context = null!;

        if (_runtime is null || query is not IContextAware contextAware)
        {
            return false;
        }

        try
        {
            context = contextAware.GetContext();
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
