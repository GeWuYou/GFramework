// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using GFramework.Core.Functional.Pipe;
using NUnit.Framework;

namespace GFramework.Core.Tests.Functional.Pipe;

/// <summary>
///     PipeExtensions扩展方法测试类，用于验证管道和函数组合扩展方法的正确性
///     包括管道操作、函数组合等核心功能的测试
/// </summary>
[TestFixture]
public class PipeExtensionsTests
{
    /// <summary>
    ///     测试Also方法 - 验证执行操作后返回原值功能
    /// </summary>
    [Test]
    public void Also_Should_Execute_Action_And_Return_Original_Value()
    {
        // Arrange
        var value = 42;
        var capturedValue = 0;

        // Act
        var result = value.Also(x => capturedValue = x);

        // Assert
        Assert.That(result, Is.EqualTo(42));
        Assert.That(capturedValue, Is.EqualTo(42));
    }

    /// <summary>
    ///     测试Tap方法执行操作并返回原值
    /// </summary>
    [Test]
    public void Tap_Should_Execute_Action_And_Return_Original_Value()
    {
        // Arrange
        var value = 42;
        var capturedValue = 0;

        // Act
        var result = value.Tap(x => capturedValue = x);

        // Assert
        Assert.That(result, Is.EqualTo(42));
        Assert.That(capturedValue, Is.EqualTo(42));
    }

    /// <summary>
    ///     测试Tap方法在操作为null时抛出ArgumentNullException
    /// </summary>
    [Test]
    public void Tap_WithNullAction_Should_Throw_ArgumentNullException()
    {
        // Arrange
        var value = 42;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => value.Tap(null!));
    }

    /// <summary>
    ///     测试Tap方法支持链式调用
    /// </summary>
    [Test]
    public void Tap_Should_Allow_Chaining()
    {
        // Arrange
        var value = 10;
        var log = new List<string>();

        // Act
        var result = value
            .Tap(x => log.Add($"Step 1: {x}"))
            .Tap(x => log.Add($"Step 2: {x}"));

        // Assert
        Assert.That(result, Is.EqualTo(10));
        Assert.That(log, Has.Count.EqualTo(2));
        Assert.That(log[0], Is.EqualTo("Step 1: 10"));
        Assert.That(log[1], Is.EqualTo("Step 2: 10"));
    }

    /// <summary>
    ///     测试Pipe方法转换值
    /// </summary>
    [Test]
    public void Pipe_Should_Transform_Value()
    {
        // Arrange
        var value = 42;

        // Act
        var result = value.Pipe(x => x * 2);

        // Assert
        Assert.That(result, Is.EqualTo(84));
    }

    /// <summary>
    ///     测试Pipe方法在函数为null时抛出ArgumentNullException
    /// </summary>
    [Test]
    public void Pipe_WithNullFunction_Should_Throw_ArgumentNullException()
    {
        // Arrange
        var value = 42;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => value.Pipe<int, int>(null!));
    }

    /// <summary>
    ///     测试Pipe方法支持链式调用
    /// </summary>
    [Test]
    public void Pipe_Should_Allow_Chaining()
    {
        // Arrange
        var value = 5;

        // Act
        var result = value
            .Pipe(x => x * 2)
            .Pipe(x => x + 10)
            .Pipe(x => x.ToString(CultureInfo.InvariantCulture));

        // Assert
        Assert.That(result, Is.EqualTo("20"));
    }

    /// <summary>
    ///     测试Let方法转换值
    /// </summary>
    [Test]
    public void Let_Should_Transform_Value()
    {
        // Arrange
        var value = 42;

        // Act
        var result = value.Let(x => x.ToString(CultureInfo.InvariantCulture));

        // Assert
        Assert.That(result, Is.EqualTo("42"));
    }

    /// <summary>
    ///     测试Let方法在转换函数为null时抛出ArgumentNullException
    /// </summary>
    [Test]
    public void Let_WithNullTransform_Should_Throw_ArgumentNullException()
    {
        // Arrange
        var value = 42;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => value.Let<int, string>(null!));
    }

    /// <summary>
    ///     测试Let方法支持复杂转换
    /// </summary>
    [Test]
    public void Let_Should_Allow_Complex_Transformations()
    {
        // Arrange
        var value = "hello";

        // Act
        var result = value.Let(s => new
        {
            Original = s,
            Upper = s.ToUpperInvariant(),
            Length = s.Length
        });

        // Assert
        Assert.That(result.Original, Is.EqualTo("hello"));
        Assert.That(result.Upper, Is.EqualTo("HELLO"));
        Assert.That(result.Length, Is.EqualTo(5));
    }

    /// <summary>
    ///     测试PipeIf方法在谓词为true时应用IfTrue函数
    /// </summary>
    [Test]
    public void PipeIf_WithTruePredicate_Should_Apply_IfTrue_Function()
    {
        // Arrange
        var value = 42;

        // Act
        var result = value.PipeIf(
            x => x > 0,
            x => $"Positive: {x}",
            x => $"Non-positive: {x}"
        );

        // Assert
        Assert.That(result, Is.EqualTo("Positive: 42"));
    }

    /// <summary>
    ///     测试PipeIf方法在谓词为false时应用IfFalse函数
    /// </summary>
    [Test]
    public void PipeIf_WithFalsePredicate_Should_Apply_IfFalse_Function()
    {
        // Arrange
        var value = -5;

        // Act
        var result = value.PipeIf(
            x => x > 0,
            x => $"Positive: {x}",
            x => $"Non-positive: {x}"
        );

        // Assert
        Assert.That(result, Is.EqualTo("Non-positive: -5"));
    }

    /// <summary>
    ///     测试PipeIf方法在谓词为null时抛出ArgumentNullException
    /// </summary>
    [Test]
    public void PipeIf_WithNullPredicate_Should_Throw_ArgumentNullException()
    {
        // Arrange
        var value = 42;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            value.PipeIf<int, string>(null!, x => "", x => ""));
    }

    /// <summary>
    ///     测试PipeIf方法在IfTrue函数为null时抛出ArgumentNullException
    /// </summary>
    [Test]
    public void PipeIf_WithNullIfTrue_Should_Throw_ArgumentNullException()
    {
        // Arrange
        var value = 42;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            value.PipeIf(x => true, null!, x => ""));
    }

    /// <summary>
    ///     测试PipeIf方法在IfFalse函数为null时抛出ArgumentNullException
    /// </summary>
    [Test]
    public void PipeIf_WithNullIfFalse_Should_Throw_ArgumentNullException()
    {
        // Arrange
        var value = 42;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            value.PipeIf(x => true, x => "", null!));
    }

    /// <summary>
    ///     测试PipeIf方法支持链式调用
    /// </summary>
    [Test]
    public void PipeIf_Should_Allow_Chaining()
    {
        // Arrange
        var value = 10;

        // Act
        var result = value
            .PipeIf(x => x > 5, x => x * 2, x => x + 10)
            .PipeIf(x => x > 15, x => $"Large: {x}", x => $"Small: {x}");

        // Assert
        Assert.That(result, Is.EqualTo("Large: 20"));
    }
}
