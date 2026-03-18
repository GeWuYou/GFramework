using GFramework.Core.Localization;

namespace GFramework.Core.Tests.Localization;

[TestFixture]
public class LocalizationIntegrationTests
{
    private LocalizationManager? _manager;
    private string _testDataPath = null!;

    [SetUp]
    public void Setup()
    {
        _testDataPath = "/tmp/localization_example";
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