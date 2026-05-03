// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;

namespace GFramework.Ecs.Arch.Abstractions;

/// <summary>
///     Arch ECS 模块接口 - 定义 ECS 模块的核心契约
/// </summary>
public interface IArchEcsModule : IServiceModule
{
    /// <summary>
    ///     更新所有 ECS 系统
    /// </summary>
    /// <param name="deltaTime">帧间隔时间</param>
    void Update(float deltaTime);
}