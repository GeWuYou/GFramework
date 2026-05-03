// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using Arch.Core;
using GFramework.Ecs.Arch.Components;

namespace GFramework.Ecs.Arch.Systems;

/// <summary>
/// 移动系统 - 继承 ArchSystemAdapter
/// 负责更新具有位置和速度组件的实体的位置
/// </summary>
public sealed class MovementSystem : ArchSystemAdapter<float>
{
    private QueryDescription _query;

    /// <summary>
    /// Arch 系统初始化
    /// </summary>
    protected override void OnArchInitialize()
    {
        // 创建查询：查找所有同时拥有Position和Velocity组件的实体
        _query = new QueryDescription()
            .WithAll<Position, Velocity>();
    }

    /// <summary>
    /// 系统更新方法，每帧调用一次
    /// </summary>
    /// <param name="deltaTime">帧间隔时间</param>
    protected override void OnUpdate(in float deltaTime)
    {
        // 查询并更新所有符合条件的实体
        var f = deltaTime;
        World.Query(in _query, (ref Position pos, ref Velocity vel) =>
        {
            pos.X += vel.X * f;
            pos.Y += vel.Y * f;
        });
    }
}