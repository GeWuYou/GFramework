using GFramework.Core.Abstractions.query;
using GFramework.Core.rule;

namespace GFramework.Core.query;

/// <summary>
///     抽象查询类，提供查询操作的基础实现
/// </summary>
/// <typeparam name="TResult">查询结果的类型</typeparam>
public abstract class AbstractQuery<TResult> : ContextAwareBase, IQuery<TResult>

{
    /// <summary>
    ///     执行查询操作
    /// </summary>
    /// <returns>查询结果，类型为TResult</returns>
    public TResult Do()
    {
        // 调用抽象方法执行具体的查询逻辑
        return OnDo();
    }

    /// <summary>
    ///     抽象方法，用于实现具体的查询逻辑
    /// </summary>
    /// <returns>查询结果，类型为TResult</returns>
    protected abstract TResult OnDo();
}