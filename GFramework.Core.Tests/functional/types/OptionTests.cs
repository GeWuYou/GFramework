using GFramework.Core.functional.types;
using NUnit.Framework;

namespace GFramework.Core.Tests.functional.types;

/// <summary>
/// Option类型测试类，用于验证Option类型的基本功能
/// 包括创建Some和None实例、值访问等功能的测试
/// </summary>
[TestFixture]
public class OptionTests
{
    /// <summary>
    /// 测试创建Some实例 - 验证非null值能正确创建Some
    /// </summary>
    [Test]
    public void Option_Some_Should_Create_WithValue()
    {
        // Arrange
        var value = "Hello";

        // Act
        var option = Option<string>.Some(value);

        // Assert
        Assert.That(option.IsSome, Is.True);
        Assert.That(option.IsNone, Is.False);
        Assert.That(option.Value, Is.EqualTo(value));
    }

    /// <summary>
    /// 测试创建Some实例 - 验证null值时抛出异常
    /// </summary>
    [Test]
    public void Option_Some_Should_Throw_When_Value_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Option<string>.Some(null!));
    }

    /// <summary>
    /// 测试创建None实例 - 验证能正确创建None
    /// </summary>
    [Test]
    public void Option_None_Should_Create_Empty_Instance()
    {
        // Act
        var option = Option<string>.None();

        // Assert
        Assert.That(option.IsNone, Is.True);
        Assert.That(option.IsSome, Is.False);
    }

    /// <summary>
    /// 测试访问None的值 - 验证抛出异常
    /// </summary>
    [Test]
    public void Option_None_Value_Access_Should_Throw_Exception()
    {
        // Arrange
        var option = Option<string>.None();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
        {
            var _ = option.Value;
        });
    }
}