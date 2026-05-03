// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Godot.Text.Effects;

/// <summary>
///     为文本提供正弦波形的上下漂浮效果。
/// </summary>
[GlobalClass]
[Tool]
public partial class RichTextSineEffect : RichTextEffectBase
{
    private readonly bool _animatedEffectsEnabled;

    /// <summary>
    ///     初始化正弦效果。
    /// </summary>
    /// <param name="animatedEffectsEnabled">是否允许动态效果实际生效。</param>
    public RichTextSineEffect(bool animatedEffectsEnabled = true)
    {
        _animatedEffectsEnabled = animatedEffectsEnabled;
    }

    /// <summary>
    ///     获取标签名。
    /// </summary>
    protected override string TagName => "sine";

    /// <summary>
    ///     应用正弦位移。
    /// </summary>
    /// <param name="charFx">当前字符上下文。</param>
    /// <returns>始终返回 <see langword="true" />。</returns>
    public override bool _ProcessCustomFX(CharFXTransform charFx)
    {
        if (!_animatedEffectsEnabled)
        {
            return true;
        }

        var amplitude = GetFloat(charFx, "amplitude", 0.8f);
        var frequency = GetFloat(charFx, "frequency", 0.5f);
        var speed = GetFloat(charFx, "speed", 1.5f);
        var phase = (float)(charFx.ElapsedTime * speed + charFx.RelativeIndex * 0.1f);
        var offsetY = amplitude * Mathf.Sin(phase * Mathf.Pi * 2f * frequency);
        charFx.Offset += new Vector2(0f, offsetY);
        ApplyVisibility(charFx);
        return true;
    }
}
