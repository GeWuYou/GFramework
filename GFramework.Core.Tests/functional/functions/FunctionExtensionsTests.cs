using GFramework.Core.functional.functions;
using NUnit.Framework;

namespace GFramework.Core.Tests.functional.functions;

/// <summary>
///     FunctionExtensions扩展方法测试类，用于验证高级函数式编程扩展方法的正确性
///     包括柯里化、偏函数应用、重复执行、安全执行和缓存等功能的测试
/// </summary>
[TestFixture]
public class FunctionExtensionsTests
{
    /// <summary>
    ///     测试Partial方法 - 验证部分应用函数功能
    /// </summary>
    [Test]
    public void Partial_Should_Fix_First_Argument_Of_Binary_Function()
    {
        // Arrange
        Func<int, int, int> multiply = (x, y) => x * y;

        // Act
        var doubleFunction = multiply.Partial(2);
        var result = doubleFunction(5);

        // Assert
        Assert.That(result, Is.EqualTo(10));
    }

    /// <summary>
    ///     测试Repeat方法 - 验证重复执行函数功能
    /// </summary>
    [Test]
    public void Repeat_Should_Execute_Function_N_Times()
    {
        // Arrange
        var initialValue = 2;

        // Act
        var result = initialValue.Repeat(3, x => x * 2);

        // Assert
        // 2 -> 4 -> 8 -> 16 (3次重复)
        Assert.That(result, Is.EqualTo(16));
    }
}