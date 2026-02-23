using Arch.Core;

namespace GFramework.Core.Abstractions.ecs;

/// <summary>
///     ECS世界接口，封装Arch的World实例
/// </summary>
public interface IEcsWorld : IDisposable
{
    /// <summary>
    ///     当前实体数量
    /// </summary>
    int EntityCount { get; }

    /// <summary>
    ///     获取内部的Arch World实例（用于高级操作）
    /// </summary>
    World InternalWorld { get; }

    /// <summary>
    ///     创建一个新实体
    /// </summary>
    /// <param name="types">组件类型数组</param>
    /// <returns>创建的实体</returns>
    Entity CreateEntity(params ComponentType[] types);

    /// <summary>
    ///     销毁指定实体
    /// </summary>
    /// <param name="entity">要销毁的实体</param>
    void DestroyEntity(Entity entity);

    /// <summary>
    ///     检查实体是否存活
    /// </summary>
    /// <param name="entity">要检查的实体</param>
    /// <returns>实体是否存活</returns>
    bool IsAlive(Entity entity);

    /// <summary>
    ///     清空所有实体
    /// </summary>
    void Clear();
}