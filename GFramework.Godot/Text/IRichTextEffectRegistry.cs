// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Godot.Text;

/// <summary>
///     富文本效果注册表，负责把配置中的效果键解析为可安装的 <see cref="RichTextEffect" /> 实例。
/// </summary>
/// <remarks>
///     <see cref="RichTextEffectsController" /> 会在 <see cref="GfRichTextLabel" /> 就绪或显式刷新时调用该注册表，重建
///     <see cref="RichTextLabel.CustomEffects" />。
///     该抽象存在的目的，是把“配置里声明了哪些效果”与“这些效果如何实例化”解耦，使宿主节点、配置资源和测试替身都不必
///     直接依赖具体内置效果类型。
///     当项目只需要组合现有标签时，应优先使用 <see cref="RichTextProfile" />；当项目需要替换内置映射、注入自定义
///     <see cref="RichTextEffect" />，或按“动态效果是否启用”的边界切换效果实现时，应实现该接口。
/// </remarks>
public interface IRichTextEffectRegistry
{
    /// <summary>
    ///     根据指定配置创建完整的效果实例集合。
    ///     该方法会在每次宿主刷新时执行，因此实现应保持可重复、确定且与宿主生命周期兼容。
    /// </summary>
    /// <param name="profile">效果组合配置。</param>
    /// <param name="animatedEffectsEnabled">当前是否允许字符级动态效果生效。</param>
    /// <returns>可直接写入 <see cref="RichTextLabel.CustomEffects" /> 的效果实例集合。</returns>
    /// <exception cref="ArgumentNullException">
    ///     当 <paramref name="profile" /> 为 <see langword="null" /> 且实现不接受空配置时抛出。
    /// </exception>
    IReadOnlyList<RichTextEffect> CreateEffects(RichTextProfile profile, bool animatedEffectsEnabled);

    /// <summary>
    ///     根据单个效果键创建对应效果实例。
    ///     该方法主要用于将配置声明的 key 映射为宿主可安装的单个效果对象。
    /// </summary>
    /// <param name="key">效果键。</param>
    /// <param name="animatedEffectsEnabled">当前是否允许字符级动态效果生效。</param>
    /// <returns>解析成功时返回效果实例；否则返回 <see langword="null" />。</returns>
    RichTextEffect? CreateEffect(string key, bool animatedEffectsEnabled);
}
