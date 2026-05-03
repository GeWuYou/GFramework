// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Extensions;
using NUnit.Framework;

namespace GFramework.Core.Tests.Extensions;

/// <summary>
///     测试 StringExtensions 扩展方法的功能
/// </summary>
[TestFixture]
public class StringExtensionsTests
{
    /// <summary>
    ///     测试IsNullOrEmpty方法在字符串为null时返回true
    /// </summary>
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

    /// <summary>
    ///     测试IsNullOrEmpty方法在字符串为空时返回true
    /// </summary>
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

    /// <summary>
    ///     测试IsNullOrEmpty方法在字符串有内容时返回false
    /// </summary>
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

    /// <summary>
    ///     测试IsNullOrWhiteSpace方法在字符串为null时返回true
    /// </summary>
    [Test]
    public void IsNullOrWhiteSpace_Should_Return_True_When_String_Is_Null()
    {
        // Arrange
        string? text = null;

        // Act
        var result = string.IsNullOrWhiteSpace(text);

        // Assert
        Assert.That(result, Is.True);
    }

    /// <summary>
    ///     测试IsNullOrWhiteSpace方法在字符串为空白时返回true
    /// </summary>
    [Test]
    public void IsNullOrWhiteSpace_Should_Return_True_When_String_Is_WhiteSpace()
    {
        // Arrange
        var text = "   ";

        // Act
        var result = string.IsNullOrWhiteSpace(text);

        // Assert
        Assert.That(result, Is.True);
    }

    /// <summary>
    ///     测试IsNullOrWhiteSpace方法在字符串有内容时返回false
    /// </summary>
    [Test]
    public void IsNullOrWhiteSpace_Should_Return_False_When_String_Has_Content()
    {
        // Arrange
        var text = "Hello";

        // Act
        var result = string.IsNullOrWhiteSpace(text);

        // Assert
        Assert.That(result, Is.False);
    }

    /// <summary>
    ///     测试NullIfEmpty方法在字符串为空时返回null
    /// </summary>
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

    /// <summary>
    ///     测试NullIfEmpty方法在字符串为null时返回null
    /// </summary>
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

    /// <summary>
    ///     测试NullIfEmpty方法在字符串有内容时返回原字符串
    /// </summary>
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

    /// <summary>
    ///     测试Truncate方法在字符串长度小于最大长度时返回原字符串
    /// </summary>
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

    /// <summary>
    ///     测试Truncate方法在字符串长度超过最大长度时截断并添加后缀
    /// </summary>
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

    /// <summary>
    ///     测试Truncate方法使用自定义后缀
    /// </summary>
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

    /// <summary>
    ///     测试Truncate方法在字符串为null时抛出ArgumentNullException
    /// </summary>
    [Test]
    public void Truncate_Should_Throw_ArgumentNullException_When_String_Is_Null()
    {
        // Arrange
        string? text = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => text!.Truncate(10));
    }

    /// <summary>
    ///     测试Truncate方法在最大长度小于后缀长度时抛出ArgumentOutOfRangeException
    /// </summary>
    [Test]
    public void Truncate_Should_Throw_ArgumentOutOfRangeException_When_MaxLength_Is_Less_Than_Suffix_Length()
    {
        // Arrange
        var text = "Hello";

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => text.Truncate(2, "..."));
    }

    /// <summary>
    ///     测试Join方法使用分隔符连接字符串数组
    /// </summary>
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

    /// <summary>
    ///     测试Join方法在集合为空时返回空字符串
    /// </summary>
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

    /// <summary>
    ///     测试Join方法在集合为null时抛出ArgumentNullException
    /// </summary>
    [Test]
    public void Join_Should_Throw_ArgumentNullException_When_Collection_Is_Null()
    {
        // Arrange
        IEnumerable<string>? words = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => words!.Join(", "));
    }

    /// <summary>
    ///     测试Join方法在分隔符为null时抛出ArgumentNullException
    /// </summary>
    [Test]
    public void Join_Should_Throw_ArgumentNullException_When_Separator_Is_Null()
    {
        // Arrange
        var words = new[] { "Hello", "World" };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => words.Join(null!));
    }
}