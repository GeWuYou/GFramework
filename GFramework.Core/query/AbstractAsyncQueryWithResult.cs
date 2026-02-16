using GFramework.Core.Abstractions.cqrs.query;
using GFramework.Core.Abstractions.query;
using GFramework.Core.rule;

namespace GFramework.Core.query;

/// <summary>
///     抽象异步查询基类，用于处理输入类型为TInput、结果类型为TResult的异步查询操作
/// </summary>
/// <typeparam name="TInput">查询输入类型，必须实现IQueryInput接口</typeparam>
/// <typeparam name="TResult">查询结果类型</typeparam>
/// <param name="input">查询输入参数</param>
public abstract class AbstractAsyncQuery<TInput, TResult>(
    TInput input
) : ContextAwareBase, IAsyncQuery<TResult>
    where TInput : IQueryInput
{
    /// <summary>
    ///     执行异步查询操作
    /// </summary>
    /// <returns>返回查询结果的异步任务</returns>
    public Task<TResult> DoAsync()
    {
        return OnDoAsync(input);
    }

    /// <summary>
    ///     抽象方法，用于实现具体的异步查询逻辑
    /// </summary>
    /// <param name="input">查询输入参数</param>
    /// <returns>返回查询结果的异步任务</returns>
    protected abstract Task<TResult> OnDoAsync(TInput input);
}