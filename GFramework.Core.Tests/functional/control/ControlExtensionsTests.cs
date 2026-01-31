using GFramework.Core.functional.control;
using NUnit.Framework;

namespace GFramework.Core.Tests.functional.control;

/// <summary>
/// ControlExtensions扩展方法测试类，用于验证控制流函数式编程扩展方法的正确性
/// 包括模式匹配、条件执行等控制流功能的测试
/// </summary>
[TestFixture]
public class ControlExtensionsTests
{
    #region Match Tests
    
    /// <summary>
    /// 测试Match方法 - 验证模式匹配功能
    /// </summary>
    [Test]
    public void Match_Should_Execute_Matching_Case()
    {
        // Arrange
        var value = 5;
        
        // Act
        var result = value.Match(
            (x => x < 0, _ => "negative"),
            (x => x > 0, _ => "positive"),
            (x => x == 0, _ => "zero")
        );
        
        // Assert
        Assert.That(result, Is.EqualTo("positive"));
    }
    
    /// <summary>
    /// 测试Match方法 - 验证无匹配时抛出异常
    /// </summary>
    [Test]
    public void Match_Should_Throw_Exception_When_No_Case_Matches()
    {
        // Arrange
        var value = 10;
        
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            value.Match(
                (x => x < 0, _ => "negative"),
                (x => x > 10, _ => "large positive")
            )
        );
    }

    #endregion

    #region MatchOrDefault Tests
    
    /// <summary>
    /// 测试MatchOrDefault方法 - 验证模式匹配带默认值功能
    /// </summary>
    [Test]
    public void MatchOrDefault_Should_Execute_Matching_Case_Or_Return_Default()
    {
        // Arrange
        var value = 10;
        
        // Act
        var result = value.MatchOrDefault("unknown",
            (x => x < 0, _ => "negative"),
            (x => x > 10, _ => "large positive")
        );
        
        // Assert
        Assert.That(result, Is.EqualTo("unknown"));
    }

    #endregion

    #region If Tests
    
    /// <summary>
    /// 测试If方法 - 验证条件执行功能
    /// </summary>
    [Test]
    public void If_Should_Execute_ThenFunc_When_Condition_Is_True()
    {
        // Arrange
        var value = 5;
        
        // Act
        var result = value.If(x => x > 0, x => x * 2);
        
        // Assert
        Assert.That(result, Is.EqualTo(10));
    }
    
    /// <summary>
    /// 测试If方法 - 验证条件为假时不执行转换函数
    /// </summary>
    [Test]
    public void If_Should_Return_Original_Value_When_Condition_Is_False()
    {
        // Arrange
        var value = -5;
        
        // Act
        var result = value.If(x => x > 0, x => x * 2);
        
        // Assert
        Assert.That(result, Is.EqualTo(-5));
    }

    #endregion

    #region IfElse Tests
    
    /// <summary>
    /// 测试IfElse方法 - 验证条件分支功能
    /// </summary>
    [Test]
    public void IfElse_Should_Execute_ThenFunc_When_Condition_Is_True()
    {
        // Arrange
        var value = 5;
        
        // Act
        var result = value.IfElse(
            x => x > 0,
            x => x * 2,
            x => x * -1
        );
        
        // Assert
        Assert.That(result, Is.EqualTo(10));
    }
    
    /// <summary>
    /// 测试IfElse方法 - 验证条件为假时执行else分支
    /// </summary>
    [Test]
    public void IfElse_Should_Execute_ElseFunc_When_Condition_Is_False()
    {
        // Arrange
        var value = -5;
        
        // Act
        var result = value.IfElse(
            x => x > 0,
            x => x * 2,
            x => x * -1
        );
        
        // Assert
        Assert.That(result, Is.EqualTo(5));
    }

    #endregion

    #region TakeIf Tests
    
    /// <summary>
    /// 测试TakeIf方法 - 验证条件为真时返回原值
    /// </summary>
    [Test]
    public void TakeIf_Should_Return_Value_When_Condition_Is_True()
    {
        // Arrange
        string str = "Hello";
        
        // Act
        var result = str.TakeIf(s => s.Length > 3);
        
        // Assert
        Assert.That(result, Is.EqualTo("Hello"));
    }
    
    /// <summary>
    /// 测试TakeIf方法 - 验证条件为假时返回null
    /// </summary>
    [Test]
    public void TakeIf_Should_Return_Null_When_Condition_Is_False()
    {
        // Arrange
        string str = "Hi";
        
        // Act
        var result = str.TakeIf(s => s.Length > 3);
        
        // Assert
        Assert.That(result, Is.Null);
    }

    #endregion

    #region TakeUnless Tests
    
    /// <summary>
    /// 测试TakeUnless方法 - 验证条件为假时返回原值
    /// </summary>
    [Test]
    public void TakeUnless_Should_Return_Value_When_Condition_Is_False()
    {
        // Arrange
        string str = "Hi";
        
        // Act
        var result = str.TakeUnless(s => s.Length > 3);
        
        // Assert
        Assert.That(result, Is.EqualTo("Hi"));
    }
    
    /// <summary>
    /// 测试TakeUnless方法 - 验证条件为真时返回null
    /// </summary>
    [Test]
    public void TakeUnless_Should_Return_Null_When_Condition_Is_True()
    {
        // Arrange
        string str = "Hello";
        
        // Act
        var result = str.TakeUnless(s => s.Length > 3);
        
        // Assert
        Assert.That(result, Is.Null);
    }

    #endregion
}