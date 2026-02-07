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
using Godot;

namespace GFramework.Godot.ui;

/// <summary>
/// 表示一个用于管理Toast层UI页面行为的泛型类。
/// 此类继承自CanvasItemUiPageBehaviorBase，专门用于处理Toast类型的UI层逻辑。
/// </summary>
/// <typeparam name="T">指定的CanvasItem类型，表示此类的所有者。</typeparam>
/// <param name="owner">当前UI页面行为的所有者对象，必须是CanvasItem的实例。</param>
/// <param name="key">用于标识此UI页面行为的唯一键值。</param>
public class ToastLayerUiPageBehavior<T>(T owner, string key) : CanvasItemUiPageBehaviorBase<T>(owner, key)
    where T : CanvasItem
{
    /// <summary>
    /// 获取当前UI页面行为所属的UI层类型。
    /// 对于Toast层，此属性始终返回UiLayer.Toast。
    /// </summary>
    public override UiLayer Layer => UiLayer.Toast;

    /// <summary>
    /// 指示此UI页面行为是否支持重入（即是否允许在已激活状态下再次被调用）。
    /// Toast层通常允许多次触发，因此此属性返回true。
    /// </summary>
    public override bool IsReentrant => true;

    /// <summary>
    /// 指示此UI页面行为是否为模态（即是否会阻止用户与其他UI交互）。
    /// Toast层通常是非模态的，因此此属性返回false。
    /// </summary>
    public override bool IsModal => false;

    /// <summary>
    /// 指示此UI页面行为是否会阻塞用户输入。
    /// Toast层通常不会阻塞输入，因此此属性返回false。
    /// </summary>
    public override bool BlocksInput => false;
}