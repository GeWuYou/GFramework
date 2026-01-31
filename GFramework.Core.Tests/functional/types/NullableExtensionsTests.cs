using GFramework.Core.functional.types;
using NUnit.Framework;

namespace GFramework.Core.Tests.functional.types;

/// <summary>
/// NullableExtensions扩展方法测试类，用于验证可空类型转换为Option类型的功能
/// 包括引用类型和值类型的可空转换测试
/// </summary>
[TestFixture]
public class NullableExtensionsTests
{
    /// <summary>
    /// 测试引用类型可空转换 - 验证非null值转换为Some
    /// </summary>
    [Test]
    public void NullableExtensions_ReferenceType_ToOption_Should_Create_Some_For_NonNull()
    {
        // Arrange
        string? value = "Hello";

        // Act
        var option = value.ToOption();

        // Assert
        Assert.That(option.IsSome, Is.True);
        Assert.That(option.Value, Is.EqualTo("Hello"));
    }

    /// <summary>
    /// 测试引用类型可空转换 - 验证null值转换为None
    /// </summary>
    [Test]
    public void NullableExtensions_ReferenceType_ToOption_Should_Create_None_For_Null()
    {
        // Arrange
        string? value = null;

        // Act
        var option = value.ToOption();

        // Assert
        Assert.That(option.IsNone, Is.True);
    }

    /// <summary>
    /// 测试值类型可空转换 - 验证有值的可空值类型转换为Some
    /// </summary>
    [Test]
    public void NullableExtensions_ValueType_ToOption_Should_Create_Some_For_HasValue()
    {
        // Arrange
        int? value = 42;

        // Act
        var option = value.ToOption();

        // Assert
        Assert.That(option.IsSome, Is.True);
        Assert.That(option.Value, Is.EqualTo(42));
    }

    /// <summary>
    /// 测试值类型可空转换 - 验证无值的可空值类型转换为None
    /// </summary>
    [Test]
    public void NullableExtensions_ValueType_ToOption_Should_Create_None_For_NoValue()
    {
        // Arrange
        int? value = null;

        // Act
        var option = value.ToOption();

        // Assert
        Assert.That(option.IsNone, Is.True);
    }
}