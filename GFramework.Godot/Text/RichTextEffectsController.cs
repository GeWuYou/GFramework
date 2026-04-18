using Array = Godot.Collections.Array;

namespace GFramework.Godot.Text;

/// <summary>
///     负责把配置、开关和注册表装配为宿主标签的实际效果集合。
///     该控制器是组合式扩展的装配中心，使 <see cref="GfRichTextLabel" /> 保持轻量。
/// </summary>
internal sealed class RichTextEffectsController
{
    private readonly Func<bool> _animatedEffectsEnabledAccessor;
    private readonly Func<bool> _frameworkEffectsEnabledAccessor;
    private readonly IRichTextEffectHost _host;
    private readonly Func<RichTextProfile?> _profileAccessor;
    private readonly Func<IRichTextEffectRegistry> _registryAccessor;

    /// <summary>
    ///     初始化控制器实例。
    /// </summary>
    /// <param name="host">目标富文本标签。</param>
    /// <param name="registryAccessor">当前效果注册表访问器。</param>
    /// <param name="profileAccessor">当前配置访问器。</param>
    /// <param name="frameworkEffectsEnabledAccessor">框架效果总开关访问器。</param>
    /// <param name="animatedEffectsEnabledAccessor">字符动画开关访问器。</param>
    /// <exception cref="ArgumentNullException">
    ///     当 <paramref name="host" />、<paramref name="registryAccessor" />、<paramref name="profileAccessor" />、
    ///     <paramref name="frameworkEffectsEnabledAccessor" /> 或 <paramref name="animatedEffectsEnabledAccessor" />
    ///     为 <see langword="null" /> 时抛出。
    /// </exception>
    public RichTextEffectsController(
        IRichTextEffectHost host,
        Func<IRichTextEffectRegistry> registryAccessor,
        Func<RichTextProfile?> profileAccessor,
        Func<bool> frameworkEffectsEnabledAccessor,
        Func<bool> animatedEffectsEnabledAccessor)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _registryAccessor = registryAccessor ?? throw new ArgumentNullException(nameof(registryAccessor));
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
            _host.CustomEffects = new Array();
            return;
        }

        var profile = _profileAccessor() ?? RichTextProfile.CreateBuiltInDefault();
        var registry = _registryAccessor()
                       ?? throw new InvalidOperationException("The rich text effect registry accessor returned null.");

        var effects = registry.CreateEffects(profile, _animatedEffectsEnabledAccessor());
        var customEffects = new Array();
        foreach (var effect in effects)
        {
            customEffects.Add(effect);
        }

        _host.CustomEffects = customEffects;
    }
}
