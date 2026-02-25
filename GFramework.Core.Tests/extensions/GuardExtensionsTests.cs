using GFramework.Core.extensions;
using NUnit.Framework;

namespace GFramework.Core.Tests.extensions;

/// <summary>
///     测试 GuardExtensions 扩展方法的功能
/// </summary>
[TestFixture]
public class GuardExtensionsTests
{
    [Test]
    public void ThrowIfNull_Should_Return_Value_When_Value_Is_Not_Null()
    {
        // Arrange
        var value = "test";

        // Act
        var result = value.ThrowIfNull();

        // Assert
        Assert.That(result, Is.EqualTo("test"));
    }

    [Test]
    public void ThrowIfNull_Should_Throw_ArgumentNullException_When_Value_Is_Null()
    {
        // Arrange
        string? value = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => value.ThrowIfNull());
    }

    [Test]
    public void ThrowIfNull_Should_Include_ParamName_In_Exception()
    {
        // Arrange
        string? value = null;

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => value.ThrowIfNull("testParam"));
        Assert.That(ex.ParamName, Is.EqualTo("testParam"));
    }

    [Test]
    public void ThrowIfNullOrEmpty_Should_Return_Value_When_Value_Is_Not_Empty()
    {
        // Arrange
        var value = "test";

        // Act
        var result = value.ThrowIfNullOrEmpty();

        // Assert
        Assert.That(result, Is.EqualTo("test"));
    }

    [Test]
    public void ThrowIfNullOrEmpty_Should_Throw_ArgumentNullException_When_Value_Is_Null()
    {
        // Arrange
        string? value = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => value.ThrowIfNullOrEmpty());
    }

    [Test]
    public void ThrowIfNullOrEmpty_Should_Throw_ArgumentException_When_Value_Is_Empty()
    {
        // Arrange
        var value = string.Empty;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => value.ThrowIfNullOrEmpty());
    }

    [Test]
    public void ThrowIfNullOrEmpty_Should_Include_ParamName_In_Exception()
    {
        // Arrange
        var value = string.Empty;

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => value.ThrowIfNullOrEmpty("testParam"));
        Assert.That(ex.ParamName, Is.EqualTo("testParam"));
    }

    [Test]
    public void ThrowIfEmpty_Should_Return_Collection_When_Collection_Has_Elements()
    {
        // Arrange
        var collection = new[] { 1, 2, 3 };

        // Act
        var result = collection.ThrowIfEmpty();

        // Assert
        Assert.That(result, Is.EqualTo(collection));
    }

    [Test]
    public void ThrowIfEmpty_Should_Throw_ArgumentNullException_When_Collection_Is_Null()
    {
        // Arrange
        IEnumerable<int>? collection = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => collection.ThrowIfEmpty());
    }

    [Test]
    public void ThrowIfEmpty_Should_Throw_ArgumentException_When_Collection_Is_Empty()
    {
        // Arrange
        var collection = Array.Empty<int>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => collection.ThrowIfEmpty());
    }

    [Test]
    public void ThrowIfEmpty_Should_Include_ParamName_In_Exception()
    {
        // Arrange
        var collection = Array.Empty<int>();

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => collection.ThrowIfEmpty("testParam"));
        Assert.That(ex.ParamName, Is.EqualTo("testParam"));
    }
}
