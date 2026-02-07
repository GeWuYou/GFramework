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
///     页面层 UI 行为类，用于实现栈式管理的页面行为。
///     此类继承自 CanvasItemUiPageBehaviorBase，提供页面层级的 UI 控制逻辑。
///     特性包括：不可重入、非模态、阻塞输入。
/// </summary>
/// <typeparam name="T">泛型参数，表示拥有此行为的 CanvasItem 类型。</typeparam>
/// <param name="owner">拥有此行为的 CanvasItem 实例。</param>
/// <param name="key">用于标识此行为的唯一键。</param>
public class PageLayerUiPageBehavior<T>(T owner, string key) : CanvasItemUiPageBehaviorBase<T>(owner, key)
    where T : CanvasItem
{
    /// <summary>
    ///     获取当前 UI 行为所属的层级。
    ///     返回值固定为 UiLayer.Page，表示页面层级。
    /// </summary>
    public override UiLayer Layer => UiLayer.Page;

    /// <summary>
    ///     指示当前 UI 行为是否可重入。
    ///     返回值为 false，表示不可重入。
    /// </summary>
    public override bool IsReentrant => false;

    /// <summary>
    ///     指示当前 UI 行为是否为模态。
    ///     返回值为 false，表示非模态。
    /// </summary>
    public override bool IsModal => false;

    /// <summary>
    ///     指示当前 UI 行为是否会阻塞输入。
    ///     返回值为 true，表示会阻塞输入。
    /// </summary>
    public override bool BlocksInput => true;
}