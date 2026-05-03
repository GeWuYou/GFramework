// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Godot.Text.Effects;

/// <summary>
///     为文本应用金色语义高亮。
/// </summary>
[GlobalClass]
[Tool]
public partial class RichTextGoldEffect : RichTextEffectBase
{
    private static readonly Color GoldColor = new(0.96f, 0.79f, 0.34f, 1.0f);

    /// <summary>
    ///     获取标签名。
    /// </summary>
    protected override string TagName => "gold";

    /// <summary>
    ///     应用金色颜色效果。
    /// </summary>
    /// <param name="charFx">当前字符上下文。</param>
    /// <returns>始终返回 <see langword="true" />。</returns>
    public override bool _ProcessCustomFX(CharFXTransform charFx)
    {
        charFx.Color = GoldColor;
        return true;
    }
}
