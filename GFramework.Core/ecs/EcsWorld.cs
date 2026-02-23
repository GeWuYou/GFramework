using Arch.Core;
using GFramework.Core.Abstractions.ecs;

namespace GFramework.Core.ecs;

/// <summary>
///     ECS世界实现，封装Arch的World实例
/// </summary>
public sealed class EcsWorld : IEcsWorld
{
    private readonly World _world = World.Create();
    private bool _disposed;

    /// <summary>
    ///     获取内部的Arch World实例
    /// </summary>
    public World InternalWorld => _world;

    /// <summary>
    ///     当前实体数量
    /// </summary>
    public int EntityCount => _world.Size;

    /// <summary>
    ///     创建一个新实体
    /// </summary>
    public Entity CreateEntity(params ComponentType[] types)
    {
        return _world.Create(types);
    }

    /// <summary>
    ///     销毁指定实体
    /// </summary>
    public void DestroyEntity(Entity entity)
    {
        _world.Destroy(entity);
    }

    /// <summary>
    ///     检查实体是否存活
    /// </summary>
    public bool IsAlive(Entity entity)
    {
        return _world.IsAlive(entity);
    }

    /// <summary>
    ///     清空所有实体
    /// </summary>
    public void Clear()
    {
        _world.Clear();
    }

    /// <summary>
    ///     释放资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        World.Destroy(_world);
        _disposed = true;
    }
}