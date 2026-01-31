using GFramework.Core.functional.pipe;
using NUnit.Framework;

namespace GFramework.Core.Tests.functional.pipe;

/// <summary>
/// PipeExtensions扩展方法测试类，用于验证管道和函数组合扩展方法的正确性
/// 包括管道操作、函数组合等核心功能的测试
/// </summary>
[TestFixture]
public class PipeExtensionsTests
{
    /// <summary>
    /// 测试Pipe方法 - 验证值能够正确传递给函数并返回结果
    /// </summary>
    [Test]
    public void Pipe_Should_Execute_Function_And_Return_Result()
    {
        // Arrange
        var value = 5;

        // Act
        var result = value.Pipe(x => x * 2);

        // Assert
        Assert.That(result, Is.EqualTo(10));
    }

    /// <summary>
    /// 测试Then方法 - 验证两个函数能够正确组合执行
    /// </summary>
    [Test]
    public void Then_Should_Compose_Two_Functions()
    {
        // Arrange
        Func<int, int> addTwo = x => x + 2;

        // Act
        var composed = addTwo.Then((Func<int, int>)MultiplyByThree);
        var result = composed(5);

        // Assert
        Assert.That(result, Is.EqualTo(21)); // (5+2)*3 = 21
        return;

        int MultiplyByThree(int x) => x * 3;
    }

    /// <summary>
    /// 测试After方法 - 验证反向函数组合的正确性
    /// </summary>
    [Test]
    public void After_Should_Compose_Functions_In_Reversed_Order()
    {
        // Arrange
        Func<int, int> multiplyByThree = x => x * 3;

        // Act
        var composed = multiplyByThree.After((Func<int, int>)AddTwo);
        var result = composed(5);

        // Assert
        Assert.That(result, Is.EqualTo(21)); // (5+2)*3 = 21
        return;

        int AddTwo(int x) => x + 2;
    }

    /// <summary>
    /// 测试Apply方法 - 验证函数能够正确应用到参数上
    /// </summary>
    [Test]
    public void Apply_Should_Apply_Function_To_Argument()
    {
        // Arrange
        Func<int, int> multiplyByTwo = x => x * 2;

        // Act
        var result = multiplyByTwo.Apply(5);

        // Assert
        Assert.That(result, Is.EqualTo(10));
    }

    /// <summary>
    /// 测试Also方法 - 验证执行操作后返回原值功能
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
    /// 测试Let方法 - 验证值转换功能
    /// </summary>
    [Test]
    public void Let_Should_Transform_Value()
    {
        // Arrange
        var value = 5;

        // Act
        var result = value.Let(x => x * 2);

        // Assert
        Assert.That(result, Is.EqualTo(10));
    }
}