using GFramework.Core.Abstractions.query;
using GFramework.Core.rule;

namespace GFramework.Core.query;

/// <summary>
/// 异步查询抽象基类，提供异步查询的基本框架和执行机制
/// 继承自ContextAwareBase并实现IAsyncQuery&lt;TResult&gt;接口
/// </summary>
/// <typeparam name="TResult">查询结果的类型</typeparam>
public abstract class AbstractAsyncQuery<TResult> : ContextAwareBase, IAsyncQuery<TResult> {
    /// <summary>
    /// 执行异步查询操作
    /// </summary>
    /// <returns>返回查询结果的异步任务</returns>
    public Task<TResult> DoAsync()
    {
        return OnDoAsync();
    }

    /// <summary>
    /// 抽象方法，用于实现具体的异步查询逻辑
    /// </summary>
    /// <returns>返回查询结果的异步任务</returns>
    protected abstract Task<TResult> OnDoAsync();
}
