// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Godot.Text;

/// <summary>
///     描述一条富文本效果配置项。
///     该资源只负责声明需要启用的效果键与开关状态，不承担实例创建逻辑。
/// </summary>
[GlobalClass]
public partial class RichTextEffectEntry : Resource
{
    /// <summary>
    ///     获取或设置效果键。
    ///     键值由 <see cref="IRichTextEffectRegistry" /> 解析为具体的 <see cref="RichTextEffect" /> 实例。
    /// </summary>
    [Export]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    ///     获取或设置该配置项是否启用。
    /// </summary>
    [Export]
    public bool Enabled { get; set; } = true;
}
