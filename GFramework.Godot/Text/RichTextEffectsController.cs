// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Godot.Text;

/// <summary>
///     负责把纯托管效果计划和开关装配为宿主标签的实际效果集合。
///     该控制器是组合式扩展的装配中心，使 <see cref="GfRichTextLabel" /> 保持轻量。
/// </summary>
internal sealed class RichTextEffectsController
{
    private readonly Func<bool> _animatedEffectsEnabledAccessor;
    private readonly Func<bool> _frameworkEffectsEnabledAccessor;
    private readonly IRichTextEffectHost _host;
    private readonly Func<RichTextEffectPlan?> _profileAccessor;

    /// <summary>
    ///     初始化控制器实例。
    /// </summary>
    /// <param name="host">目标富文本标签。</param>
    /// <param name="profileAccessor">当前纯托管效果计划访问器。</param>
    /// <param name="frameworkEffectsEnabledAccessor">框架效果总开关访问器。</param>
    /// <param name="animatedEffectsEnabledAccessor">字符动画开关访问器。</param>
    /// <exception cref="ArgumentNullException">
    ///     当 <paramref name="host" />、<paramref name="profileAccessor" />、
    ///     <paramref name="frameworkEffectsEnabledAccessor" /> 或 <paramref name="animatedEffectsEnabledAccessor" />
    ///     为 <see langword="null" /> 时抛出。
    /// </exception>
    public RichTextEffectsController(
        IRichTextEffectHost host,
        Func<RichTextEffectPlan?> profileAccessor,
        Func<bool> frameworkEffectsEnabledAccessor,
        Func<bool> animatedEffectsEnabledAccessor)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _profileAccessor = profileAccessor ?? throw new ArgumentNullException(nameof(profileAccessor));
        _frameworkEffectsEnabledAccessor = frameworkEffectsEnabledAccessor
                                           ?? throw new ArgumentNullException(nameof(frameworkEffectsEnabledAccessor));
        _animatedEffectsEnabledAccessor = animatedEffectsEnabledAccessor
                                          ?? throw new ArgumentNullException(nameof(animatedEffectsEnabledAccessor));
    }

    /// <summary>
    ///     初始化并立即刷新宿主标签的效果集合。
    /// </summary>
    public void Initialize()
    {
        RefreshEffects();
    }

    /// <summary>
    ///     根据当前配置和开关重建宿主标签上的 <see cref="RichTextLabel.CustomEffects" />。
    /// </summary>
    public void RefreshEffects()
    {
        var frameworkEffectsEnabled = _frameworkEffectsEnabledAccessor();
        if (frameworkEffectsEnabled && !_host.BbcodeEnabled)
        {
            _host.BbcodeEnabled = true;
        }

        if (!frameworkEffectsEnabled)
        {
            _host.ClearCustomEffects();
            return;
        }

        var profile = _profileAccessor() ?? RichTextEffectPlan.CreateBuiltInDefault();
        _host.ApplyEffects(profile, _animatedEffectsEnabledAccessor());
    }
}
