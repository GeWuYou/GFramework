// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Query;
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
    /// <returns>查询执行成功后还原出的 <typeparamref name="TResult" /> 结果。</returns>
    /// <exception cref="NullReferenceException">
    ///     统一 CQRS runtime 返回 <see langword="null" />，但 <typeparamref name="TResult" /> 为值类型。
    /// </exception>
    /// <exception cref="InvalidCastException">
    ///     统一 CQRS runtime 返回的装箱结果无法转换为 <typeparamref name="TResult" />。
    /// </exception>
    public TResult Send<TResult>(IQuery<TResult> query)
    {
        ArgumentNullException.ThrowIfNull(query);

        var cqrsRuntime = _runtime;

        if (LegacyCqrsDispatchHelper.TryResolveDispatchContext(cqrsRuntime, query, out var context))
        {
            var boxedResult = LegacyCqrsDispatchHelper.SendSynchronously(
                cqrsRuntime,
                context,
                new LegacyQueryDispatchRequest(
                    query,
                    () => query.Do()));
            return (TResult)boxedResult!;
        }

        return query.Do();
    }
}
