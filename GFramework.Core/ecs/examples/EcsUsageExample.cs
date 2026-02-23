using Arch.Core;
using GFramework.Core.Abstractions.architecture;
using GFramework.Core.ecs.components;

namespace GFramework.Core.ecs.examples;

/// <summary>
///     ECS使用示例 - 演示如何创建和管理实体
/// </summary>
public static class EcsUsageExample
{
    /// <summary>
    ///     示例1: 创建移动的敌人实体
    /// </summary>
    public static void CreateMovingEnemies(IArchitectureContext context, int count)
    {
        var ecsWorld = context.GetEcsWorld();
        var world = ecsWorld.InternalWorld;

        for (int i = 0; i < count; i++)
        {
            // 创建实体
            var entity = ecsWorld.CreateEntity(
                new ComponentType[]
                {
                    typeof(Position),
                    typeof(Velocity)
                }
            );

            // 设置初始位置和速度
            world.Set(entity, new Position(i * 10, 0));

            var random = new Random();
            world.Set(entity, new Velocity(
                (float)(random.NextDouble() * 100 - 50),
                (float)(random.NextDouble() * 100 - 50)
            ));
        }
    }

    /// <summary>
    ///     示例2: 批量清理实体
    /// </summary>
    public static void ClearAllEntities(IArchitectureContext context)
    {
        var ecsWorld = context.GetEcsWorld();
        ecsWorld.Clear();
    }

    /// <summary>
    ///     示例3: 查询特定实体
    /// </summary>
    public static int CountMovingEntities(IArchitectureContext context)
    {
        var ecsWorld = context.GetEcsWorld();
        var world = ecsWorld.InternalWorld;

        int count = 0;
        var query = new QueryDescription()
            .WithAll<Position, Velocity>();

        world.Query(in query, (Entity entity) => { count++; });

        return count;
    }
}