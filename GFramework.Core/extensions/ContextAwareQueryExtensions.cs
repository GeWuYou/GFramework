using GFramework.Core.Abstractions.query;
using GFramework.Core.Abstractions.rule;

namespace GFramework.Core.extensions;

/// <summary>
///     提供对 IContextAware 接口的查询执行扩展方法
/// </summary>
public static class ContextAwareQueryExtensions
{
    /// <summary>
    ///     [Mediator] 发送查询的同步版本（不推荐,仅用于兼容性）
    /// </summary>
    /// <typeparam name="TResponse">查询响应类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <param name="query">要发送的查询对象</param>
    /// <returns>查询结果</returns>
    /// <exception cref="ArgumentNullException">当 contextAware 或 query 为 null 时抛出</exception>
    public static TResponse SendQuery<TResponse>(this IContextAware contextAware, Mediator.IQuery<TResponse> query)
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        ArgumentNullException.ThrowIfNull(query);

        var context = contextAware.GetContext();
        return context.SendQuery(query);
    }

    /// <summary>
    ///     发送一个查询请求
    /// </summary>
    /// <typeparam name="TResult">查询结果类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <param name="query">要发送的查询</param>
    /// <returns>查询结果</returns>
    /// <exception cref="ArgumentNullException">当 contextAware 或 query 为 null 时抛出</exception>
    public static TResult SendQuery<TResult>(this IContextAware contextAware, Abstractions.query.IQuery<TResult> query)
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        ArgumentNullException.ThrowIfNull(query);

        var context = contextAware.GetContext();
        return context.SendQuery(query);
    }

    /// <summary>
    ///     [Mediator] 异步发送查询并返回结果
    /// </summary>
    /// <typeparam name="TResponse">查询响应类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <param name="query">要发送的查询对象</param>
    /// <param name="cancellationToken">取消令牌,用于取消操作</param>
    /// <returns>包含查询结果的ValueTask</returns>
    /// <exception cref="ArgumentNullException">当 contextAware 或 query 为 null 时抛出</exception>
    public static ValueTask<TResponse> SendQueryAsync<TResponse>(this IContextAware contextAware,
        Mediator.IQuery<TResponse> query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        ArgumentNullException.ThrowIfNull(query);

        var context = contextAware.GetContext();
        return context.SendQueryAsync(query, cancellationToken);
    }

    /// <summary>
    ///     异步发送一个查询请求
    /// </summary>
    /// <typeparam name="TResult">查询结果类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <param name="query">要发送的异步查询</param>
    /// <returns>查询结果</returns>
    /// <exception cref="ArgumentNullException">当 contextAware 或 query 为 null 时抛出</exception>
    public static async Task<TResult> SendQueryAsync<TResult>(this IContextAware contextAware,
        IAsyncQuery<TResult> query)
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        ArgumentNullException.ThrowIfNull(query);

        var context = contextAware.GetContext();
        return await context.SendQueryAsync(query);
    }
}