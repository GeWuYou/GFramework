namespace GFramework.Core.Abstractions.query;


/// <summary>
///     定义一个查询执行器接口，用于发送查询请求并获取结果。
/// </summary>
public interface IQueryExecutor
{
    /// <summary>
    ///     发送查询请求并返回结果。
    /// </summary>
    /// <typeparam name="TResult">查询结果的类型。</typeparam>
    /// <param name="query">要发送的查询对象，必须实现 IQuery&lt;TResult&gt; 接口。</param>
    /// <returns>查询的结果，类型为 TResult。</returns>
    TResult Send<TResult>(IQuery<TResult> query);
}
