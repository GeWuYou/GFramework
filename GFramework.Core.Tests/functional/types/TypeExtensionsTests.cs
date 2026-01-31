using GFramework.Core.extensions;
using GFramework.Core.functional.types;
using NUnit.Framework;

namespace GFramework.Core.Tests.functional.types;

/// <summary>
/// TypeExtensions扩展方法测试类，用于验证类型转换相关扩展方法的正确性
/// 包括安全类型转换和强制类型转换功能的测试
/// </summary>
[TestFixture]
public class TypeExtensionsTests
{
    #region As Tests
    
    /// <summary>
    /// 测试As方法 - 验证安全类型转换功能
    /// </summary>
    [Test]
    public void As_Should_Perform_Safe_Type_Cast()
    {
        // Arrange
        object obj = "Hello";
        
        // Act
        var result = obj.As<string>();
        
        // Assert
        Assert.That(result, Is.EqualTo("Hello"));
    }
    
    /// <summary>
    /// 测试As方法 - 验证不兼容类型转换返回null
    /// </summary>
    [Test]
    public void As_Should_Return_Null_For_Incompatible_Types()
    {
        // Arrange
        object obj = 42;
        
        // Act
        var result = obj.As<string>();
        
        // Assert
        Assert.That(result, Is.Null);
    }

    #endregion

    #region Cast Tests
    
    /// <summary>
    /// 测试Cast方法 - 验证强制类型转换功能
    /// </summary>
    [Test]
    public void Cast_Should_Perform_Forced_Type_Cast()
    {
        // Arrange
        object obj = "Hello";
        
        // Act
        var result = obj.Cast<string>();
        
        // Assert
        Assert.That(result, Is.EqualTo("Hello"));
    }

    #endregion
}