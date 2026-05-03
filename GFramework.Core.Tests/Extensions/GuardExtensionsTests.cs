// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Extensions;
using NUnit.Framework;

namespace GFramework.Core.Tests.Extensions;

/// <summary>
///     测试 GuardExtensions 扩展方法的功能
/// </summary>
[TestFixture]
public class GuardExtensionsTests
{
    const string TestParamName = "testParam";

    /// <summary>
    ///     测试ThrowIfNull方法在值不为null时返回值本身
    /// </summary>
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

    /// <summary>
    ///     测试ThrowIfNull方法在值为null时抛出ArgumentNullException
    /// </summary>
    [Test]
    public void ThrowIfNull_Should_Throw_ArgumentNullException_When_Value_Is_Null()
    {
        // Arrange
        string? value = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => value.ThrowIfNull());
    }

    /// <summary>
    ///     测试ThrowIfNull方法在抛出异常时包含参数名称
    /// </summary>
    [Test]
    public void ThrowIfNull_Should_Include_ParamName_In_Exception()
    {
        // Arrange
        string? value = null;

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => value.ThrowIfNull(TestParamName));
        Assert.That(ex?.ParamName, Is.EqualTo(TestParamName));
    }

    /// <summary>
    ///     测试ThrowIfNullOrEmpty方法在值不为空时返回值本身
    /// </summary>
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

    /// <summary>
    ///     测试ThrowIfNullOrEmpty方法在值为null时抛出ArgumentNullException
    /// </summary>
    [Test]
    public void ThrowIfNullOrEmpty_Should_Throw_ArgumentNullException_When_Value_Is_Null()
    {
        // Arrange
        string? value = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => value.ThrowIfNullOrEmpty());
    }

    /// <summary>
    ///     测试ThrowIfNullOrEmpty方法在值为空时抛出ArgumentException
    /// </summary>
    [Test]
    public void ThrowIfNullOrEmpty_Should_Throw_ArgumentException_When_Value_Is_Empty()
    {
        // Arrange
        var value = string.Empty;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => value.ThrowIfNullOrEmpty());
    }

    /// <summary>
    ///     测试ThrowIfNullOrEmpty方法在抛出异常时包含参数名称
    /// </summary>
    [Test]
    public void ThrowIfNullOrEmpty_Should_Include_ParamName_In_Exception()
    {
        // Arrange
        var value = string.Empty;

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => value.ThrowIfNullOrEmpty(TestParamName));
        Assert.That(ex?.ParamName, Is.EqualTo(TestParamName));
    }

    /// <summary>
    ///     测试ThrowIfEmpty方法在集合有元素时返回集合本身
    /// </summary>
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

    /// <summary>
    ///     测试ThrowIfEmpty方法在集合为null时抛出ArgumentNullException
    /// </summary>
    [Test]
    public void ThrowIfEmpty_Should_Throw_ArgumentNullException_When_Collection_Is_Null()
    {
        // Arrange
        IEnumerable<int>? collection = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => collection.ThrowIfEmpty());
    }

    /// <summary>
    ///     测试ThrowIfEmpty方法在集合为空时抛出ArgumentException
    /// </summary>
    [Test]
    public void ThrowIfEmpty_Should_Throw_ArgumentException_When_Collection_Is_Empty()
    {
        // Arrange
        var collection = Array.Empty<int>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => collection.ThrowIfEmpty());
    }

    /// <summary>
    ///     测试ThrowIfEmpty方法在抛出异常时包含参数名称
    /// </summary>
    [Test]
    public void ThrowIfEmpty_Should_Include_ParamName_In_Exception()
    {
        // Arrange
        var collection = Array.Empty<int>();

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => collection.ThrowIfEmpty(TestParamName));
        Assert.That(ex?.ParamName, Is.EqualTo(TestParamName));
    }
}