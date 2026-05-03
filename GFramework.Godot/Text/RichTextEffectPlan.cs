// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Godot.Text;

/// <summary>
///     描述一次富文本效果安装所需的纯托管计划。
///     该类型用于把控制器与测试替身隔离在 Godot runtime 之外，使刷新决策可以在普通 .NET 测试进程中验证。
/// </summary>
internal sealed class RichTextEffectPlan
{
    /// <summary>
    ///     初始化一个富文本效果计划。
    /// </summary>
    /// <param name="effects">计划中声明的效果条目集合。</param>
    /// <exception cref="ArgumentNullException">
    ///     当 <paramref name="effects" /> 为 <see langword="null" /> 时抛出。
    /// </exception>
    public RichTextEffectPlan(IReadOnlyList<RichTextEffectPlanEntry> effects)
    {
        ArgumentNullException.ThrowIfNull(effects);

        Effects = effects.ToArray();
    }

    /// <summary>
    ///     获取当前计划启用的效果条目集合。
    /// </summary>
    public IReadOnlyList<RichTextEffectPlanEntry> Effects { get; }

    /// <summary>
    ///     从 Godot 资源配置转换为纯托管计划。
    /// </summary>
    /// <param name="profile">待转换的资源配置。</param>
    /// <returns>与资源配置等价的纯托管计划。</returns>
    /// <exception cref="ArgumentNullException">
    ///     当 <paramref name="profile" /> 为 <see langword="null" /> 时抛出。
    /// </exception>
    public static RichTextEffectPlan FromProfile(RichTextProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var effects = new RichTextEffectPlanEntry[profile.Effects.Length];
        for (var index = 0; index < profile.Effects.Length; index++)
        {
            var entry = profile.Effects[index];
            effects[index] = entry is null
                ? default
                : new RichTextEffectPlanEntry(entry.Key, entry.Enabled);
        }

        return new RichTextEffectPlan(effects);
    }

    /// <summary>
    ///     创建包含全部内置效果的默认计划。
    /// </summary>
    /// <returns>包含第一阶段全部内置效果键的默认计划。</returns>
    public static RichTextEffectPlan CreateBuiltInDefault()
    {
        return new RichTextEffectPlan(
        [
            new RichTextEffectPlanEntry("green"),
            new RichTextEffectPlanEntry("red"),
            new RichTextEffectPlanEntry("gold"),
            new RichTextEffectPlanEntry("blue"),
            new RichTextEffectPlanEntry("fade_in"),
            new RichTextEffectPlanEntry("sine"),
            new RichTextEffectPlanEntry("jitter"),
            new RichTextEffectPlanEntry("fly_in")
        ]);
    }
}
