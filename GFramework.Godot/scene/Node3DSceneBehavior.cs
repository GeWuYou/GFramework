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

using Godot;

namespace GFramework.Godot.scene;

/// <summary>
///     Node3D 场景行为类，用于管理 3D 场景节点的生命周期。
///     适用于 MeshInstance3D、Camera3D 等 3D 场景元素。
/// </summary>
public sealed class Node3DSceneBehavior : SceneBehaviorBase<Node3D>
{
    /// <summary>
    ///     初始化 Node3DSceneBehavior 实例。
    /// </summary>
    /// <param name="owner">3D 场景节点的所有者实例。</param>
    /// <param name="key">场景的唯一标识键。</param>
    public Node3DSceneBehavior(Node3D owner, string key)
        : base(owner, key)
    {
    }
}