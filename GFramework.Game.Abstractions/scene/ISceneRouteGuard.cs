// Copyright (c) 2026 GeWuYou
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace GFramework.Game.Abstractions.scene;

/// <summary>
/// 场景路由守卫接口，用于在场景切换前进行权限检查和条件验证。
/// 实现此接口可以拦截场景的进入和离开操作。
/// </summary>
public interface ISceneRouteGuard
{
    /// <summary>
    /// 获取守卫的执行优先级。
    /// 数值越小优先级越高，越先执行。
    /// 建议范围：-1000 到 1000。
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// 获取守卫是否可以中断后续守卫的执行。
    /// true 表示当前守卫通过后，可以跳过后续守卫直接允许操作。
    /// false 表示即使当前守卫通过，仍需执行所有后续守卫。
    /// </summary>
    bool CanInterrupt { get; }

    /// <summary>
    /// 异步检查是否允许进入指定场景。
    /// </summary>
    /// <param name="sceneKey">目标场景的唯一标识符。</param>
    /// <param name="param">场景进入参数，可能包含初始化数据或上下文信息。</param>
    /// <returns>如果允许进入则返回 true，否则返回 false。</returns>
    Task<bool> CanEnterAsync(string sceneKey, ISceneEnterParam? param);

    /// <summary>
    /// 异步检查是否允许离开指定场景。
    /// </summary>
    /// <param name="sceneKey">当前场景的唯一标识符。</param>
    /// <returns>如果允许离开则返回 true，否则返回 false。</returns>
    Task<bool> CanLeaveAsync(string sceneKey);
}