using GFramework.Core.architecture;

namespace GFramework.Core.query;

/// <summary>
/// 抽象查询类，提供查询操作的基础实现
/// </summary>
/// <typeparam name="T">查询结果的类型</typeparam>
public abstract class AbstractQuery<T> : IQuery<T>
{
    /// <summary>
    /// 执行查询操作
    /// </summary>
    /// <returns>查询结果</returns>
    public T Do() => OnDo();

    /// <summary>
    /// 抽象方法，由子类实现具体的查询逻辑
    /// </summary>
    /// <returns>查询结果</returns>
    protected abstract T OnDo();

    private IArchitecture _mArchitecture;

    /// <summary>
    /// 获取架构实例
    /// </summary>
    /// <returns>架构实例</returns>
    public IArchitecture GetArchitecture() => _mArchitecture;

    /// <summary>
    /// 设置架构实例
    /// </summary>
    /// <param name="architecture">要设置的架构实例</param>
    public void SetArchitecture(IArchitecture architecture) => _mArchitecture = architecture;
}
