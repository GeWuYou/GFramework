// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Localization;

namespace GFramework.Core.Tests.Localization;

[TestFixture]
public class LocalizationTableTests
{
    [Test]
    public void GetRawText_ShouldReturnCorrectText()
    {
        // Arrange
        var data = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["test.key"] = "Test Value"
        };
        var table = new LocalizationTable("test", "eng", data);

        // Act
        var result = table.GetRawText("test.key");

        // Assert
        Assert.That(result, Is.EqualTo("Test Value"));
    }

    [Test]
    public void GetRawText_WithFallback_ShouldReturnFallbackValue()
    {
        // Arrange
        var fallbackData = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["test.key"] = "Fallback Value"
        };
        var fallbackTable = new LocalizationTable("test", "eng", fallbackData);

        var data = new Dictionary<string, string>(StringComparer.Ordinal);
        var table = new LocalizationTable("test", "zhs", data, fallbackTable);

        // Act
        var result = table.GetRawText("test.key");

        // Assert
        Assert.That(result, Is.EqualTo("Fallback Value"));
    }

    [Test]
    public void ContainsKey_ShouldReturnTrue_WhenKeyExists()
    {
        // Arrange
        var data = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["test.key"] = "Test Value"
        };
        var table = new LocalizationTable("test", "eng", data);

        // Act
        var result = table.ContainsKey("test.key");

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void Merge_ShouldOverrideExistingValues()
    {
        // Arrange
        var data = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["test.key"] = "Original Value"
        };
        var table = new LocalizationTable("test", "eng", data);

        var overrides = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["test.key"] = "Override Value"
        };

        // Act
        table.Merge(overrides);
        var result = table.GetRawText("test.key");

        // Assert
        Assert.That(result, Is.EqualTo("Override Value"));
    }
}
