// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Godot.Text;

/// <summary>
///     GFramework 提供的组合式富文本标签宿主。
///     该类型只负责桥接 Godot 的 <see cref="RichTextLabel" /> 与框架的效果装配逻辑，不承载具体效果实现。
/// </summary>
[GlobalClass]
[Tool]
public partial class GfRichTextLabel : RichTextLabel, IRichTextEffectHost
{
    private IRichTextEffectRegistry? _effectRegistry;
    private RichTextEffectsController? _effectsController;

    /// <summary>
    ///     获取或设置当前标签使用的效果配置。
    ///     为空时将回退到内置默认配置。
    /// </summary>
    [Export]
    public RichTextProfile? Profile { get; set; }

    /// <summary>
    ///     获取或设置是否启用框架管理的富文本效果装配。
    ///     关闭后只会停止框架效果安装，不会覆盖调用方手动维护的其他 BBCode 解析状态。
    /// </summary>
    [Export]
    public bool EnableFrameworkEffects { get; set; } = true;

    /// <summary>
    ///     获取或设置是否允许字符级动态效果实际生效。
    ///     关闭后仍然会安装对应标签，使富文本内容保持可解析。
    /// </summary>
    [Export]
    public bool AnimatedEffectsEnabled { get; set; } = true;

    /// <summary>
    ///     获取当前使用的效果注册表。
    /// </summary>
    /// <exception cref="ArgumentNullException">
    ///     当设置值为 <see langword="null" /> 时抛出。
    /// </exception>
    internal IRichTextEffectRegistry EffectRegistry
    {
        get => _effectRegistry ??= new DefaultRichTextEffectRegistry();
        set => _effectRegistry = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    ///     根据控制器提供的配置参数在适配层实例化 Godot 原生效果，并写回标签宿主。
    ///     这样控制器与测试替身不需要直接触碰 <see cref="RichTextEffect" /> 或
    ///     <see cref="global::Godot.Collections.Array" />，而真正依赖 Godot runtime 的工作只发生在节点边界上。
    /// </summary>
    /// <param name="profile">需要安装的纯托管效果计划。</param>
    /// <param name="animatedEffectsEnabled">当前是否允许字符级动态效果生效。</param>
    void IRichTextEffectHost.ApplyEffects(RichTextEffectPlan profile, bool animatedEffectsEnabled)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var registry = EffectRegistry;
        var effects = registry.CreateEffects(RichTextProfile.FromPlan(profile), animatedEffectsEnabled);
        var customEffects = new global::Godot.Collections.Array();
        foreach (var effect in effects)
        {
            customEffects.Add(effect);
        }

        CustomEffects = customEffects;
    }

    /// <summary>
    ///     清空标签当前持有的自定义效果集合。
    /// </summary>
    void IRichTextEffectHost.ClearCustomEffects()
    {
        CustomEffects = new global::Godot.Collections.Array();
    }

    /// <summary>
    ///     节点就绪时初始化控制器并安装效果集合。
    /// </summary>
    public override void _Ready()
    {
        EnsureController().Initialize();
    }

    /// <summary>
    ///     手动刷新框架效果集合。
    ///     当调用方在运行时替换配置或切换动画开关时，可通过该方法同步宿主状态。
    /// </summary>
    public void RefreshFrameworkEffects()
    {
        EnsureController().RefreshEffects();
    }

    /// <summary>
    ///     获取或创建控制器实例。
    /// </summary>
    /// <returns>组合式装配控制器。</returns>
    private RichTextEffectsController EnsureController()
    {
        return _effectsController ??= new RichTextEffectsController(
            this,
            () => Profile is null ? null : RichTextEffectPlan.FromProfile(Profile),
            () => EnableFrameworkEffects,
            () => AnimatedEffectsEnabled);
    }
}
