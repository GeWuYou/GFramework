// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Godot.Text.Effects;

namespace GFramework.Godot.Text;

/// <summary>
///     默认的富文本效果注册表。
///     该实现仅负责内置效果键的解析，不处理业务层文本构建或配置持久化。
/// </summary>
public sealed class DefaultRichTextEffectRegistry : IRichTextEffectRegistry
{
    /// <summary>
    ///     创建当前配置对应的全部效果实例。
    /// </summary>
    /// <param name="profile">效果组合配置。</param>
    /// <param name="animatedEffectsEnabled">当前是否允许字符级动态效果生效。</param>
    /// <returns>内置效果实例集合。</returns>
    /// <exception cref="ArgumentNullException">
    ///     当 <paramref name="profile" /> 为 <see langword="null" /> 时抛出。
    /// </exception>
    public IReadOnlyList<RichTextEffect> CreateEffects(RichTextProfile profile, bool animatedEffectsEnabled)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var effects = new List<RichTextEffect>(profile.Effects.Length);
        foreach (var entry in profile.Effects)
        {
            if (entry is null || !entry.Enabled || string.IsNullOrWhiteSpace(entry.Key))
            {
                continue;
            }

            var effect = CreateEffect(entry.Key, animatedEffectsEnabled);
            if (effect is not null)
            {
                effects.Add(effect);
            }
        }

        return effects;
    }

    /// <summary>
    ///     根据效果键创建单个效果实例。
    /// </summary>
    /// <param name="key">效果键。</param>
    /// <param name="animatedEffectsEnabled">当前是否允许字符级动态效果生效。</param>
    /// <returns>解析成功时返回效果实例；否则返回 <see langword="null" />。</returns>
    public RichTextEffect? CreateEffect(string key, bool animatedEffectsEnabled)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        return key.Trim().ToLowerInvariant() switch
        {
            "green" => new RichTextGreenEffect(),
            "red" => new RichTextRedEffect(),
            "gold" => new RichTextGoldEffect(),
            "blue" => new RichTextBlueEffect(),
            "fade_in" => new RichTextFadeInEffect(animatedEffectsEnabled),
            "sine" => new RichTextSineEffect(animatedEffectsEnabled),
            "jitter" => new RichTextJitterEffect(animatedEffectsEnabled),
            "fly_in" => new RichTextFlyInEffect(animatedEffectsEnabled),
            _ => null
        };
    }
}
