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
/// 定义场景路由接口，用于管理场景的切换、卸载以及根节点绑定。
/// </summary>
public interface ISceneRouter
{
    /// <summary>
    /// 获取当前场景的唯一标识符（键）。
    /// </summary>
    /// <value>当前场景的键，如果未加载任何场景则返回 null。</value>
    string? CurrentKey { get; }

    /// <summary>
    /// 替换当前场景为指定键对应的场景。
    /// </summary>
    /// <param name="sceneKey">目标场景的唯一标识符（键）。</param>
    void Replace(string sceneKey);

    /// <summary>
    /// 卸载当前场景。
    /// </summary>
    void Unload();

    /// <summary>
    /// 将指定的场景根节点与当前路由进行绑定。
    /// </summary>
    /// <param name="root">需要绑定的场景根节点。</param>
    void BindRoot(ISceneRoot root);
}