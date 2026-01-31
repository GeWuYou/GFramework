using GFramework.Core.functional.collections;
using NUnit.Framework;

namespace GFramework.Core.Tests.functional.collections;

/// <summary>
/// EnumerableExtensions扩展方法测试类，用于验证集合函数式编程扩展方法的正确性
/// 包括Map、Filter、Reduce等集合操作功能的测试
/// </summary>
[TestFixture]
public class EnumerableExtensionsTests
{
    #region Map Tests
    
    /// <summary>
    /// 测试Map方法 - 验证集合中的每个元素都能被正确转换
    /// </summary>
    [Test]
    public void Map_Should_Transform_Each_Element_In_Collection()
    {
        // Arrange
        var numbers = new[] {1, 2, 3, 4};
        
        // Act
        var result = numbers.Map(x => x * x).ToArray();
        
        // Assert
        Assert.That(result, Is.EquivalentTo([1, 4, 9, 16]));
    }

    #endregion

    #region Filter Tests
    
    /// <summary>
    /// 测试Filter方法 - 验证集合能够根据条件正确过滤
    /// </summary>
    [Test]
    public void Filter_Should_Filter_Elements_Based_On_Predicate()
    {
        // Arrange
        var numbers = new[] {1, 2, 3, 4, 5, 6};
        
        // Act
        var result = numbers.Filter(x => x % 2 == 0).ToArray();
        
        // Assert
        Assert.That(result, Is.EquivalentTo([2, 4, 6]));
    }

    #endregion

    #region Reduce Tests
    
    /// <summary>
    /// 测试Reduce方法 - 验证集合能够正确归约为单个值
    /// </summary>
    [Test]
    public void Reduce_Should_Reduce_Collection_To_Single_Value()
    {
        // Arrange
        var numbers = new[] {1, 2, 3, 4};
        
        // Act
        var result = numbers.Reduce(0, (acc, x) => acc + x);
        
        // Assert
        Assert.That(result, Is.EqualTo(10));
    }

    #endregion
}