using GFramework.Core.pool;
using Godot;

namespace GFramework.Godot.pool;

/// <summary>
///     抽象节点对象池系统，用于管理Godot节点类型的对象池
/// </summary>
/// <typeparam name="TKey">用作键的类型，必须不为null</typeparam>
/// <typeparam name="TNode">节点类型，必须继承自Node并实现IPoolableNode接口</typeparam>
public abstract class AbstractNodePoolSystem<TKey, TNode>
    : AbstractObjectPoolSystem<TKey, TNode>
    where TKey : notnull
    where TNode : Node, IPoolableNode
{
    /// <summary>
    ///     加载场景的抽象方法
    /// </summary>
    /// <param name="key">用于标识场景的键</param>
    /// <returns>加载的PackedScene对象</returns>
    protected abstract PackedScene LoadScene(TKey key);

    /// <summary>
    ///     创建新节点实例的重写方法
    /// </summary>
    /// <param name="key">用于创建节点的键</param>
    /// <returns>创建的新节点实例</returns>
    protected override TNode Create(TKey key)
    {
        return LoadScene(key).Instantiate<TNode>();
    }
}