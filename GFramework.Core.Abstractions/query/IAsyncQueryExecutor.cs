namespace GFramework.Core.Abstractions.query;


/// <summary>
/// 定义一个异步查询执行器接口，用于发送异步查询请求并获取结果。
/// </summary>
public interface IAsyncQueryExecutor
{
    /// <summary>
    /// 异步发送查询请求并返回结果。
    /// </summary>
    /// <typeparam name="TResult">查询结果的类型。</typeparam>
    /// <param name="query">要执行的异步查询对象，必须实现 IAsyncQuery&lt;TResult&gt; 接口。</param>
    /// <returns>表示异步操作的任务，任务完成时返回查询结果。</returns>
    Task<TResult> SendAsync<TResult>(IAsyncQuery<TResult> query);
}
