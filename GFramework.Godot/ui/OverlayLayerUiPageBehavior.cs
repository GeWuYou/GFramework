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
///     浮层 UI 行为类，用于管理覆盖层、对话框等 UI 元素。
///     该行为支持可重入性，适用于需要叠加显示的场景。
/// </summary>
/// <typeparam name="T">UI 元素的类型，必须继承自 CanvasItem。</typeparam>
public class OverlayLayerUiPageBehavior<T> : CanvasItemUiPageBehaviorBase<T>
    where T : CanvasItem
{
    /// <summary>
    ///     初始化 OverlayLayerUiPageBehavior 实例。
    /// </summary>
    /// <param name="owner">关联的 UI 元素实例。</param>
    /// <param name="key">用于标识该行为的唯一键。</param>
    public OverlayLayerUiPageBehavior(T owner, string key) : base(owner, key)
    {
    }

    /// <summary>
    ///     获取当前 UI 行为所属的层级，固定为浮层（Overlay）。
    /// </summary>
    public override UiLayer Layer => UiLayer.Overlay;

    /// <summary>
    ///     指示该行为是否支持可重入性，始终返回 true。
    /// </summary>
    public override bool IsReentrant => true;

    /// <summary>
    ///     指示该行为是否为模态行为，始终返回 false。
    /// </summary>
    public override bool IsModal => false;

    /// <summary>
    ///     指示该行为是否会阻塞输入，始终返回 false。
    /// </summary>
    public override bool BlocksInput => false;

    /// <summary>
    ///     当浮层被暂停时调用此方法。
    ///     浮层在暂停时不中断处理逻辑（如动画等），仅触发业务层的 OnPause 方法。
    /// </summary>
    public override void OnPause()
    {
        // 浮层不暂停处理,保持动画和交互
        // 只调用业务层的 OnPause
        if (Owner is IUiPage page)
            page.OnPause();
    }
}