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

using GFramework.Core.system;
using GFramework.Game.Abstractions.scene;

namespace GFramework.Game.scene;

/// <summary>
/// 场景路由基类，提供场景切换和卸载的基础功能。
/// 实现了 <see cref="ISceneRouter"/> 接口，用于管理场景的加载、替换和卸载操作。
/// </summary>
public abstract class SceneRouterBase
    : AbstractSystem, ISceneRouter
{
    /// <summary>
    /// 当前绑定的场景根节点。
    /// </summary>
    protected ISceneRoot? Root;

    /// <summary>
    /// 当前激活场景的键值。
    /// </summary>
    public string? CurrentKey { get; private set; }

    /// <summary>
    /// 绑定场景根节点。
    /// </summary>
    /// <param name="root">要绑定的场景根节点。</param>
    public void BindRoot(ISceneRoot root)
    {
        Root = root;
    }

    /// <summary>
    /// 替换当前场景为指定键值的新场景。
    /// 在替换前后会调用相应的虚方法以支持扩展逻辑。
    /// </summary>
    /// <param name="sceneKey">目标场景的键值。</param>
    public void Replace(string sceneKey)
    {
        // 调用替换前的钩子方法
        OnBeforeReplace(sceneKey);

        // 执行场景替换操作
        Root!.Replace(sceneKey);

        // 更新当前场景键值
        CurrentKey = sceneKey;

        // 调用替换后的钩子方法
        OnAfterReplace(sceneKey);
    }

    /// <summary>
    /// 卸载当前场景，并将当前场景键值置为空。
    /// </summary>
    public void Unload()
    {
        // 执行场景卸载操作
        Root!.Unload();

        // 清空当前场景键值
        CurrentKey = null;
    }

    /// <summary>
    /// 场景替换前的虚方法，可在子类中重写以实现自定义逻辑。
    /// </summary>
    /// <param name="key">即将被替换的场景键值。</param>
    protected virtual void OnBeforeReplace(string key)
    {
    }

    /// <summary>
    /// 场景替换后的虚方法，可在子类中重写以实现自定义逻辑。
    /// </summary>
    /// <param name="key">已替换的场景键值。</param>
    protected virtual void OnAfterReplace(string key)
    {
    }
}