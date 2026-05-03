// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Functional.Control;
using NUnit.Framework;

namespace GFramework.Core.Tests.Functional.Control;

/// <summary>
///     ControlExtensions扩展方法测试类，用于验证控制流函数式编程扩展方法的正确性
/// </summary>
[TestFixture]
public class ControlExtensionsTests
{
    /// <summary>
    ///     测试TakeIf方法 - 验证条件为真时返回原值
    /// </summary>
    [Test]
    public void TakeIf_Should_Return_Value_When_Condition_Is_True()
    {
        // Arrange
        var str = "Hello";

        // Act
        var result = str.TakeIf(s => s.Length > 3);

        // Assert
        Assert.That(result, Is.EqualTo("Hello"));
    }

    /// <summary>
    ///     测试TakeIf方法 - 验证条件为假时返回null
    /// </summary>
    [Test]
    public void TakeIf_Should_Return_Null_When_Condition_Is_False()
    {
        // Arrange
        var str = "Hi";

        // Act
        var result = str.TakeIf(s => s.Length > 3);

        // Assert
        Assert.That(result, Is.Null);
    }

    /// <summary>
    ///     测试TakeUnless方法 - 验证条件为假时返回原值
    /// </summary>
    [Test]
    public void TakeUnless_Should_Return_Value_When_Condition_Is_False()
    {
        // Arrange
        var str = "Hi";

        // Act
        var result = str.TakeUnless(s => s.Length > 3);

        // Assert
        Assert.That(result, Is.EqualTo("Hi"));
    }

    /// <summary>
    ///     测试TakeUnless方法 - 验证条件为真时返回null
    /// </summary>
    [Test]
    public void TakeUnless_Should_Return_Null_When_Condition_Is_True()
    {
        // Arrange
        var str = "Hello";

        // Act
        var result = str.TakeUnless(s => s.Length > 3);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void TakeIfValue_Should_Return_Value_When_Condition_Is_True()
    {
        // Arrange
        var value = 42;

        // Act
        var result = value.TakeIfValue(x => x > 0);

        // Assert
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void TakeIfValue_Should_Return_Null_When_Condition_Is_False()
    {
        // Arrange
        var value = -5;

        // Act
        var result = value.TakeIfValue(x => x > 0);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void TakeIfValue_WithNullPredicate_Should_Throw_ArgumentNullException()
    {
        // Arrange
        var value = 42;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => value.TakeIfValue(null!));
    }

    [Test]
    public void TakeUnlessValue_Should_Return_Value_When_Condition_Is_False()
    {
        // Arrange
        var value = 42;

        // Act
        var result = value.TakeUnlessValue(x => x < 0);

        // Assert
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void TakeUnlessValue_Should_Return_Null_When_Condition_Is_True()
    {
        // Arrange
        var value = -5;

        // Act
        var result = value.TakeUnlessValue(x => x < 0);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void TakeUnlessValue_WithNullPredicate_Should_Throw_ArgumentNullE()
    {
        // Arrange
        var value = 42;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => value.TakeUnlessValue(null!));
    }

    [Test]
    public void When_Should_Execute_Action_When_Condition_Is_True()
    {
        // Arrange
        var value = 42;
        var executed = false;

        // Act
        var result = value.When(x => x > 0, x => executed = true);

        // Assert
        Assert.That(result, Is.EqualTo(42));
        Assert.That(executed, Is.True);
    }

    [Test]
    public void When_Should_Not_Executehen_Condition_Is_False()
    {
        // Arrange
        var value = -5;
        var executed = false;

        // Act
        var result = value.When(x => x > 0, x => executed = true);

        // Assert
        Assert.That(result, Is.EqualTo(-5));
        Assert.That(executed, Is.False);
    }

    [Test]
    public void When_WithNullPredicate_Should_Throw_ArgumentNullException()
    {
        // Arrange
        var value = 42;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            value.When(null!, x => { }));
    }

    [Test]
    public void When_WithNullAction_Should_Throw_ArgumentNullException()
    {
        // Arrange
        var value = 42;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            value.When(x => true, null!));
    }

    [Test]
    public void When_Should_Allow_Chaining()
    {
        // Arrange
        var value = 10;
        var log = new List<string>();

        // Act
        var result = value
            .When(x => x > 5, x => log.Add("Greater than 5"))
            .When(x => x % 2 == 0, x => log.Add("Even number"));

        // Assert
        Assert.That(result, Is.EqualTo(10));
        Assert.That(log, Has.Count.EqualTo(2));
    }

    [Test]
    public void RepeatUntil_Should_Repeat_Until_Condition_Is_Met()
    {
        // Arrange
        var value = 1;

        // Act
        var result = value.RepeatUntil(
            x => x * 2,
            x => x >= 100,
            maxIterations: 10
        );

        // Assert
        Assert.That(result, Is.EqualTo(128));
    }

    [Test]
    public void RepeatUntil_Should_Return_Initial_Value_If_Condition_Already_Met()
    {
        // Arrange
        var value = 100;

        // Act
        var result = value.RepeatUntil(
            x => x * 2,
            x => x >= 100,
            maxIterations: 10
        );

        // Assert
        Assert.That(result, Is.EqualTo(100));
    }

    [Test]
    public void RepeatUntil_Should_Throw_When_Max_Iterations_Reached()
    {
        // Arrange
        var value = 1;

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            value.RepeatUntil(
                x => x + 1,
                x => x > 1000,
                maxIterations: 10
            ));
    }

