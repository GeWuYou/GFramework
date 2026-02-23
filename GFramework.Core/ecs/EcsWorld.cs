using Arch.Core;
using GFramework.Core.Abstractions.ecs;

namespace GFramework.Core.ecs;

/// <summary>
///     ECS世界实现，封装Arch的World实例
/// </summary>
public sealed class EcsWorld : IEcsWorld
{
    private bool _disposed;

    /// <summary>
    ///     获取内部的Arch World实例
    /// </summary>
    public World InternalWorld { get; } = World.Create();

    /// <summary>
    ///     当前实体数量
    /// </summary>
    public int EntityCount => InternalWorld.Size;

    /// <summary>
    ///     创建一个新实体
    /// </summary>
    public Entity CreateEntity(params ComponentType[] types)
    {
        return InternalWorld.Create(types);
    }

    /// <summary>
    ///     销毁指定实体
    /// </summary>
    public void DestroyEntity(Entity entity)
    {
        InternalWorld.Destroy(entity);
    }

    /// <summary>
    ///     检查实体是否存活
    /// </summary>
    public bool IsAlive(Entity entity)
    {
        return InternalWorld.IsAlive(entity);
    }

    /// <summary>
    ///     清空所有实体
    /// </summary>
    public void Clear()
    {
        InternalWorld.Clear();
    }

    /// <summary>
    ///     释放资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        World.Destroy(InternalWorld);
        _disposed = true;
    }
}