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
///     顶层 UI 行为 - 不可重入,最高优先级,用于系统级弹窗
/// </summary>
public class TopmostLayerUiPageBehavior<T>(T owner, string key) : CanvasItemUiPageBehaviorBase<T>(owner, key)
    where T : CanvasItem
{
    public override UiLayer Layer => UiLayer.Topmost;
    public override bool IsReentrant => false; // ❌ 顶层不支持重入
    public override bool IsModal => true; // 顶层通常是模态的
    public override bool BlocksInput => true; // 必须阻止所有下层交互

    /// <summary>
    ///     顶层显示时,可以禁用所有下层 UI
    /// </summary>
    public override void OnShow()
    {
        base.OnShow();
        // TODO: 可在此禁用其他所有层级
        // DisableAllLowerLayers();
    }

    public override void OnHide()
    {
        // TODO: 恢复其他层级
        // EnableAllLowerLayers();
        base.OnHide();
    }
}