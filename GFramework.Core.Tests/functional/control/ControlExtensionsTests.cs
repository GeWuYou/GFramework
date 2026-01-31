using GFramework.Core.functional.control;
using NUnit.Framework;

namespace GFramework.Core.Tests.functional.control;

/// <summary>
/// ControlExtensions扩展方法测试类，用于验证控制流函数式编程扩展方法的正确性
/// </summary>
[TestFixture]
public class ControlExtensionsTests
{
    /// <summary>
    /// 测试TakeIf方法 - 验证条件为真时返回原值
    /// </summary>
    [Test]
    public void TakeIf_Should_Return_Value_When_Condition_Is_True()
    {
        // Arrange
        string str = "Hello";

        // Act
        var result = str.TakeIf(s => s.Length > 3);

        // Assert
        Assert.That(result, Is.EqualTo("Hello"));
    }

    /// <summary>
    /// 测试TakeIf方法 - 验证条件为假时返回null
    /// </summary>
    [Test]
    public void TakeIf_Should_Return_Null_When_Condition_Is_False()
    {
        // Arrange
        string str = "Hi";

        // Act
        var result = str.TakeIf(s => s.Length > 3);

        // Assert
        Assert.That(result, Is.Null);
    }

    /// <summary>
    /// 测试TakeUnless方法 - 验证条件为假时返回原值
    /// </summary>
    [Test]
    public void TakeUnless_Should_Return_Value_When_Condition_Is_False()
    {
        // Arrange
        string str = "Hi";

        // Act
        var result = str.TakeUnless(s => s.Length > 3);

        // Assert
        Assert.That(result, Is.EqualTo("Hi"));
    }

    /// <summary>
    /// 测试TakeUnless方法 - 验证条件为真时返回null
    /// </summary>
    [Test]
    public void TakeUnless_Should_Return_Null_When_Condition_Is_True()
    {
        // Arrange
        string str = "Hello";

        // Act
        var result = str.TakeUnless(s => s.Length > 3);

        // Assert
        Assert.That(result, Is.Null);
    }
}