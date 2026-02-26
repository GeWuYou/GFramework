using GFramework.Core.extensions;
using NUnit.Framework;

namespace GFramework.Core.Tests.extensions;

/// <summary>
///     测试 NumericExtensions 扩展方法的功能
/// </summary>
[TestFixture]
public class NumericExtensionsTests
{
    /// <summary>
    ///     测试Between方法在值在范围内时返回true
    /// </summary>
    [Test]
    public void Between_Should_Return_True_When_Value_Is_Within_Range()
    {
        // Arrange
        var value = 50;

        // Act
        var result = value.Between(0, 100);

        // Assert
        Assert.That(result, Is.True);
    }

    /// <summary>
    ///     测试Between方法在值等于最小值时返回true
    /// </summary>
    [Test]
    public void Between_Should_Return_True_When_Value_Equals_Min()
    {
        // Arrange
        var value = 0;

        // Act
        var result = value.Between(0, 100);

        // Assert
        Assert.That(result, Is.True);
    }

    /// <summary>
    ///     测试Between方法在值等于最大值时返回true
    /// </summary>
    [Test]
    public void Between_Should_Return_True_When_Value_Equals_Max()
    {
        // Arrange
        var value = 100;

        // Act
        var result = value.Between(0, 100);

        // Assert
        Assert.That(result, Is.True);
    }

    /// <summary>
    ///     测试Between方法在值小于最小值时返回false
    /// </summary>
    [Test]
    public void Between_Should_Return_False_When_Value_Is_Less_Than_Min()
    {
        // Arrange
        var value = -10;

        // Act
        var result = value.Between(0, 100);

        // Assert
        Assert.That(result, Is.False);
    }

    /// <summary>
    ///     测试Between方法在值大于最大值时返回false
    /// </summary>
    [Test]
    public void Between_Should_Return_False_When_Value_Is_Greater_Than_Max()
    {
        // Arrange
        var value = 150;

        // Act
        var result = value.Between(0, 100);

        // Assert
        Assert.That(result, Is.False);
    }

    /// <summary>
    ///     测试Between方法在边界不包含时返回false
    /// </summary>
    [Test]
    public void Between_Should_Return_False_When_Value_Equals_Boundary_And_Not_Inclusive()
    {
        // Arrange
        var value = 0;

        // Act
        var result = value.Between(0, 100, inclusive: false);

        // Assert
        Assert.That(result, Is.False);
    }

    /// <summary>
    ///     测试Between方法在最小值大于最大值时抛出ArgumentException
    /// </summary>
    [Test]
    public void Between_Should_Throw_ArgumentException_When_Min_Is_Greater_Than_Max()
    {
        // Arrange
        var value = 50;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => value.Between(100, 0));
    }

    /// <summary>
    ///     测试Lerp方法在t为0时返回起始值
    /// </summary>
    [Test]
    public void Lerp_Should_Return_From_When_T_Is_Zero()
    {
        // Arrange & Act
        var result = 0f.Lerp(100f, 0f);

        // Assert
        Assert.That(result, Is.EqualTo(0f));
    }

    /// <summary>
    ///     测试Lerp方法在t为1时返回结束值
    /// </summary>
    [Test]
    public void Lerp_Should_Return_To_When_T_Is_One()
    {
        // Arrange & Act
        var result = 0f.Lerp(100f, 1f);

        // Assert
        Assert.That(result, Is.EqualTo(100f));
    }

    /// <summary>
    ///     测试Lerp方法在t为0.5时返回中间值
    /// </summary>
    [Test]
    public void Lerp_Should_Return_Midpoint_When_T_Is_Half()
    {
        // Arrange & Act
        var result = 0f.Lerp(100f, 0.5f);

        // Assert
        Assert.That(result, Is.EqualTo(50f));
    }

    /// <summary>
    ///     测试InverseLerp方法在值等于起始值时返回0
    /// </summary>
    [Test]
    public void InverseLerp_Should_Return_Zero_When_Value_Equals_From()
    {
        // Arrange & Act
        var result = 0f.InverseLerp(0f, 100f);

        // Assert
        Assert.That(result, Is.EqualTo(0f));
    }

    /// <summary>
    ///     测试InverseLerp方法在值等于结束值时返回1
    /// </summary>
    [Test]
    public void InverseLerp_Should_Return_One_When_Value_Equals_To()
    {
        // Arrange & Act
        var result = 100f.InverseLerp(0f, 100f);

        // Assert
        Assert.That(result, Is.EqualTo(1f));
    }

    /// <summary>
    ///     测试InverseLerp方法在值为中间值时返回0.5
    /// </summary>
    [Test]
    public void InverseLerp_Should_Return_Half_When_Value_Is_Midpoint()
    {
        // Arrange & Act
        var result = 50f.InverseLerp(0f, 100f);

        // Assert
        Assert.That(result, Is.EqualTo(0.5f));
    }

    /// <summary>
    ///     测试InverseLerp方法在起始值等于结束值时抛出DivideByZeroException
    /// </summary>
    [Test]
    public void InverseLerp_Should_Throw_DivideByZeroException_When_From_Equals_To()
    {
        // Arrange & Act & Assert
        Assert.Throws<DivideByZeroException>(() => 50f.InverseLerp(100f, 100f));
    }
}