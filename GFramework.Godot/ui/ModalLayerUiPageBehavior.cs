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
///     模态层 UI 行为 - 可重入但需谨慎,带遮罩阻止下层交互
/// </summary>
public class ModalLayerUiPageBehavior<T>(T owner, string key) : CanvasItemUiPageBehaviorBase<T>(owner, key)
    where T : CanvasItem
{
    public override UiLayer Layer => UiLayer.Modal;
    public override bool IsReentrant => true; // ✅ 支持重入(如多层确认弹窗)
    public override bool IsModal => true; // 模态窗口
    public override bool BlocksInput => true; // 必须阻止下层交互

    /// <summary>
    ///     模态窗口显示时,可以添加遮罩逻辑
    /// </summary>
    public override void OnShow()
    {
        base.OnShow();
        // TODO: 可在此添加半透明遮罩层
        // AddModalMask();
    }

    public override void OnHide()
    {
        // TODO: 移除遮罩层
        // RemoveModalMask();
        base.OnHide();
    }
}