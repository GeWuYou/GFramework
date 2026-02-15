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

using GFramework.Core.Abstractions.logging;
using GFramework.Core.extensions;
using GFramework.Core.logging;
using GFramework.Core.utility;
using GFramework.Game.Abstractions.scene;

namespace GFramework.Godot.scene;

/// <summary>
///     Godot 场景工厂类，用于创建场景实例。
///     继承自 AbstractContextUtility 并实现 ISceneFactory 接口。
/// </summary>
public class GodotSceneFactory : AbstractContextUtility, ISceneFactory
{
    /// <summary>
    ///     日志记录器，用于记录调试信息。
    /// </summary>
    private static readonly ILogger Log =
        LoggerFactoryResolver.Provider.CreateLogger(nameof(GodotSceneFactory));

    /// <summary>
    ///     场景注册表，用于管理场景资源。
    /// </summary>
    private IGodotSceneRegistry _registry = null!;

    /// <summary>
    ///     根据指定的场景键创建场景行为实例。
    /// </summary>
    /// <param name="sceneKey">场景的唯一标识符。</param>
    /// <returns>返回创建的场景行为实例。</returns>
    public ISceneBehavior Create(string sceneKey)
    {
        // 从注册表中获取指定场景键对应的 PackedScene
        var scene = _registry.Get(sceneKey);

        // 实例化场景节点
        var node = scene.Instantiate();

        // 检查节点是否实现了 ISceneBehaviorProvider 接口
        if (node is ISceneBehaviorProvider provider)
        {
            var behavior = provider.GetScene();
            Log.Debug("Created scene instance from provider: {0}", sceneKey);
            return behavior;
        }

        // 否则使用工厂自动创建
        var autoBehavior = SceneBehaviorFactory.Create(node, sceneKey);
        Log.Debug("Created scene instance with auto factory: {0}", sceneKey);
        return autoBehavior;
    }

    /// <summary>
    ///     初始化方法，在对象初始化时调用。
    ///     获取并设置场景注册表实例。
    /// </summary>
    protected override void OnInit()
    {
        _registry = this.GetUtility<IGodotSceneRegistry>()!;
    }
}