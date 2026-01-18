using GFramework.Core.Abstractions.query;

namespace GFramework.Core.query;

/// <summary>
/// 异步查询总线接口，用于处理异步查询请求
/// </summary>
public interface IAsyncQueryBus
{
    /// <summary>
    /// 异步发送查询请求并返回结果
    /// </summary>
    /// <typeparam name="TResult">查询结果的类型</typeparam>
    /// <param name="query">要执行的异步查询对象</param>
    /// <returns>表示异步操作的任务，任务结果为查询结果</returns>
    Task<TResult> SendAsync<TResult>(IAsyncQuery<TResult> query);
}