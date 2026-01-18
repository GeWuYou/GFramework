using GFramework.Core.Abstractions.query;

namespace GFramework.Core.query;

/// <summary>
/// 异步查询总线实现，用于处理异步查询请求
/// </summary>
public sealed class AsyncQueryBus : IAsyncQueryBus
{
    /// <summary>
    /// 异步发送查询请求并返回结果
    /// </summary>
    /// <typeparam name="TResult">查询结果类型</typeparam>
    /// <param name="query">要执行的异步查询对象</param>
    /// <returns>包含查询结果的异步任务</returns>
    public Task<TResult> SendAsync<TResult>(IAsyncQuery<TResult> query)
    {
        // 验证查询参数不为空
        ArgumentNullException.ThrowIfNull(query);
        return query.DoAsync();
    }
}