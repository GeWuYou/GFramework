using Arch.Core;
using GFramework.Core.ecs.components;

namespace GFramework.Core.ecs.systems;

/// <summary>
/// 移动系统，负责更新具有位置和速度组件的实体的位置。
/// 根据速度和时间增量计算实体的新位置。
/// </summary>
public class MovementSystem : EcsSystemBase
{
    private QueryDescription _query;

    /// <summary>
    /// 获取系统的优先级，数值越小优先级越高。
    /// </summary>
    public override int Priority => 0;

    /// <summary>
    /// ECS初始化回调方法，在系统初始化时调用。
    /// 创建查询描述符，用于查找同时拥有Position和Velocity组件的实体。
    /// </summary>
    protected override void OnEcsInit()
    {
        // 创建查询：查找所有同时拥有Position和Velocity组件的实体
        _query = new QueryDescription()
            .WithAll<Position, Velocity>();
    }

    /// <summary>
    /// 系统更新方法，每帧调用一次。
    /// </summary>
    /// <param name="deltaTime">帧间隔时间，用于计算位置变化量</param>
    public override void Update(float deltaTime)
    {
        // 查询并更新所有符合条件的实体
        World.Query(in _query, (ref Position pos, ref Velocity vel) =>
        {
            pos.X += vel.X * deltaTime;
            pos.Y += vel.Y * deltaTime;
        });
    }
}