using GFramework.Core.Abstractions.query;

namespace GFramework.Core.query;


/// <summary>
///     QueryExecutor 类负责执行查询操作，实现 IQueryExecutor 接口。
///     该类是密封的，防止被继承。
/// </summary>
public sealed class QueryExecutor : IQueryExecutor
{
    /// <summary>
    ///     执行指定的查询并返回结果。
    ///     该方法通过调用查询对象的 Do 方法来获取结果。
    /// </summary>
    /// <typeparam name="TResult">查询结果的类型。</typeparam>
    /// <param name="query">要执行的查询对象，必须实现 IQuery&lt;TResult&gt; 接口。</param>
    /// <returns>查询执行的结果，类型为 TResult。</returns>
    public TResult Send<TResult>(IQuery<TResult> query)
    {
        // 验证查询参数不为 null，如果为 null 则抛出 ArgumentNullException 异常
        ArgumentNullException.ThrowIfNull(query);

        // 调用查询对象的 Do 方法执行查询并返回结果
        return query.Do();
    }
}
