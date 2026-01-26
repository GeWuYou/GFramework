using GFramework.Core.Abstractions.query;
using GFramework.Core.rule;

namespace GFramework.Core.query;

/// <summary>
///     抽象查询类，提供查询操作的基础实现
/// </summary>
/// <typeparam name="TInput">查询输入参数的类型，必须实现IQueryInput接口</typeparam>
/// <typeparam name="TResult">查询结果的类型</typeparam>
public abstract class AbstractQuery<TInput, TResult>(TInput input) : ContextAwareBase, IQuery<TResult>
    where TInput : IQueryInput
{
    /// <summary>
    ///     执行查询操作
    /// </summary>
    /// <returns>查询结果，类型为TResult</returns>
    public TResult Do()
    {
        return OnDo(input);
    }

    /// <summary>
    ///     抽象方法，用于实现具体的查询逻辑
    /// </summary>
    /// <param name="input">查询输入参数</param>
    /// <returns>查询结果，类型为TResult</returns>
    protected abstract TResult OnDo(TInput input);
}