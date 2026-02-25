using GFramework.Core.extensions;
using NUnit.Framework;

namespace GFramework.Core.Tests.extensions;

/// <summary>
///     测试 StringExtensions 扩展方法的功能
/// </summary>
[TestFixture]
public class StringExtensionsTests
{
    [Test]
    public void IsNullOrEmpty_Should_Return_True_When_String_Is_Null()
    {
        // Arrange
        string? text = null;

        // Act
        var result = text.IsNullOrEmpty();

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsNullOrEmpty_Should_Return_True_When_String_Is_Empty()
    {
        // Arrange
        var text = string.Empty;

        // Act
        var result = text.IsNullOrEmpty();

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsNullOrEmpty_Should_Return_False_When_String_Has_Content()
    {
        // Arrange
        var text = "Hello";

        // Act
        var result = text.IsNullOrEmpty();

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsNullOrWhiteSpace_Should_Return_True_When_String_Is_Null()
    {
        // Arrange
        string? text = null;

        // Act
        var result = text.IsNullOrWhiteSpace();

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsNullOrWhiteSpace_Should_Return_True_When_String_Is_WhiteSpace()
    {
        // Arrange
        var text = "   ";

        // Act
        var result = text.IsNullOrWhiteSpace();

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsNullOrWhiteSpace_Should_Return_False_When_String_Has_Content()
    {
        // Arrange
        var text = "Hello";

        // Act
        var result = text.IsNullOrWhiteSpace();

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void NullIfEmpty_Should_Return_Null_When_String_Is_Empty()
    {
        // Arrange
        var text = string.Empty;

        // Act
        var result = text.NullIfEmpty();

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void NullIfEmpty_Should_Return_Null_When_String_Is_Null()
    {
        // Arrange
        string? text = null;

        // Act
        var result = text.NullIfEmpty();

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void NullIfEmpty_Should_Return_String_When_String_Has_Content()
    {
        // Arrange
        var text = "Hello";

        // Act
        var result = text.NullIfEmpty();

        // Assert
        Assert.That(result, Is.EqualTo("Hello"));
    }

    [Test]
    public void Truncate_Should_Return_Original_String_When_Length_Is_Less_Than_MaxLength()
    {
        // Arrange
        var text = "Hello";

        // Act
        var result = text.Truncate(10);

        // Assert
        Assert.That(result, Is.EqualTo("Hello"));
    }

    [Test]
    public void Truncate_Should_Truncate_String_And_Add_Suffix()
    {
        // Arrange
        var text = "Hello World";

        // Act
        var result = text.Truncate(8);

        // Assert
        Assert.That(result, Is.EqualTo("Hello..."));
    }

    [Test]
    public void Truncate_Should_Use_Custom_Suffix()
    {
        // Arrange
        var text = "Hello World";

        // Act
        var result = text.Truncate(8, "~");

        // Assert
        Assert.That(result, Is.EqualTo("Hello W~"));
    }

    [Test]
    public void Truncate_Should_Throw_ArgumentNullException_When_String_Is_Null()
    {
        // Arrange
        string? text = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => text!.Truncate(10));
    }

    [Test]
    public void Truncate_Should_Throw_ArgumentOutOfRangeException_When_MaxLength_Is_Less_Than_Suffix_Length()
    {
        // Arrange
        var text = "Hello";

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => text.Truncate(2, "..."));
    }

    [Test]
    public void Join_Should_Join_Strings_With_Separator()
    {
        // Arrange
        var words = new[] { "Hello", "World" };

        // Act
        var result = words.Join(", ");

        // Assert
        Assert.That(result, Is.EqualTo("Hello, World"));
    }

    [Test]
    public void Join_Should_Return_Empty_String_When_Collection_Is_Empty()
    {
        // Arrange
        var words = Array.Empty<string>();

        // Act
        var result = words.Join(", ");

        // Assert
        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public void Join_Should_Throw_ArgumentNullException_When_Collection_Is_Null()
    {
        // Arrange
        IEnumerable<string>? words = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => words!.Join(", "));
    }

    [Test]
    public void Join_Should_Throw_ArgumentNullException_When_Separator_Is_Null()
    {
        // Arrange
        var words = new[] { "Hello", "World" };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => words.Join(null!));
    }
}
