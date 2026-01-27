namespace GFramework.Core.Abstractions.pool;

/// <summary>
///     对象池系统接口，定义了对象池的基本操作
/// </summary>
/// <typeparam name="TKey">池键的类型</typeparam>
/// <typeparam name="TObject">池中对象的类型，必须实现IPoolableObject接口</typeparam>
public interface IObjectPoolSystem<in TKey, TObject>
    where TObject : IPoolableObject
    where TKey : notnull
{
    /// <summary>
    ///     从对象池中获取一个对象实例
    /// </summary>
    /// <param name="key">对象池的键</param>
    /// <returns>池中的对象实例，如果池中没有可用对象则创建新实例</returns>
    TObject Acquire(TKey key);

    /// <summary>
    ///     将对象释放回对象池
    /// </summary>
    /// <param name="key">对象池的键</param>
    /// <param name="obj">要释放的对象</param>
    void Release(TKey key, TObject obj);

    /// <summary>
    ///     清空所有对象池
    /// </summary>
    void Clear();
}