// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Godot.Text.Effects;

/// <summary>
///     为文本提供逐字符淡入效果。
/// </summary>
[GlobalClass]
[Tool]
public partial class RichTextFadeInEffect : RichTextEffectBase
{
    private readonly bool _animatedEffectsEnabled;

    /// <summary>
    ///     初始化淡入效果。
    /// </summary>
    /// <param name="animatedEffectsEnabled">是否允许动态效果实际生效。</param>
    public RichTextFadeInEffect(bool animatedEffectsEnabled = true)
    {
        _animatedEffectsEnabled = animatedEffectsEnabled;
    }

    /// <summary>
    ///     获取标签名。
    /// </summary>
    protected override string TagName => "fade_in";

    /// <summary>
    ///     应用淡入动画。
    /// </summary>
    /// <param name="charFx">当前字符上下文。</param>
    /// <returns>始终返回 <see langword="true" />。</returns>
    public override bool _ProcessCustomFX(CharFXTransform charFx)
    {
        if (!_animatedEffectsEnabled)
        {
            return true;
        }

        var speed = GetFloat(charFx, "speed", 4.0f);
        var tick = GetFloat(charFx, "tick", 0.01f);
        var progress = (float)(charFx.ElapsedTime * speed - charFx.RelativeIndex * tick);
        var color = charFx.Color;
        color.A = Mathf.Clamp(progress, 0f, 1f);
        charFx.Color = color;
        ApplyVisibility(charFx);
        return true;
    }
}
