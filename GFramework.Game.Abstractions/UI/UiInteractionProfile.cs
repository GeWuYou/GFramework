// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Pause;

namespace GFramework.Game.Abstractions.UI;

/// <summary>
///     描述一个 UI 页面在输入、World 阻断与暂停上的交互契约数据。
/// </summary>
/// <remarks>
///     该类型仅承载抽象层需要共享的页面交互配置，不包含默认值工厂或动作判定等运行时策略。
///     运行时层可在不反向依赖 Abstractions 的前提下，通过专门的 helper 为该 DTO 提供默认值和语义判定。
/// </remarks>
public sealed class UiInteractionProfile
{
    /// <summary>
    ///     声明当前页面要捕获的语义动作集合。
    /// </summary>
    public UiInputActionMask CapturedActions { get; init; } = UiInputActionMask.None;

    /// <summary>
    ///     指示当前页面是否阻断 World 指针输入，例如地图点击或相机拖拽。
    /// </summary>
    public bool BlocksWorldPointerInput { get; init; }

    /// <summary>
    ///     指示当前页面是否阻断 World 语义动作输入，例如 gameplay 快捷键。
    /// </summary>
    public bool BlocksWorldActionInput { get; init; }

    /// <summary>
    ///     指示当前页面的可见性是否应驱动暂停栈。
    /// </summary>
    public UiPauseMode PauseMode { get; init; } = UiPauseMode.None;

    /// <summary>
    ///     当 <see cref="PauseMode" /> 生效时使用的暂停组。
    /// </summary>
    public PauseGroup PauseGroup { get; init; } = PauseGroup.Global;

    /// <summary>
    ///     当场景树暂停时，该页面是否仍需继续处理输入与动画。
    /// </summary>
    public bool ContinueProcessingWhenPaused { get; init; }

    /// <summary>
    ///     页面向暂停栈登记时使用的原因文本。
    /// </summary>
    public string PauseReason { get; init; } = string.Empty;
}
