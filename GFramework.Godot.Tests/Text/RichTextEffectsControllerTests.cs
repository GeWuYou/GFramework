// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

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
        var controller = new RichTextEffectsController(
            host,
            () => null,
            () => true,
            () => false);

        controller.RefreshEffects();

        Assert.That(host.BbcodeEnabled, Is.True);
        Assert.That(host.CapturedAnimatedEffectsEnabled, Has.Count.EqualTo(1));
        Assert.That(host.CapturedAnimatedEffectsEnabled[0], Is.False);
        Assert.That(host.CapturedProfiles, Has.Count.EqualTo(1));
        Assert.That(host.CapturedProfiles[0].Effects.Select(static entry => entry.Key), Is.EqualTo(new[]
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
    ///     验证关闭框架效果时不会触发新的效果安装，并会清空宿主的自定义效果集合。
    /// </summary>
    [Test]
    public void RefreshEffects_Should_Clear_CustomEffects_When_Framework_Effects_Are_Disabled()
    {
        var host = new FakeRichTextEffectHost
        {
            BbcodeEnabled = true
        };
        host.SimulateInstalledEffects();
        var controller = new RichTextEffectsController(
            host,
            () => RichTextEffectPlan.CreateBuiltInDefault(),
            () => false,
            () => true);

        controller.RefreshEffects();

        Assert.That(host.BbcodeEnabled, Is.True);
        Assert.That(host.CustomEffectsInstalled, Is.False);
        Assert.That(host.ClearCustomEffectsCallCount, Is.EqualTo(1));
        Assert.That(host.CapturedProfiles, Is.Empty);
    }

    /// <summary>
    ///     验证控制器会在每次刷新时读取最新的配置访问器结果，避免缓存旧配置。
    /// </summary>
    [Test]
    public void RefreshEffects_Should_Use_The_Current_Profile_From_Accessor()
    {
        var host = new FakeRichTextEffectHost();
        var firstProfile = new RichTextEffectPlan(
        [
            new RichTextEffectPlanEntry("green")
        ]);
        var secondProfile = new RichTextEffectPlan(
        [
            new RichTextEffectPlanEntry("gold")
        ]);
        RichTextEffectPlan? currentProfile = firstProfile;

        var controller = new RichTextEffectsController(
            host,
            () => currentProfile,
            () => true,
            () => true);

        controller.RefreshEffects();
        currentProfile = secondProfile;
        controller.RefreshEffects();

        Assert.That(host.CapturedProfiles, Has.Count.EqualTo(2));
        Assert.That(host.CapturedProfiles[0], Is.SameAs(firstProfile));
        Assert.That(host.CapturedProfiles[1], Is.SameAs(secondProfile));
    }

    private sealed class FakeRichTextEffectHost : IRichTextEffectHost
    {
        public List<RichTextEffectPlan> CapturedProfiles { get; } = [];

        public List<bool> CapturedAnimatedEffectsEnabled { get; } = [];

        public bool CustomEffectsInstalled { get; private set; }

        public int ClearCustomEffectsCallCount { get; private set; }
        public bool BbcodeEnabled { get; set; }

        public void ApplyEffects(RichTextEffectPlan profile, bool animatedEffectsEnabled)
        {
            ArgumentNullException.ThrowIfNull(profile);

            CapturedProfiles.Add(profile);
            CapturedAnimatedEffectsEnabled.Add(animatedEffectsEnabled);
            CustomEffectsInstalled = true;
        }

        public void ClearCustomEffects()
        {
            ClearCustomEffectsCallCount++;
            CustomEffectsInstalled = false;
        }

        public void SimulateInstalledEffects()
        {
            CustomEffectsInstalled = true;
        }
    }
}
