// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Extensions;
using NUnit.Framework;

namespace GFramework.Core.Tests.Extensions;

/// <summary>
///     测试 SpanExtensions 扩展方法的功能
/// </summary>
[TestFixture]
public class SpanExtensionsTests
{
    [Test]
    public void TryParseValue_Should_Parse_Valid_Integer()
    {
        // Arrange
        ReadOnlySpan<char> span = "123";

        // Act
        var success = span.TryParseValue<int>(out var result);

        // Assert
        Assert.That(success, Is.True);
        Assert.That(result, Is.EqualTo(123));
    }

    [Test]
    public void TryParseValue_Should_Fail_For_Invalid_Integer()
    {
        // Arrange
        ReadOnlySpan<char> span = "abc";

        // Act
        var success = span.TryParseValue<int>(out var result);

        // Assert
        Assert.That(success, Is.False);
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void TryParseValue_Should_Parse_Valid_Double()
    {
        // Arrange
        ReadOnlySpan<char> span = "123.45";

        // Act
        var success = span.TryParseValue<double>(out var result);

        // Assert
        Assert.That(success, Is.True);
        Assert.That(result, Is.EqualTo(123.45).Within(0.001));
    }

    [Test]
    public void TryParseValue_Should_Parse_Valid_Boolean()
    {
        // Arrange
        ReadOnlySpan<char> span = "true";

        // Act
        var success = span.TryParseValue<bool>(out var result);

        // Assert
        Assert.That(success, Is.True);
        Assert.That(result, Is.True);
    }

    [Test]
    public void TryParseValue_Should_Parse_Valid_Guid()
    {
        // Arrange
        var guid = Guid.NewGuid();
        ReadOnlySpan<char> span = guid.ToString();

        // Act
        var success = span.TryParseValue<Guid>(out var result);

        // Assert
        Assert.That(success, Is.True);
        Assert.That(result, Is.EqualTo(guid));
    }

    [Test]
    public void CountOccurrences_Should_Return_Correct_Count()
    {
        // Arrange
        ReadOnlySpan<int> span = stackalloc int[] { 1, 2, 3, 2, 1 };

        // Act
        var count = span.CountOccurrences(2);

        // Assert
        Assert.That(count, Is.EqualTo(2));
    }

    [Test]
    public void CountOccurrences_Should_Return_Zero_When_Value_Not_Found()
    {
        // Arrange
        ReadOnlySpan<int> span = stackalloc int[] { 1, 2, 3 };

        // Act
        var count = span.CountOccurrences(5);

        // Assert
        Assert.That(count, Is.EqualTo(0));
    }

    [Test]
    public void CountOccurrences_Should_Return_Zero_For_Empty_Span()
    {
        // Arrange
        ReadOnlySpan<int> span = ReadOnlySpan<int>.Empty;

        // Act
        var count = span.CountOccurrences(1);

        // Assert
        Assert.That(count, Is.EqualTo(0));
    }

    [Test]
    public void CountOccurrences_With_Chars_Should_Work()
    {
        // Arrange
        ReadOnlySpan<char> span = "hello";

        // Act
        var count = span.CountOccurrences('l');

        // Assert
        Assert.That(count, Is.EqualTo(2));
    }

    [Test]
    public void CountOccurrences_Should_Work_With_Custom_Types()
    {
        // Arrange
        var item1 = new TestItem(1);
        var item2 = new TestItem(2);
        var item3 = new TestItem(2);
        ReadOnlySpan<TestItem> span = new[] { item1, item2, item3 };

        // Act
        var count = span.CountOccurrences(new TestItem(2));

        // Assert
        Assert.That(count, Is.EqualTo(2));
    }

    private record TestItem(int Value);
}
