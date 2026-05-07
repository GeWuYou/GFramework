// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Query;
using GFramework.Core.Abstractions.Rule;
using GFramework.Core.Cqrs;
using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Core.Query;


/// <summary>
///     QueryExecutor 类负责执行查询操作，实现 IQueryExecutor 接口。
///     该类是密封的，防止被继承。
/// </summary>
public sealed class QueryExecutor(ICqrsRuntime? runtime = null) : IQueryExecutor
{
    private readonly ICqrsRuntime? _runtime = runtime;

    /// <summary>
    ///     获取当前执行器是否已接入统一 CQRS runtime。
    /// </summary>
    public bool UsesCqrsRuntime => _runtime is not null;

    /// <summary>
    ///     执行指定的查询并返回结果。
    ///     当查询对象携带可用的架构上下文且执行器已接入统一 runtime 时，
    ///     该方法会先把 legacy 查询包装成内部 request 并交给 <see cref="ICqrsRuntime" />，
    ///     以复用统一的 dispatch / pipeline 入口；否则回退到 legacy 直接执行。
    /// </summary>
    /// <typeparam name="TResult">查询结果的类型。</typeparam>
    /// <param name="query">要执行的查询对象，必须实现 IQuery&lt;TResult&gt; 接口。</param>
    /// <returns>查询执行的结果，类型为 TResult。</returns>
    public TResult Send<TResult>(IQuery<TResult> query)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (TryResolveDispatchContext(query, out var context) && _runtime is not null)
        {
            var boxedResult = _runtime.SendAsync(
                    context,
                    new LegacyQueryDispatchRequest(
                        query,
                        () => query.Do()))
                .AsTask()
                .GetAwaiter()
                .GetResult();
            return (TResult)boxedResult!;
        }

        return query.Do();
    }

    /// <summary>
    ///     解析当前 legacy 查询应该绑定到哪个架构上下文。
    /// </summary>
    /// <param name="query">即将执行的 legacy 查询对象。</param>
    /// <param name="context">命中时返回可用于 CQRS runtime 的架构上下文。</param>
    /// <returns>如果既接入了 runtime 且查询对象提供了上下文，则返回 <see langword="true" />。</returns>
    private bool TryResolveDispatchContext(object query, out GFramework.Core.Abstractions.Architectures.IArchitectureContext context)
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
