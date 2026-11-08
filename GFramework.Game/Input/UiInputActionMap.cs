// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Input;
using GFramework.Game.Abstractions.UI;

namespace GFramework.Game.Input;

/// <summary>
///     提供动作名称到 UI 语义动作的默认映射实现。
/// </summary>
/// <remarks>
///     默认映射只负责桥接现有 `UiInputAction` 语义，并通过字符串别名兼容 Godot 常见 `ui_*` 动作命名。
///     更复杂的项目级 action map 可以通过自定义实现覆盖该行为。
/// </remarks>
public sealed class UiInputActionMap : IUiInputActionMap
{
    private static readonly Dictionary<string, UiInputAction> DefaultMappings =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["cancel"] = UiInputAction.Cancel,
            ["ui_cancel"] = UiInputAction.Cancel,
            ["confirm"] = UiInputAction.Confirm,
            ["ui_accept"] = UiInputAction.Confirm,
            ["submit"] = UiInputAction.Confirm,
        };

    /// <inheritdoc />
    public bool TryMap(string actionName, out UiInputAction action)
    {
        if (string.IsNullOrWhiteSpace(actionName))
        {
            action = UiInputAction.None;
            return false;
        }

        return DefaultMappings.TryGetValue(actionName, out action);
    }
}
