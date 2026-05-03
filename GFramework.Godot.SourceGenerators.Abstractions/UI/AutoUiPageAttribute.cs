// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Godot.SourceGenerators.Abstractions.UI;

/// <summary>
///     标记 UI 页面类型，Source Generator 会生成页面行为样板代码。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class AutoUiPageAttribute(string key, string layerName) : Attribute
{
    /// <summary>
    ///     获取 UI 键。
    /// </summary>
    public string Key { get; } = key;

    /// <summary>
    ///     获取 <c>UiLayer</c> 枚举成员名称。
    /// </summary>
    public string LayerName { get; } = layerName;
}
