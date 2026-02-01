using GFramework.Core.functional.pipe;
using NUnit.Framework;

namespace GFramework.Core.Tests.functional.pipe;

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
}