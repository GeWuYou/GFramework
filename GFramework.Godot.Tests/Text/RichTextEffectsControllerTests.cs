using GFramework.Godot.Text;
using Godot;
using Array = Godot.Collections.Array;

namespace GFramework.Godot.Tests.Text;

/// <summary>
///     <see cref="RichTextEffectsController" /> 的纯托管行为测试。
/// </summary>
[TestFixture]
public sealed class RichTextEffectsControllerTests
{
    /// <summary>
    ///     验证启用框架效果时会开启宿主 BBCode，并在 Profile 为空时回退到内置默认配置。
    /// </summary>
    [Test]
    public void RefreshEffects_Should_Enable_Bbcode_And_Use_BuiltIn_Default_Profile()
    {
        var host = new FakeRichTextEffectHost();
        var registry = new RecordingRegistry();
        var controller = new RichTextEffectsController(
            host,
            () => registry,
            () => null,
            () => true,
            () => false);

        controller.RefreshEffects();

        Assert.That(host.BbcodeEnabled, Is.True);
        Assert.That(registry.CapturedAnimatedEffectsEnabled, Is.False);
        Assert.That(registry.CapturedProfiles, Has.Count.EqualTo(1));
        Assert.That(registry.CapturedProfiles[0].Effects.Select(static entry => entry.Key), Is.EqualTo(new[]
        {
            "green",
            "red",
            "gold",
            "blue",
            "fade_in",
            "sine",
            "jitter",
            "fly_in"
        }));
    }

    /// <summary>
    ///     验证关闭框架效果时不会调用注册表，并会清空宿主的自定义效果集合。
    /// </summary>
    [Test]
    public void RefreshEffects_Should_Clear_CustomEffects_When_Framework_Effects_Are_Disabled()
    {
        var existingEffects = new Array();
        existingEffects.Add("placeholder");

        var host = new FakeRichTextEffectHost
        {
            BbcodeEnabled = true,
            CustomEffects = existingEffects
        };
        var registry = new RecordingRegistry();
        var controller = new RichTextEffectsController(
            host,
            () => registry,
            () => RichTextProfile.CreateBuiltInDefault(),
            () => false,
            () => true);

        controller.RefreshEffects();

        Assert.That(host.BbcodeEnabled, Is.True);
        Assert.That(host.CustomEffects.Count, Is.EqualTo(0));
        Assert.That(registry.CapturedProfiles, Is.Empty);
    }

    /// <summary>
    ///     验证控制器会在每次刷新时读取最新的注册表访问器结果，避免缓存旧注册表。
    /// </summary>
    [Test]
    public void RefreshEffects_Should_Use_The_Current_Registry_From_Accessor()
    {
        var host = new FakeRichTextEffectHost();
        var firstRegistry = new RecordingRegistry();
        var secondRegistry = new RecordingRegistry();
        IRichTextEffectRegistry currentRegistry = firstRegistry;

        var controller = new RichTextEffectsController(
            host,
            () => currentRegistry,
            () => RichTextProfile.CreateBuiltInDefault(),
            () => true,
            () => true);

        controller.RefreshEffects();
        currentRegistry = secondRegistry;
        controller.RefreshEffects();

        Assert.That(firstRegistry.CapturedProfiles, Has.Count.EqualTo(1));
        Assert.That(secondRegistry.CapturedProfiles, Has.Count.EqualTo(1));
    }

    private sealed class FakeRichTextEffectHost : IRichTextEffectHost
    {
        public bool BbcodeEnabled { get; set; }

        public Array CustomEffects { get; set; } = new();
    }

    private sealed class RecordingRegistry : IRichTextEffectRegistry
    {
        public List<RichTextProfile> CapturedProfiles { get; } = [];

        public bool CapturedAnimatedEffectsEnabled { get; private set; }

        public IReadOnlyList<RichTextEffect> CreateEffects(RichTextProfile profile, bool animatedEffectsEnabled)
        {
            CapturedProfiles.Add(profile);
            CapturedAnimatedEffectsEnabled = animatedEffectsEnabled;
            return System.Array.Empty<RichTextEffect>();
        }

        public RichTextEffect? CreateEffect(string key, bool animatedEffectsEnabled)
        {
            return null;
        }
    }
}
