using GFramework.Core.Abstractions.pool;
using GFramework.Core.system;

namespace GFramework.Core.pool;

/// <summary>
/// 抽象对象池系统，提供基于键值的对象池管理功能
/// </summary>
/// <typeparam name="TKey">对象池的键类型，必须不为null</typeparam>
/// <typeparam name="TObject">池化对象类型，必须实现IPoolableObject接口</typeparam>
public abstract class AbstractObjectPoolSystem<TKey, TObject>
    : AbstractSystem, IObjectPoolSystem<TKey, TObject> where TObject : IPoolableObject where TKey : notnull
{
    /// <summary>
    /// 存储对象池的字典，键为池标识，值为对应类型的对象栈
    /// </summary>
    protected readonly Dictionary<TKey, Stack<TObject>> Pools = new();

    /// <summary>
    /// 获取对象池中的对象，如果池中没有可用对象则创建新的对象
    /// </summary>
    /// <param name="key">对象池的键值</param>
    /// <returns>获取到的对象实例</returns>
    public TObject Acquire(TKey key)
    {
        if (!Pools.TryGetValue(key, out var pool))
        {
            pool = new Stack<TObject>();
            Pools[key] = pool;
        }

        var obj = pool.Count > 0
            ? pool.Pop()
            : Create(key);

        obj.OnAcquire();
        return obj;
    }

    /// <summary>
    /// 将对象释放回对象池中
    /// </summary>
    /// <param name="key">对象池的键值</param>
    /// <param name="obj">需要释放的对象</param>
    public void Release(TKey key, TObject obj)
    {
        obj.OnRelease();

        if (!Pools.TryGetValue(key, out var pool))
        {
            pool = new Stack<TObject>();
            Pools[key] = pool;
        }

        pool.Push(obj);
    }

    /// <summary>
    /// 清空所有对象池，销毁所有池中的对象并清理池容器
    /// </summary>
    public void Clear()
    {
        // 遍历所有对象池，调用每个对象的销毁方法
        foreach (var obj in Pools.Values.SelectMany(pool => pool))
        {
            obj.OnPoolDestroy();
        }

        Pools.Clear();
    }

    /// <summary>
    /// 创建一个新的对象实例（由子类决定怎么创建）
    /// </summary>
    /// <param name="key">用于创建对象的键值</param>
    /// <returns>新创建的对象实例</returns>
    protected abstract TObject Create(TKey key);

    /// <summary>
    /// 系统销毁时的清理操作，清空所有对象池
    /// </summary>
    protected override void OnDestroy()
    {
        Clear();
    }
}