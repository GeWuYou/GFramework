using GFramework.Core.Abstractions.query;
using GFramework.Core.rule;

namespace GFramework.Core.query;

/// <summary>
///     抽象查询类，提供查询操作的基础实现
/// </summary>
/// <typeparam name="T">查询结果的类型</typeparam>
public abstract class AbstractQuery<T> : ContextAwareBase, IQuery<T>
{
    /// <summary>
    ///     执行查询操作
    /// </summary>
    /// <returns>查询结果</returns>
    public T Do()
    {
        return OnDo();
    }

    /// <summary>
    ///     抽象方法，由子类实现具体的查询逻辑
    /// </summary>
    /// <returns>查询结果</returns>
    protected abstract T OnDo();
}