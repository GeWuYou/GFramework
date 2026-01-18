using System.Threading.Tasks;

namespace GFramework.Core.Abstractions.query;

/// <summary>
/// 异步查询接口，定义了执行异步查询操作的方法
/// </summary>
/// <typeparam name="TResult">查询结果的类型</typeparam>
public interface IAsyncQuery<TResult>
{
    /// <summary>
    /// 执行异步查询操作
    /// </summary>
    /// <returns>返回查询结果的Task，结果类型为TResult</returns>
    Task<TResult> DoAsync();
}