using GFramework.Core.functional.types;
using NUnit.Framework;

namespace GFramework.Core.Tests.functional.types;

/// <summary>
/// OptionExtensions扩展方法测试类，用于验证Option类型的功能扩展方法
/// 包括映射、绑定、过滤和匹配操作等功能的测试
/// </summary>
[TestFixture]
public class OptionExtensionsTests
{
    /// <summary>
    /// 测试Map方法 - 验证Some值能正确映射
    /// </summary>
    [Test]
    public void Option_Map_Should_Transform_Some_Value()
    {
        // Arrange
        var option = Option<string>.Some("hello");

        // Act
        var result = option.Map(s => s.Length);

        // Assert
        Assert.That(result.IsSome, Is.True);
        Assert.That(result.Value, Is.EqualTo(5));
    }

    /// <summary>
    /// 测试Map方法 - 验证None映射后仍为None
    /// </summary>
    [Test]
    public void Option_Map_Should_Return_None_For_None()
    {
        // Arrange
        var option = Option<string>.None();

        // Act
        var result = option.Map(s => s.Length);

        // Assert
        Assert.That(result.IsNone, Is.True);
    }

    /// <summary>
    /// 测试Bind方法 - 验证Some值能正确绑定到另一个Option
    /// </summary>
    [Test]
    public void Option_Bind_Should_Transform_Some_To_Another_Option()
    {
        // Arrange
        var option = Option<string>.Some("hello");

        // Act
        var result = option.Bind(s => s.Length > 3 ? Option<int>.Some(s.Length) : Option<int>.None());

        // Assert
        Assert.That(result.IsSome, Is.True);
        Assert.That(result.Value, Is.EqualTo(5));
    }

    /// <summary>
    /// 测试Bind方法 - 验证None绑定后仍为None
    /// </summary>
    [Test]
    public void Option_Bind_Should_Return_None_For_None()
    {
        // Arrange
        var option = Option<string>.None();

        // Act
        var result = option.Bind(s => Option<int>.Some(s.Length));

        // Assert
        Assert.That(result.IsNone, Is.True);
    }

    /// <summary>
    /// 测试Bind方法 - 验证Some值绑定到None的情况
    /// </summary>
    [Test]
    public void Option_Bind_Should_Return_None_When_Binder_Returns_None()
    {
        // Arrange
        var option = Option<string>.Some("hi"); // 长度小于3

        // Act
        var result = option.Bind(s => s.Length > 3 ? Option<int>.Some(s.Length) : Option<int>.None());

        // Assert
        Assert.That(result.IsNone, Is.True);
    }

    /// <summary>
    /// 测试Filter方法 - 验证满足条件的Some值保留
    /// </summary>
    [Test]
    public void Option_Filter_Should_Keep_Some_When_Predicate_Matches()
    {
        // Arrange
        var option = Option<string>.Some("hello");

        // Act
        var result = option.Filter(s => s.Length > 3);

        // Assert
        Assert.That(result.IsSome, Is.True);
        Assert.That(result.Value, Is.EqualTo("hello"));
    }

    /// <summary>
    /// 测试Filter方法 - 验证不满足条件的Some值变为None
    /// </summary>
    [Test]
    public void Option_Filter_Should_Return_None_When_Predicate_Does_Not_Match()
    {
        // Arrange
        var option = Option<string>.Some("hi");

        // Act
        var result = option.Filter(s => s.Length > 3);

        // Assert
        Assert.That(result.IsNone, Is.True);
    }

    /// <summary>
    /// 测试Filter方法 - 验证None过滤后仍为None
    /// </summary>
    [Test]
    public void Option_Filter_Should_Return_None_For_None()
    {
        // Arrange
        var option = Option<string>.None();

        // Act
        var result = option.Filter(s => s.Length > 3);

        // Assert
        Assert.That(result.IsNone, Is.True);
    }

    /// <summary>
    /// 测试Match方法 - 验证Some值执行some分支
    /// </summary>
    [Test]
    public void Option_Match_Should_Execute_Some_Branch_For_Some()
    {
        // Arrange
        var option = Option<string>.Some("hello");

        // Act
        var result = option.Match(
            some: s => $"Value: {s}",
            none: () => "No value"
        );

        // Assert
        Assert.That(result, Is.EqualTo("Value: hello"));
    }

    /// <summary>
    /// 测试Match方法 - 验证None值执行none分支
    /// </summary>
    [Test]
    public void Option_Match_Should_Execute_None_Branch_For_None()
    {
        // Arrange
        var option = Option<string>.None();

        // Act
        var result = option.Match(
            some: s => $"Value: {s}",
            none: () => "No value"
        );

        // Assert
        Assert.That(result, Is.EqualTo("No value"));
    }
}