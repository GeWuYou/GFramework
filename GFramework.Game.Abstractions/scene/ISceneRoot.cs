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
/// 场景根接口，定义了场景管理系统的核心功能。
/// 负责管理场景的生命周期、场景栈操作以及场景间的切换控制。
/// </summary>
public interface ISceneRoot
{
    /// <summary>
    /// 获取当前活动的场景行为对象。
    /// 返回null表示当前没有活动场景。
    /// </summary>
    ISceneBehavior? Current { get; }

    /// <summary>
    /// 获取所有已加载场景的行为对象列表。
    /// 列表采用栈式结构，索引0为栈底场景，最后一个元素为当前活动场景。
    /// </summary>
    IReadOnlyList<ISceneBehavior> Stack { get; }

    /// <summary>
    /// 获取场景系统是否正在进行切换操作。
    /// true表示正在执行场景加载、卸载或切换，false表示系统空闲。
    /// </summary>
    bool IsTransitioning { get; }

    /// <summary>
    /// 异步替换当前场景，清空整个场景栈并加载新场景。
    /// 此操作会卸载所有现有场景，然后加载指定的新场景。
    /// </summary>
    /// <param name="key">要加载的场景唯一标识符。</param>
    /// <param name="param">可选的场景进入参数，用于传递初始化数据。</param>
    /// <returns>表示替换操作完成的ValueTask。</returns>
    ValueTask ReplaceAsync(string key, ISceneEnterParam? param = null);

    /// <summary>
    /// 异步压入新场景到场景栈顶部。
    /// 当前场景会被暂停，新场景成为活动场景。
    /// </summary>
    /// <param name="key">要加载的场景唯一标识符。</param>
    /// <param name="param">可选的场景进入参数，用于传递初始化数据。</param>
    /// <returns>表示压入操作完成的ValueTask。</returns>
    ValueTask PushAsync(string key, ISceneEnterParam? param = null);

    /// <summary>
    /// 异步弹出当前场景并恢复下一个场景。
    /// 当前场景会被卸载，栈中的下一个场景变为活动场景。
    /// </summary>
    /// <returns>表示弹出操作完成的ValueTask。</returns>
    ValueTask PopAsync();

    /// <summary>
    /// 异步清空所有已加载的场景。
    /// 卸载场景栈中的所有场景，使系统回到无场景状态。
    /// </summary>
    /// <returns>表示清空操作完成的ValueTask。</returns>
    ValueTask ClearAsync();

    /// <summary>
    /// 异步加载指定场景并返回场景行为实例。
    /// 此方法仅加载场景资源但不激活场景，通常用于预加载或后台加载场景。
    /// </summary>
    /// <param name="sceneKey">要加载的场景唯一标识符。</param>
    /// <returns>表示加载操作完成的ValueTask，包含加载成功的场景行为对象。</returns>
    ValueTask<ISceneBehavior> LoadAsync(string sceneKey);

    /// <summary>
    /// 异步卸载指定的场景行为实例。
    /// 释放场景占用的资源并从系统中移除该场景实例。
    /// </summary>
    /// <param name="scene">要卸载的场景行为实例。</param>
    /// <returns>表示卸载操作完成的ValueTask。</returns>
    ValueTask UnloadAsync(ISceneBehavior scene);
}