using GFramework.Core.functional.functions;
using NUnit.Framework;

namespace GFramework.Core.Tests.functional.functions;

/// <summary>
/// FunctionExtensions扩展方法测试类，用于验证高级函数式编程扩展方法的正确性
/// 包括柯里化、偏函数应用、重复执行、安全执行和缓存等功能的测试
/// </summary>
[TestFixture]
public class FunctionExtensionsTests
{
    #region Curry Tests
    
    /// <summary>
    /// 测试Curry方法 - 验证二参数函数能够正确柯里化
    /// </summary>
    [Test]
    public void Curry_Should_Convert_Binary_Function_To_Curried_Form()
    {
        // Arrange
        Func<int, int, int> add = (x, y) => x + y;
        
        // Act
        var curriedAdd = add.Curry();
        var addFive = curriedAdd(5);
        var result = addFive(3);
        
        // Assert
        Assert.That(result, Is.EqualTo(8));
    }

    #endregion

    #region Uncurry Tests
    
    /// <summary>
    /// 测试Uncurry方法 - 验证柯里化函数能够正确还原为二参数函数
    /// </summary>
    [Test]
    public void Uncurry_Should_Convert_Curried_Function_Back_To_Binary_Form()
    {
        // Arrange
        Func<int, int, int> originalAdd = (x, y) => x + y;
        var curriedAdd = originalAdd.Curry();
        
        // Act
        var uncurriedAdd = curriedAdd.Uncurry();
        var result = uncurriedAdd(5, 3);
        
        // Assert
        Assert.That(result, Is.EqualTo(8));
    }

    #endregion

    #region Partial Tests
    
    /// <summary>
    /// 测试Partial方法 - 验证部分应用函数功能
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

    #endregion

    #region Repeat Tests
    
    /// <summary>
    /// 测试Repeat方法 - 验证重复执行函数功能
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

    #endregion

    #region Try Tests
    
    /// <summary>
    /// 测试Try方法 - 验证安全执行成功情况
    /// </summary>
    [Test]
    public void Try_Should_Return_Success_When_Function_Does_Not_Throw()
    {
        // Arrange
        var value = 10;
        
        // Act
        var (success, result, error) = value.Try(x => 100 / x);
        
        // Assert
        Assert.That(success, Is.True);
        Assert.That(result, Is.EqualTo(10));
        Assert.That(error, Is.Null);
    }
    
    /// <summary>
    /// 测试Try方法 - 验证安全执行异常情况
    /// </summary>
    [Test]
    public void Try_Should_Return_Failure_When_Function_Throws()
    {
        // Arrange
        var value = 0;
        
        // Act
        var (success, result, error) = value.Try(x => 100 / x);
        
        // Assert
        Assert.That(success, Is.False);
        Assert.That(result, Is.EqualTo(0)); // 对于int类型，默认值是0
        Assert.That(error, Is.Not.Null);
        Assert.That(error, Is.TypeOf<DivideByZeroException>());
    }

    #endregion

    #region Memoize Tests
    
    /// <summary>
    /// 测试Memoize方法 - 验证函数结果缓存功能
    /// </summary>
    [Test]
    public void Memoize_Should_Cache_Function_Results()
    {
        // Arrange
        var callCount = 0;
        Func<int, int> expensiveFunction = x =>
        {
            callCount++;
            return x * x;
        };
        var memoized = expensiveFunction.Memoize();
        
        // Act
        var result1 = memoized(5); // 第一次调用
        var result2 = memoized(5); // 第二次调用，应该使用缓存
        var result3 = memoized(3); // 新参数，应该调用函数
        var result4 = memoized(3); // 再次使用相同参数，应该使用缓存
        
        // Assert
        Assert.That(result1, Is.EqualTo(25));
        Assert.That(result2, Is.EqualTo(25));
        Assert.That(result3, Is.EqualTo(9));
        Assert.That(result4, Is.EqualTo(9));
        Assert.That(callCount, Is.EqualTo(2)); // 只应调用两次，而不是四次
    }

    #endregion
}