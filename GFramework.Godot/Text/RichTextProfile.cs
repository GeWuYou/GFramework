// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Godot.Text;

/// <summary>
///     描述一个富文本效果组合配置。
///     该资源是 Godot 编辑器与场景系统使用的配置载体；运行时控制器会先把它转换为
///     <see cref="RichTextEffectPlan" />，再在纯托管边界内完成刷新决策。
/// </summary>
[GlobalClass]
public partial class RichTextProfile : Resource
{
    /// <summary>
    ///     获取或设置当前配置启用的效果条目集合。
    /// </summary>
    [Export]
    public RichTextEffectEntry[] Effects { get; set; } = [];

    /// <summary>
    ///     创建包含全部内置效果的默认配置。
    ///     该方法为第一阶段提供零配置可用的回退组合。
    /// </summary>
    /// <returns>包含全部内置效果键的默认配置。</returns>
    public static RichTextProfile CreateBuiltInDefault()
    {
        return FromPlan(RichTextEffectPlan.CreateBuiltInDefault());
    }

    /// <summary>
    ///     从纯托管效果计划创建对应的 Godot 资源配置。
    ///     该转换只应发生在真正需要与 Godot 宿主或公开注册表交互的适配层边界上。
    /// </summary>
    /// <param name="plan">待转换的纯托管效果计划。</param>
    /// <returns>与计划等价的 Godot 资源配置。</returns>
    /// <exception cref="ArgumentNullException">
    ///     当 <paramref name="plan" /> 为 <see langword="null" /> 时抛出。
    /// </exception>
    internal static RichTextProfile FromPlan(RichTextEffectPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var profile = new RichTextProfile();
        profile.Effects = plan.Effects
            .Select(static entry => new RichTextEffectEntry
            {
                Key = entry.Key,
                Enabled = entry.Enabled
            })
            .ToArray();
        return profile;
    }
}
