using Arch.Core;
using GFramework.Core.ecs.components;

namespace GFramework.Core.ecs.systems;

/// <summary>
/// 移动系统 - Arch 原生实现
/// 负责更新具有位置和速度组件的实体的位置
/// </summary>
public sealed class MovementSystem : ArchSystemAdapter<float>
{
    private QueryDescription _query;

    /// <summary>
    /// 初始化系统
    /// </summary>
    public void Initialize(World world)
    {
        // 创建查询：查找所有同时拥有Position和Velocity组件的实体
        _query = new QueryDescription()
            .WithAll<Position, Velocity>();
    }


    /// <summary>
    /// 系统更新方法，每帧调用一次
    /// </summary>
    /// <param name="world">ECS 世界</param>
    /// <param name="deltaTime">帧间隔时间</param>
    public void Update(World world, float deltaTime)
    {
        // 查询并更新所有符合条件的实体
        world.Query(in _query, (ref Position pos, ref Velocity vel) =>
        {
            pos.X += vel.X * deltaTime;
            pos.Y += vel.Y * deltaTime;
        });
    }
}