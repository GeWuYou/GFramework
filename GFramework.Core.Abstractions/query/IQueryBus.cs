namespace GFramework.Core.Abstractions.query;

/// <summary>
/// 查询总线接口，用于发送和处理查询请求
/// </summary>
public interface IQueryBus
{
    /// <summary>
    /// 发送查询请求并返回结果
    /// </summary>
    /// <typeparam name="TResult">查询结果的类型</typeparam>
    /// <param name="query">要发送的查询对象</param>
    /// <returns>查询结果</returns>
    public TResult Send<TResult>(IQuery<TResult> query);
}