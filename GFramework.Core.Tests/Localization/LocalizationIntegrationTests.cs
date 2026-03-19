using System.IO;
using GFramework.Core.Abstractions.Localization;
using GFramework.Core.Localization;

namespace GFramework.Core.Tests.Localization;

[TestFixture]
public class LocalizationIntegrationTests
{
    [SetUp]
    public void Setup()
    {
        _testDataPath = Path.Combine(Path.GetTempPath(), $"gframework_localization_{Guid.NewGuid():N}");
        CreateTestLocalizationFiles(_testDataPath);

        var config = new LocalizationConfig
        {
            DefaultLanguage = "eng",
            FallbackLanguage = "eng",
            LocalizationPath = _testDataPath,
            EnableHotReload = false,
            ValidateOnLoad = false
        };

        _manager = new LocalizationManager(config);
        _manager.Initialize();
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_testDataPath))
        {
            Directory.Delete(_testDataPath, recursive: true);
        }
    }

    private LocalizationManager? _manager;
    private string _testDataPath = null!;

    private static void CreateTestLocalizationFiles(string rootPath)
    {
        var engPath = Path.Combine(rootPath, "eng");
        var zhsPath = Path.Combine(rootPath, "zhs");
        Directory.CreateDirectory(engPath);
        Directory.CreateDirectory(zhsPath);

        File.WriteAllText(Path.Combine(engPath, "common.json"), """
                                                                {
                                                                  "game.title": "My Game",
                                                                  "ui.message.welcome": "Welcome, {playerName}!",
                                                                  "status.health": "Health: {current}/{max}"
                                                                }
                                                                """);

        File.WriteAllText(Path.Combine(zhsPath, "common.json"), """
                                                                {
                                                                  "game.title": "我的游戏",
                                                                  "ui.message.welcome": "欢迎, {playerName}!",
                                                                  "status.health": "生命值: {current}/{max}"
                                                                }
                                                                """);
    }

    [Test]
    public void GetText_ShouldReturnEnglishText()
    {
        // Act
        var title = _manager!.GetText("common", "game.title");

        // Assert
        Assert.That(title, Is.EqualTo("My Game"));
    }

    [Test]
    public void GetString_WithVariable_ShouldFormatCorrectly()
    {
        // Act
        var message = _manager!.GetString("common", "ui.message.welcome")
            .WithVariable("playerName", "Alice")
            .Format();

        // Assert
        Assert.That(message, Is.EqualTo("Welcome, Alice!"));
    }

    [Test]
    public void SetLanguage_ShouldSwitchToChineseText()
    {
        // Act
        _manager!.SetLanguage("zhs");
        var title = _manager.GetText("common", "game.title");

        // Assert
        Assert.That(title, Is.EqualTo("我的游戏"));
    }

    [Test]
    public void GetString_WithMultipleVariables_ShouldFormatCorrectly()
    {
        // Act
        var health = _manager!.GetString("common", "status.health")
            .WithVariable("current", 80)
            .WithVariable("max", 100)
            .Format();

        // Assert
        Assert.That(health, Is.EqualTo("Health: 80/100"));
    }

    [Test]
    public void LanguageChange_ShouldTriggerCallback()
    {
        // Arrange
        var callbackTriggered = false;
        var newLanguage = string.Empty;

        _manager!.SubscribeToLanguageChange(lang =>
        {
            callbackTriggered = true;
            newLanguage = lang;
        });

        // Act
        _manager.SetLanguage("zhs");

        // Assert
        Assert.That(callbackTriggered, Is.True);
        Assert.That(newLanguage, Is.EqualTo("zhs"));
    }

    [Test]
    public void AvailableLanguages_ShouldContainBothLanguages()
    {
        // Act
        var languages = _manager!.AvailableLanguages;

        // Assert
        Assert.That(languages, Contains.Item("eng"));
        Assert.That(languages, Contains.Item("zhs"));
    }
}