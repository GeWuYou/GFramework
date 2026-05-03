// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Godot.Text;

/// <summary>
///     抽象可被富文本效果控制器驱动的宿主。
///     该接口把装配决策从 Godot 原生 <see cref="RichTextLabel" /> 生命周期中解耦出来，便于在纯托管测试中验证开关、
///     配置回退和注册表替换行为。
/// </summary>
internal interface IRichTextEffectHost
{
    /// <summary>
    ///     获取或设置宿主是否启用 BBCode 解析。
    /// </summary>
    bool BbcodeEnabled { get; set; }

    /// <summary>
    ///     使用给定的配置和动画开关重建宿主上的自定义富文本效果。
    ///     纯托管控制器只负责组合刷新参数，适配层负责在真正需要时解析注册表、实例化 Godot 效果对象并写回宿主。
    /// </summary>
    /// <param name="profile">需要安装的纯托管效果计划。</param>
    /// <param name="animatedEffectsEnabled">当前是否允许字符级动态效果生效。</param>
    void ApplyEffects(RichTextEffectPlan profile, bool animatedEffectsEnabled);

    /// <summary>
    ///     清空当前安装到宿主上的自定义富文本效果集合。
    ///     关闭框架效果时，控制器会通过该方法显式撤销之前安装的效果。
    /// </summary>
    void ClearCustomEffects();
}
