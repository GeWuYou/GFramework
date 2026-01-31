using GFramework.Core.functional.types;
using NUnit.Framework;

namespace GFramework.Core.Tests.functional.types;

/// <summary>
/// OptionValueExtensions扩展方法测试类，用于验证Option类型值的操作扩展方法
/// 包括获取默认值、备选Option等功能的测试
/// </summary>
[TestFixture]
public class OptionValueExtensionsTests
{
    /// <summary>
    /// 测试GetOrElse方法 - 验证Some值直接返回其值
    /// </summary>
    [Test]
    public void OptionValueExtensions_GetOrElse_Should_Return_Value_For_Some()
    {
        // Arrange
        var option = Option<string>.Some("actual value");

        // Act
        var result = option.GetOrElse("default value");

        // Assert
        Assert.That(result, Is.EqualTo("actual value"));
    }

    /// <summary>
    /// 测试GetOrElse方法 - 验证None值返回默认值
    /// </summary>
    [Test]
    public void OptionValueExtensions_GetOrElse_Should_Return_Default_For_None()
    {
        // Arrange
        var option = Option<string>.None();

        // Act
        var result = option.GetOrElse("default value");

        // Assert
        Assert.That(result, Is.EqualTo("default value"));
    }

    /// <summary>
    /// 测试GetOrElse方法(工厂函数) - 验证Some值直接返回其值(不调用工厂)
    /// </summary>
    [Test]
    public void OptionValueExtensions_GetOrElse_With_Factory_Should_Return_Value_For_Some()
    {
        // Arrange
        var option = Option<string>.Some("actual value");
        var factoryCalled = false;

        // Act
        var result = option.GetOrElse(() =>
        {
            factoryCalled = true;
            return "factory value";
        });

        // Assert
        Assert.That(result, Is.EqualTo("actual value"));
        Assert.That(factoryCalled, Is.False);
    }

    /// <summary>
    /// 测试GetOrElse方法(工厂函数) - 验证None值调用工厂函数
    /// </summary>
    [Test]
    public void OptionValueExtensions_GetOrElse_With_Factory_Should_Call_Factory_For_None()
    {
        // Arrange
        var option = Option<string>.None();
        var factoryCalled = false;

        // Act
        var result = option.GetOrElse(() =>
        {
            factoryCalled = true;
            return "factory value";
        });

        // Assert
        Assert.That(result, Is.EqualTo("factory value"));
        Assert.That(factoryCalled, Is.True);
    }

    /// <summary>
    /// 测试OrElse方法 - 验证Some值返回自身
    /// </summary>
    [Test]
    public void OptionValueExtensions_OrElse_Should_Return_Self_For_Some()
    {
        // Arrange
        var option = Option<string>.Some("primary value");
        var fallback = Option<string>.Some("fallback value");

        // Act
        var result = option.OrElse(fallback);

        // Assert
        Assert.That(result.IsSome, Is.True);
        Assert.That(result.Value, Is.EqualTo("primary value"));
    }

    /// <summary>
    /// 测试OrElse方法 - 验证None值返回备选Option
    /// </summary>
    [Test]
    public void OptionValueExtensions_OrElse_Should_Return_Fallback_For_None()
    {
        // Arrange
        var option = Option<string>.None();
        var fallback = Option<string>.Some("fallback value");

        // Act
        var result = option.OrElse(fallback);

        // Assert
        Assert.That(result.IsSome, Is.True);
        Assert.That(result.Value, Is.EqualTo("fallback value"));
    }

    /// <summary>
    /// 测试OrElse方法 - 验证None值返回备选None
    /// </summary>
    [Test]
    public void OptionValueExtensions_OrElse_Should_Return_Fallback_None_For_None()
    {
        // Arrange
        var option = Option<string>.None();
        var fallback = Option<string>.None();

        // Act
        var result = option.OrElse(fallback);

        // Assert
        Assert.That(result.IsNone, Is.True);
    }
}