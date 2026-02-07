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

using GFramework.Game.Abstractions.enums;
using GFramework.Game.Abstractions.ui;
using Godot;

namespace GFramework.Godot.ui;

/// <summary>
///     UI 页面行为工厂类,根据层级创建对应的 Behavior 实例
/// </summary>
public static class UiPageBehaviorFactory
{
    /// <summary>
    ///     创建指定层级的 UI 页面行为实例
    /// </summary>
    /// <typeparam name="T">CanvasItem 类型</typeparam>
    /// <param name="owner">视图节点</param>
    /// <param name="key">UI 标识键</param>
    /// <param name="layer">目标层级</param>
    /// <returns>对应层级的 IUiPageBehavior 实例</returns>
    public static IUiPageBehavior Create<T>(T owner, string key, UiLayer layer)
        where T : CanvasItem
    {
        return layer switch
        {
            UiLayer.Page => new PageLayerUiPageBehavior<T>(owner, key),
            UiLayer.Overlay => new OverlayLayerUiPageBehavior<T>(owner, key),
            UiLayer.Modal => new ModalLayerUiPageBehavior<T>(owner, key),
            UiLayer.Toast => new ToastLayerUiPageBehavior<T>(owner, key),
            UiLayer.Topmost => new TopmostLayerUiPageBehavior<T>(owner, key),
            _ => throw new ArgumentException($"Unsupported UI layer: {layer}")
        };
    }
}