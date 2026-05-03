// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Godot.Text.Effects;

/// <summary>
///     富文本效果基类，提供统一的标签命名和环境参数读取辅助逻辑。
///     该基类只负责 Godot 适配细节，不承载业务语义分层。
/// </summary>
[Tool]
public abstract partial class RichTextEffectBase : RichTextEffect
{
    /// <summary>
    ///     获取当前效果对应的 BBCode 标签名。
    /// </summary>
    protected abstract string TagName { get; }

    /// <summary>
    ///     获取 Godot 识别当前效果所需的 `bbcode` 属性。
    ///     属性名使用小写是 Godot `RichTextEffect` 的约定，不是框架对公共成员命名的放宽。
    /// </summary>
    public string bbcode => TagName;

    /// <summary>
    ///     尝试从字符环境参数中读取布尔值。
    /// </summary>
    /// <param name="transform">当前字符变换上下文。</param>
    /// <param name="key">参数键。</param>
    /// <param name="defaultValue">读取失败时使用的默认值。</param>
    /// <returns>最终布尔值；当环境参数不存在或类型不是 <see cref="Variant.Type.Bool" /> 时返回默认值。</returns>
    protected bool GetBool(CharFXTransform transform, string key, bool defaultValue = false)
    {
        if (transform.Env.TryGetValue(Variant.From(key), out var value) &&
            value.VariantType == Variant.Type.Bool)
        {
            return value.AsBool();
        }

        return defaultValue;
    }

    /// <summary>
    ///     尝试从字符环境参数中读取浮点值。
    /// </summary>
    /// <param name="transform">当前字符变换上下文。</param>
    /// <param name="key">参数键。</param>
    /// <param name="defaultValue">读取失败时使用的默认值。</param>
    /// <returns>
    ///     最终浮点值；当环境参数不存在，或类型既不是 <see cref="Variant.Type.Float" /> 也不是
    ///     <see cref="Variant.Type.Int" /> 时返回默认值。
    /// </returns>
    protected float GetFloat(CharFXTransform transform, string key, float defaultValue)
    {
        if (transform.Env.TryGetValue(Variant.From(key), out var value) &&
            (value.VariantType == Variant.Type.Float || value.VariantType == Variant.Type.Int))
        {
            return (float)value.AsDouble();
        }

        return defaultValue;
    }

    /// <summary>
    ///     尝试从字符环境参数中读取颜色值。
    /// </summary>
    /// <param name="transform">当前字符变换上下文。</param>
    /// <param name="key">参数键。</param>
    /// <param name="defaultValue">读取失败时使用的默认值。</param>
    /// <returns>最终颜色值。</returns>
    protected Color GetColor(CharFXTransform transform, string key, Color defaultValue)
    {
        if (transform.Env.TryGetValue(Variant.From(key), out var value) &&
            value.VariantType == Variant.Type.Color)
        {
            return (Color)value;
        }

        return defaultValue;
    }

    /// <summary>
    ///     从字符环境参数中应用可见性开关。
    /// </summary>
    /// <param name="transform">当前字符变换上下文。</param>
    /// <param name="defaultValue">默认可见性。</param>
    protected void ApplyVisibility(CharFXTransform transform, bool defaultValue = true)
    {
        transform.Visible = GetBool(transform, "visible", defaultValue);
    }
}
