// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Godot.Text.Effects;

/// <summary>
///     为文本提供逐字符飞入效果。
/// </summary>
[GlobalClass]
[Tool]
public partial class RichTextFlyInEffect : RichTextEffectBase
{
    private readonly bool _animatedEffectsEnabled;

    /// <summary>
    ///     初始化飞入效果。
    /// </summary>
    /// <param name="animatedEffectsEnabled">是否允许动态效果实际生效。</param>
    public RichTextFlyInEffect(bool animatedEffectsEnabled = true)
    {
        _animatedEffectsEnabled = animatedEffectsEnabled;
    }

    /// <summary>
    ///     获取标签名。
    /// </summary>
    protected override string TagName => "fly_in";

    /// <summary>
    ///     应用飞入动画。
    /// </summary>
    /// <param name="charFx">当前字符上下文。</param>
    /// <returns>始终返回 <see langword="true" />。</returns>
    public override bool _ProcessCustomFX(CharFXTransform charFx)
    {
        if (!_animatedEffectsEnabled)
        {
            return true;
        }

        var startOffset = new Vector2(
            GetFloat(charFx, "offset_x", 12f),
            GetFloat(charFx, "offset_y", 0f));
        var speed = GetFloat(charFx, "speed", 3.0f);
        var tick = GetFloat(charFx, "tick", 0.015f);
        var progress = Mathf.Clamp((float)(charFx.ElapsedTime * speed - charFx.RelativeIndex * tick), 0f, 1f);
        var eased = EaseOutQuad(progress);

        charFx.Offset += startOffset * (1f - eased);

        var color = charFx.Color;
        color.A = eased;
        charFx.Color = color;
        ApplyVisibility(charFx);
        return true;
    }

    /// <summary>
    ///     计算二次缓出值。
    /// </summary>
    /// <param name="value">归一化进度。</param>
    /// <returns>缓出后的进度。</returns>
    private static float EaseOutQuad(float value)
    {
        return 1f - (1f - value) * (1f - value);
    }
}
