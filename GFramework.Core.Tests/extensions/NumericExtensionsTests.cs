using GFramework.Core.extensions;
using NUnit.Framework;

namespace GFramework.Core.Tests.extensions;

/// <summary>
///     测试 NumericExtensions 扩展方法的功能
/// </summary>
[TestFixture]
public class NumericExtensionsTests
{
    [Test]
    public void Clamp_Should_Return_Min_When_Value_Is_Less_Than_Min()
    {
        // Arrange
        var value = -10;

        // Act
        var result = value.Clamp(0, 100);

        // Assert
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void Clamp_Should_Return_Max_When_Value_Is_Greater_Than_Max()
    {
        // Arrange
        var value = 150;

        // Act
        var result = value.Clamp(0, 100);

        // Assert
        Assert.That(result, Is.EqualTo(100));
    }

    [Test]
    public void Clamp_Should_Return_Value_When_Value_Is_Within_Range()
    {
        // Arrange
        var value = 50;

        // Act
        var result = value.Clamp(0, 100);

        // Assert
        Assert.That(result, Is.EqualTo(50));
    }

    [Test]
    public void Clamp_Should_Throw_ArgumentException_When_Min_Is_Greater_Than_Max()
    {
        // Arrange
        var value = 50;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => value.Clamp(100, 0));
    }

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

    [Test]
    public void Between_Should_Throw_ArgumentException_When_Min_Is_Greater_Than_Max()
    {
        // Arrange
        var value = 50;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => value.Between(100, 0));
    }

    [Test]
    public void Lerp_Should_Return_From_When_T_Is_Zero()
    {
        // Arrange & Act
        var result = 0f.Lerp(100f, 0f);

        // Assert
        Assert.That(result, Is.EqualTo(0f));
    }

    [Test]
    public void Lerp_Should_Return_To_When_T_Is_One()
    {
        // Arrange & Act
        var result = 0f.Lerp(100f, 1f);

        // Assert
        Assert.That(result, Is.EqualTo(100f));
    }

    [Test]
    public void Lerp_Should_Return_Midpoint_When_T_Is_Half()
    {
        // Arrange & Act
        var result = 0f.Lerp(100f, 0.5f);

        // Assert
        Assert.That(result, Is.EqualTo(50f));
    }

    [Test]
    public void InverseLerp_Should_Return_Zero_When_Value_Equals_From()
    {
        // Arrange & Act
        var result = 0f.InverseLerp(0f, 100f);

        // Assert
        Assert.That(result, Is.EqualTo(0f));
    }

    [Test]
    public void InverseLerp_Should_Return_One_When_Value_Equals_To()
    {
        // Arrange & Act
        var result = 100f.InverseLerp(0f, 100f);

        // Assert
        Assert.That(result, Is.EqualTo(1f));
    }

    [Test]
    public void InverseLerp_Should_Return_Half_When_Value_Is_Midpoint()
    {
        // Arrange & Act
        var result = 50f.InverseLerp(0f, 100f);

        // Assert
        Assert.That(result, Is.EqualTo(0.5f));
    }

    [Test]
    public void InverseLerp_Should_Throw_DivideByZeroException_When_From_Equals_To()
    {
        // Arrange & Act & Assert
        Assert.Throws<DivideByZeroException>(() => 50f.InverseLerp(100f, 100f));
    }
}
