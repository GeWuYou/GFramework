// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
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
    /// <summary>
    ///     验证应用英文设置时，会同时同步 Godot locale 与框架语言管理器。
    /// </summary>
    /// <returns>表示异步断言完成的任务。</returns>
    [Test]
    public async Task ApplyAsync_ShouldSyncEnglishToGodotLocaleAndFrameworkLanguage()
    {
        var manager = new Mock<ILocalizationManager>(MockBehavior.Strict);
        manager.Setup(it => it.SetLanguage("eng"));
        string? appliedLocale = null;

        var applicator = CreateApplicator("English", manager.Object, locale => appliedLocale = locale);

        await applicator.ApplyAsync();

        Assert.That(appliedLocale, Is.EqualTo("en"));
        manager.Verify(it => it.SetLanguage("eng"), Times.Once);
    }

    /// <summary>
    ///     验证应用简体中文设置时，会同时同步 Godot locale 与框架语言管理器。
    /// </summary>
    /// <returns>表示异步断言完成的任务。</returns>
    [Test]
    public async Task ApplyAsync_ShouldSyncChineseToGodotLocaleAndFrameworkLanguage()
    {
        var manager = new Mock<ILocalizationManager>(MockBehavior.Strict);
        manager.Setup(it => it.SetLanguage("zhs"));
        string? appliedLocale = null;

        var applicator = CreateApplicator("简体中文", manager.Object, locale => appliedLocale = locale);

        await applicator.ApplyAsync();

        Assert.That(appliedLocale, Is.EqualTo("zh_CN"));
        manager.Verify(it => it.SetLanguage("zhs"), Times.Once);
    }

    /// <summary>
    ///     验证未知语言会回退到英文 locale，并同步默认框架语言代码。
    /// </summary>
    /// <returns>表示异步断言完成的任务。</returns>
    [Test]
    public async Task ApplyAsync_ShouldFallbackUnknownLanguageToEnglish()
    {
        var manager = new Mock<ILocalizationManager>(MockBehavior.Strict);
        manager.Setup(it => it.SetLanguage("eng"));
        string? appliedLocale = null;

        var applicator = CreateApplicator("Esperanto", manager.Object, locale => appliedLocale = locale);

        await applicator.ApplyAsync();

        Assert.That(appliedLocale, Is.EqualTo("en"));
        manager.Verify(it => it.SetLanguage("eng"), Times.Once);
    }

    /// <summary>
    ///     同时存在两个模型时，每个应用器只解析其模型显式绑定的上下文。
    /// </summary>
    /// <returns>表示两个独立语言应用完成的任务。</returns>
    [Test]
    public async Task ApplyAsync_ShouldResolveOnlyItsModelsExplicitContext()
    {
        var firstManager = new Mock<ILocalizationManager>(MockBehavior.Strict);
        var secondManager = new Mock<ILocalizationManager>(MockBehavior.Strict);
        firstManager.Setup(manager => manager.SetLanguage("eng"));
        secondManager.Setup(manager => manager.SetLanguage("zhs"));
        var firstContext = new Mock<IArchitectureContext>(MockBehavior.Strict);
        var secondContext = new Mock<IArchitectureContext>(MockBehavior.Strict);
        firstContext.Setup(context => context.GetSystem<ILocalizationManager>()).Returns(firstManager.Object);
        secondContext.Setup(context => context.GetSystem<ILocalizationManager>()).Returns(secondManager.Object);
        var firstModel = new Mock<ISettingsModel>(MockBehavior.Strict);
        var secondModel = new Mock<ISettingsModel>(MockBehavior.Strict);
        firstModel.Setup(model => model.GetContext()).Returns(firstContext.Object);
        secondModel.Setup(model => model.GetContext()).Returns(secondContext.Object);
        firstModel.Setup(model => model.GetData<LocalizationSettings>())
            .Returns(new LocalizationSettings { Language = "English" });
        secondModel.Setup(model => model.GetData<LocalizationSettings>())
            .Returns(new LocalizationSettings { Language = "简体中文" });

        var first = new GodotLocalizationSettings(firstModel.Object, new LocalizationMap(), _ => { });
        var second = new GodotLocalizationSettings(secondModel.Object, new LocalizationMap(), _ => { });
        await first.ApplyAsync();
        await second.ApplyAsync();

        firstManager.Verify(manager => manager.SetLanguage("eng"), Times.Once);
        secondManager.Verify(manager => manager.SetLanguage("zhs"), Times.Once);
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
