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
/// 场景根接口，定义了场景树容器的基本操作。
/// 职责单一：管理场景节点的添加和移除。
/// </summary>
public interface ISceneRoot
{
    /// <summary>
    /// 向场景树添加场景节点。
    /// 此方法仅负责将场景添加到场景树中，不涉及场景的加载逻辑。
    /// </summary>
    /// <param name="scene">要添加的场景行为实例。</param>
    void AddScene(ISceneBehavior scene);

    /// <summary>
    /// 从场景树移除场景节点。
    /// 此方法仅负责从场景树中移除场景，不涉及场景的卸载逻辑。
    /// </summary>
    /// <param name="scene">要移除的场景行为实例。</param>
    void RemoveScene(ISceneBehavior scene);
}