    [Test]
    public void RepeatUntil_WithNullFunc_Should_Throw_ArgumentNullException()
    {
        // Arrange
        var value = 1;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            value.RepeatUntil(null!, x => true));
    }

    [Test]
    public void RepeatUntil_WithNullPredicate_Should_Throw_ArgumentNullException()
    {
        // Arrange
        var value = 1;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            value.RepeatUntil(x => x, null!));
    }

    [Test]
    public void RepeatUntil_WithInvalidMaxIterations_Should_Throw_ArgumentOutOfRangeException()
    {
        // Arrange
        var value = 1;

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            value.RepeatUntil(x => x, x => true, maxIterations: 0));
    }

    [Test]
    public void Retry_Should_Return_Result_On_First_Success()
    {
        // Arrange
        var counter = 0;
        Func<int> func = () => ++counter;

        // Act
        var result = ControlExtensions.Retry(func, maxRetries: 3);

        // Assert
        Assert.That(result, Is.EqualTo(1));
        Assert.That(counter, Is.EqualTo(1));
    }

    [Test]
    public void Retry_Should_Retry_On_Failure()
    {
        // Arrange
        var counter = 0;
        Func<int> func = () =>
        {
            counter++;
            if (counter < 3)
                throw new InvalidOperationException("Not ready");
            return counter;
        };

        // Act
        var result = ControlExtensions.Retry(func, maxRetries: 3);

        // Assert
        Assert.That(result, Is.EqualTo(3));
        Assert.That(counter, Is.EqualTo(3));
    }

    [Test]
    public void Retry_Should_Throw_AggregateException_When_All_Retries_Fail()
    {
        // Arrange
        var counter = 0;
        Func<int> func = () =>
        {
            counter++;
            throw new InvalidOperationException($"Attempt {counter}");
        };

        // Act & Assert
        var ex = Assert.Throws<AggregateException>(() =>
            ControlExtensions.Retry(func, maxRetries: 2));

        Assert.That(counter, Is.EqualTo(3)); // 1 initial + 2 retries
        Assert.That(ex!.InnerExceptions, Has.Count.EqualTo(3));
    }

    [Test]
    public void Retry_WithNullFunc_Should_Throw_ArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            ControlExtensions.Retry<int>(null!, maxRetries: 3));
    }

    [Test]
    public void Retry_WithNegativeMaxRetries_Should_Throw_ArgumentOutOfRangeException()
    {
        // Arrange
        Func<int> func = () => 42;

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ControlExtensions.Retry(func, maxRetries: -1));
    }

    [Test]
    public void Retry_WithNegativeDelay_Should_Throw_ArgumentOutOfRangeException()
    {
        // Arrange
        Func<int> func = () => 42;

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ControlExtensions.Retry(func, maxRetries: 3, delayMilliseconds: -1));
    }

    [Test]
    public void Retry_Should_Delay_Between_Retries()
    {
        // Arrange
        var counter = 0;
        var startTime = DateTime.UtcNow;
        Func<int> func = () =>
        {
            counter++;
            if (counter < 3)
                throw new InvalidOperationException("Not ready");
            return counter;
        };

        // Act
        var result = ControlExtensions.Retry(func, maxRetries: 3, delayMilliseconds: 50);
        var elapsed = DateTime.UtcNow - startTime;

        // Assert
        Assert.That(result, Is.EqualTo(3));
        Assert.That(elapsed.TotalMilliseconds, Is.GreaterThanOrEqualTo(100)); // 2 delays of 50ms
    }
}