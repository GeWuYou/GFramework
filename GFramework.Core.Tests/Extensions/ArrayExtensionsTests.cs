// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Tests.Extensions;

/// <summary>
///     测试 ArrayExtensions 扩展方法的关键行为与边界语义。
/// </summary>
[TestFixture]
public class ArrayExtensionsTests
{
    /// <summary>
    ///     验证 TryGet 在坐标有效时返回 true，并输出目标位置的元素值。
    /// </summary>
    [Test]
    public void TryGet_Should_Return_True_And_Assign_Value_When_Coordinates_Are_In_Bounds()
    {
        // Arrange
        var array = new[,]
        {
            { 1, 2 },
            { 3, 4 }
        };

        // Act
        var result = array.TryGet(1, 0, out var value);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(value, Is.EqualTo(3));
        });
    }

    /// <summary>
    ///     验证 TryGet 在值类型越界时返回 false，并将输出值重置为该类型的默认值。
    /// </summary>
    [Test]
    public void TryGet_Should_Return_False_And_Default_Value_When_Value_Type_Is_Out_Of_Bounds()
    {
        // Arrange
        var array = new[,]
        {
            { 1, 2 },
            { 3, 4 }
        };

        // Act
        var result = array.TryGet(2, 0, out int value);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(value, Is.EqualTo(default(int)));
        });
    }

    /// <summary>
    ///     验证 TryGet 在引用类型越界时返回 false，并将输出值设置为 null。
    /// </summary>
    [Test]
    public void TryGet_Should_Return_False_And_Null_When_Reference_Type_Is_Out_Of_Bounds()
    {
        // Arrange
        var array = new string[,]
        {
            { "A", "B" },
            { "C", "D" }
        };

        // Act
        var result = array.TryGet(-1, 0, out string? value);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(value, Is.Null);
        });
    }

    /// <summary>
    ///     验证 Enumerate 会按第一维优先、第二维递增的顺序返回所有坐标和值。
    /// </summary>
    [Test]
    public void Enumerate_Should_Return_All_Coordinates_And_Values_In_Deterministic_Order()
    {
        // Arrange
        var array = new[,]
        {
            { 10, 20, 30 },
            { 40, 50, 60 }
        };

        // Act
        var result = array.Enumerate().ToArray();

        // Assert
        Assert.That(
            result,
            Is.EqualTo(
                new (int x, int y, int value)[]
                {
                    (0, 0, 10),
                    (0, 1, 20),
                    (0, 2, 30),
                    (1, 0, 40),
                    (1, 1, 50),
                    (1, 2, 60)
                }));
    }
}