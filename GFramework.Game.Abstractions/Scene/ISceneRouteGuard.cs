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

using GFramework.Game.Abstractions.Routing;

namespace GFramework.Game.Abstractions.Scene;

/// <summary>
/// 场景路由守卫接口，用于在场景切换前进行权限检查和条件验证。
/// 实现此接口可以拦截场景的进入和离开操作。
/// </summary>
public interface ISceneRouteGuard : IRouteGuard<ISceneBehavior>
{
    /// <summary>
    /// 异步检查是否允许进入指定场景。
    /// </summary>
    /// <param name="sceneKey">目标场景的唯一标识符。</param>
    /// <param name="param">场景进入参数，可能包含初始化数据或上下文信息。</param>
    /// <returns>如果允许进入则返回 true，否则返回 false。</returns>
    ValueTask<bool> CanEnterAsync(string sceneKey, ISceneEnterParam? param);

    /// <summary>
    /// 异步检查是否允许离开指定场景。
    /// 该成员显式细化了通用路由守卫的离开检查，使场景守卫在 API 文档中保持场景语义。
    /// </summary>
    /// <param name="sceneKey">当前场景的唯一标识符。</param>
    /// <returns>如果允许离开则返回 true，否则返回 false。</returns>
    new ValueTask<bool> CanLeaveAsync(string sceneKey);
}