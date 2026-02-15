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

using GFramework.Game.Abstractions.scene;
using Godot;

namespace GFramework.Godot.scene;

/// <summary>
///     场景行为工厂类，根据节点类型自动创建合适的场景行为实例。
///     使用模式匹配选择最适合的行为类型。
/// </summary>
public static class SceneBehaviorFactory
{
    /// <summary>
    ///     根据节点类型创建对应的场景行为实例。
    /// </summary>
    /// <typeparam name="T">节点类型，必须继承自 Node。</typeparam>
    /// <param name="owner">场景节点的所有者实例。</param>
    /// <param name="key">场景的唯一标识键。</param>
    /// <returns>创建的场景行为实例。</returns>
    public static ISceneBehavior Create<T>(T owner, string key)
        where T : Node
    {
        return owner switch
        {
            Node2D node2D => new Node2DSceneBehavior(node2D, key),
            Node3D node3D => new Node3DSceneBehavior(node3D, key),
            Control control => new ControlSceneBehavior(control, key),
            _ => new GenericSceneBehavior(owner, key)
        };
    }
}