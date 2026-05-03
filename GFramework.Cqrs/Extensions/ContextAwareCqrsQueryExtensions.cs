// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Rule;
using GFramework.Cqrs.Abstractions.Cqrs.Query;

namespace GFramework.Cqrs.Extensions;

/// <summary>
///     提供对 <see cref="IContextAware" /> 接口的 CQRS 查询扩展方法。
/// </summary>
public static class ContextAwareCqrsQueryExtensions
{
    /// <summary>
    ///     发送查询的同步版本（不推荐，仅用于兼容同步调用链）。
    /// </summary>
    /// <typeparam name="TResponse">查询响应类型。</typeparam>
    /// <param name="contextAware">实现 <see cref="IContextAware" /> 接口的对象。</param>
    /// <param name="query">要发送的查询对象。</param>
    /// <returns>查询结果。</returns>
    /// <exception cref="ArgumentNullException">
    ///     当 <paramref name="contextAware" /> 或 <paramref name="query" /> 为 <see langword="null" /> 时抛出。
    /// </exception>
    public static TResponse SendQuery<TResponse>(this IContextAware contextAware, IQuery<TResponse> query)
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        ArgumentNullException.ThrowIfNull(query);

        return contextAware.GetContext().SendQuery(query);
    }

    /// <summary>
    ///     异步发送查询并返回结果。
    /// </summary>
    /// <typeparam name="TResponse">查询响应类型。</typeparam>
    /// <param name="contextAware">实现 <see cref="IContextAware" /> 接口的对象。</param>
    /// <param name="query">要发送的查询对象。</param>
    /// <param name="cancellationToken">取消令牌，用于取消操作。</param>
    /// <returns>包含查询结果的 <see cref="ValueTask{TResult}" />。</returns>
    /// <exception cref="ArgumentNullException">
    ///     当 <paramref name="contextAware" /> 或 <paramref name="query" /> 为 <see langword="null" /> 时抛出。
    /// </exception>
    public static ValueTask<TResponse> SendQueryAsync<TResponse>(
        this IContextAware contextAware,
        IQuery<TResponse> query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        ArgumentNullException.ThrowIfNull(query);

        return contextAware.GetContext().SendQueryAsync(query, cancellationToken);
    }
}
