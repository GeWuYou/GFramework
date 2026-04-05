using GFramework.Core.Abstractions.Localization;
using GFramework.Game.Abstractions.Setting;
using GFramework.Game.Abstractions.Setting.Data;
using GFramework.Godot.Setting;
using GFramework.Godot.Setting.Data;

namespace GFramework.Game.Tests.Setting;

/// <summary>
///     覆盖 Godot 本地化设置应用器的语言同步行为，防止持久化语言仅影响 Godot 而未同步框架管理器。
/// </summary>
[TestFixture]
public sealed class GodotLocalizationSettingsTests
{
    [Test]
    public async Task Apply_ShouldSyncEnglishToGodotLocaleAndFrameworkLanguage()
    {
        var manager = new Mock<ILocalizationManager>(MockBehavior.Strict);
        manager.Setup(it => it.SetLanguage("eng"));
        string? appliedLocale = null;

        var applicator = CreateApplicator("English", manager.Object, locale => appliedLocale = locale);

        await applicator.Apply();

        Assert.That(appliedLocale, Is.EqualTo("en"));
        manager.Verify(it => it.SetLanguage("eng"), Times.Once);
    }

    [Test]
    public async Task Apply_ShouldSyncChineseToGodotLocaleAndFrameworkLanguage()
    {
        var manager = new Mock<ILocalizationManager>(MockBehavior.Strict);
        manager.Setup(it => it.SetLanguage("zhs"));
        string? appliedLocale = null;

        var applicator = CreateApplicator("简体中文", manager.Object, locale => appliedLocale = locale);

        await applicator.Apply();

        Assert.That(appliedLocale, Is.EqualTo("zh_CN"));
        manager.Verify(it => it.SetLanguage("zhs"), Times.Once);
    }

    [Test]
    public async Task Apply_ShouldFallbackUnknownLanguageToEnglish()
    {
        var manager = new Mock<ILocalizationManager>(MockBehavior.Strict);
        manager.Setup(it => it.SetLanguage("eng"));
        string? appliedLocale = null;

        var applicator = CreateApplicator("Esperanto", manager.Object, locale => appliedLocale = locale);

        await applicator.Apply();

        Assert.That(appliedLocale, Is.EqualTo("en"));
        manager.Verify(it => it.SetLanguage("eng"), Times.Once);
    }

    private static GodotLocalizationSettings CreateApplicator(
        string language,
        ILocalizationManager manager,
        Action<string> applyGodotLocale)
    {
        var settingsModel = new Mock<ISettingsModel>(MockBehavior.Strict);
        settingsModel.Setup(it => it.GetData<LocalizationSettings>()).Returns(new LocalizationSettings
        {
            Language = language
        });

        return new GodotLocalizationSettings(settingsModel.Object, new LocalizationMap(), () => manager,
            applyGodotLocale);
    }
}