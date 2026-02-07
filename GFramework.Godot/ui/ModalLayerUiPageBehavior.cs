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
///     模态层 UI 行为类，用于管理模态界面的行为。
///     此类继承自 CanvasItemUiPageBehaviorBase，提供模态层特有的功能：
///     - 支持可重入（IsReentrant = true）
///     - 带有遮罩以阻止下层交互（BlocksInput = true）
///     - 属于模态层级（Layer = UiLayer.Modal）
/// </summary>
/// <typeparam name="T">拥有者类型，必须是 CanvasItem 的子类</typeparam>
/// <param name="owner">当前行为的拥有者对象</param>
/// <param name="key">用于标识此行为的键值</param>
public class ModalLayerUiPageBehavior<T>(T owner, string key) : CanvasItemUiPageBehaviorBase<T>(owner, key)
    where T : CanvasItem
{
    /// <summary>
    ///     获取当前 UI 所属的层级，此处固定为模态层。
    /// </summary>
    public override UiLayer Layer => UiLayer.Modal;

    /// <summary>
    ///     指示当前 UI 是否支持可重入。设置为 true 表示允许重复进入同一界面。
    /// </summary>
    public override bool IsReentrant => true;

    /// <summary>
    ///     指示当前 UI 是否为模态界面。设置为 true 表示该界面会阻止用户与下层界面交互。
    /// </summary>
    public override bool IsModal => true;

    /// <summary>
    ///     指示当前 UI 是否阻止输入事件传递到下层界面。设置为 true 表示启用遮罩功能。
    /// </summary>
    public override bool BlocksInput => true;
}