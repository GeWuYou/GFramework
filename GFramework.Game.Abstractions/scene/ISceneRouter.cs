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

using GFramework.Core.Abstractions.system;

namespace GFramework.Game.Abstractions.scene;

/// <summary>
/// 场景路由接口，继承自系统接口。
/// 负责管理场景的导航、切换、生命周期控制以及与场景根节点的绑定操作。
/// 提供完整的场景栈管理和路由功能。
/// </summary>
public interface ISceneRouter : ISystem
{
    /// <summary>
    /// 获取当前活动的场景行为对象。
    /// 返回null表示当前没有活动场景。
    /// </summary>
    ISceneBehavior? Current { get; }

    /// <summary>
    /// 获取当前活动场景的唯一标识键值。
    /// 返回null表示当前没有活动场景。
    /// </summary>
    string? CurrentKey { get; }

    /// <summary>
    /// 获取场景行为对象的只读列表，表示当前的场景栈结构。
    /// 列表中第一个元素为栈底场景，最后一个元素为当前活动场景。
    /// </summary>
    IReadOnlyList<ISceneBehavior> Stack { get; }

    /// <summary>
    /// 获取场景路由器是否正在进行场景切换操作。
    /// true表示正在执行场景加载、卸载或切换，false表示系统空闲。
    /// </summary>
    bool IsTransitioning { get; }

    /// <summary>
    /// 绑定场景根节点，建立路由与场景管理器的连接。
    /// </summary>
    /// <param name="root">要绑定的场景根节点实例。</param>
    void BindRoot(ISceneRoot root);

    /// <summary>
    /// 异步替换当前所有场景，清空整个场景栈并加载新的场景。
    /// 此操作会卸载所有现有场景，然后加载指定的新场景。
    /// </summary>
    /// <param name="sceneKey">要加载的场景唯一标识符。</param>
    /// <param name="param">可选的场景进入参数，用于传递初始化数据。</param>
    /// <returns>表示替换操作完成的ValueTask。</returns>
    ValueTask ReplaceAsync(
        string sceneKey,
        ISceneEnterParam? param = null);

    /// <summary>
    /// 异步压入新场景到场景栈顶部。
    /// 当前场景会被暂停，新场景成为活动场景。
    /// </summary>
    /// <param name="sceneKey">要加载的场景唯一标识符。</param>
    /// <param name="param">可选的场景进入参数，用于传递初始化数据。</param>
    /// <returns>表示压入操作完成的ValueTask。</returns>
    ValueTask PushAsync(
        string sceneKey,
        ISceneEnterParam? param = null);

    /// <summary>
    /// 异步弹出当前场景并恢复栈中的下一个场景。
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
    /// 检查指定场景是否存在于当前场景栈中。
    /// </summary>
    /// <param name="sceneKey">要检查的场景唯一标识符。</param>
    /// <returns>true表示场景在栈中存在，false表示不存在。</returns>
    bool Contains(string sceneKey);
}