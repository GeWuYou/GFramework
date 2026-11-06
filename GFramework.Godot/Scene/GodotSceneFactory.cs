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

using GFramework.Core.Extensions;
using GFramework.Core.Utility;
using GFramework.Game.Abstractions.Scene;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GFramework.Godot.Scene;

/// <summary>
///     Godot 场景工厂类，用于创建场景实例。
///     继承自 AbstractContextUtility 并实现 ISceneFactory 接口。
/// </summary>
public class GodotSceneFactory : AbstractContextUtility, ISceneFactory
{
    /// <summary>
    ///     构造注入的标准日志记录器；未注入时为 null。
    /// </summary>
    private readonly ILogger<GodotSceneFactory>? _injectedLogger;

    /// <summary>
    ///     获取当前生效的日志记录器：优先构造注入，其次回退到上下文提供的记录器，最后退化为空记录器。
    /// </summary>
    private ILogger Log =>
        (ILogger?)_injectedLogger
        ?? (Logger is { } contextLogger ? contextLogger : NullLogger<GodotSceneFactory>.Instance);

    /// <summary>
    ///     场景注册表，用于管理场景资源。
    /// </summary>
    private IGodotSceneRegistry _registry = null!;

    /// <summary>
    ///     创建Godot场景工厂实例。
    /// </summary>
    /// <param name="logger">标准日志记录器；依赖注入容器会自动提供，手动创建时可省略。</param>
    public GodotSceneFactory(ILogger<GodotSceneFactory>? logger = null)
    {
        _injectedLogger = logger;
    }

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
            Log.LogDebug("Created scene instance from provider: {SceneKey}", sceneKey);
            return behavior;
        }

        // 否则使用工厂自动创建
        var autoBehavior = SceneBehaviorFactory.Create(node, sceneKey);
        Log.LogDebug("Created scene instance with auto factory: {SceneKey}", sceneKey);
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