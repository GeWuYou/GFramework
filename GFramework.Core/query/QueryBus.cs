using GFramework.Core.Abstractions.query;

namespace GFramework.Core.query;

/// <summary>
///     查询总线实现，负责执行查询并返回结果
/// </summary>
public sealed class QueryBus : IQueryBus
{
    /// <summary>
    ///     执行指定的查询并返回结果
    /// </summary>
    /// <typeparam name="TResult">查询结果的类型</typeparam>
    /// <param name="query">要执行的查询对象</param>
    /// <returns>查询执行结果</returns>
    public TResult Send<TResult>(IQuery<TResult> query)
    {
        // 验证查询参数不为null
        ArgumentNullException.ThrowIfNull(query);

        return query.Do();
    }
}