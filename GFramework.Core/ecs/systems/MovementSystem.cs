using Arch.Core;
using GFramework.Core.ecs.components;

namespace GFramework.Core.ecs.systems;

/// <summary>
///     移动系统示例 - 根据速度更新位置
/// </summary>
public class MovementSystem : EcsSystemBase
{
    private QueryDescription _query;

    public override int Priority => 0;

    protected override void OnEcsInit()
    {
        // 创建查询：查找所有同时拥有Position和Velocity组件的实体
        _query = new QueryDescription()
            .WithAll<Position, Velocity>();
    }

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