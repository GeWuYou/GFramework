// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Pause;
using Godot;

namespace GFramework.Godot.Pause;

/// <summary>
/// Godot 引擎的暂停处理器
/// 响应暂停栈状态变化，控制 SceneTree.Paused
/// </summary>
public class GodotPauseHandler : IPauseHandler
{
    private readonly SceneTree _tree;

    /// <summary>
    /// 创建 Godot 暂停处理器
    /// </summary>
    /// <param name="tree">场景树</param>
    public GodotPauseHandler(SceneTree tree)
    {
        _tree = tree ?? throw new ArgumentNullException(nameof(tree));
    }

    /// <summary>
    /// 处理器优先级
    /// </summary>
    public int Priority => 0;

    /// <summary>
    /// 当暂停状态变化时调用
    /// </summary>
    /// <param name="group">暂停组</param>
    /// <param name="isPaused">是否暂停</param>
    public void OnPauseStateChanged(PauseGroup group, bool isPaused)
    {
        // 只有 Global 组影响 Godot 的全局暂停
        if (group == PauseGroup.Global)
        {
            _tree.Paused = isPaused;
            GD.Print($"[GodotPauseHandler] SceneTree.Paused = {isPaused}");
        }
    }
}