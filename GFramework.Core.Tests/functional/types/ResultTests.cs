using GFramework.Core.functional.types;
using NUnit.Framework;

namespace GFramework.Core.Tests.functional.types;

/// <summary>
/// Result类型测试类，用于验证Result类型的基本功能
/// 包括创建Success和Failure实例、值访问等功能的测试
/// </summary>
[TestFixture]
public class ResultTests
{
    /// <summary>
    /// 测试创建Success实例 - 验证能正确创建成功结果
    /// </summary>
    [Test]
    public void Result_Success_Should_Create_With_Value()
    {
        // Arrange
        var value = "Success data";

        // Act
        var result = Result<string, string>.Success(value);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.IsFailure, Is.False);
        Assert.That(result.SuccessValue, Is.EqualTo(value));
    }

    /// <summary>
    /// 测试访问Success值 - 验证能正确获取成功值
    /// </summary>
    [Test]
    public void Result_Success_Value_Access_Should_Work()
    {
        // Arrange
        var result = Result<string, string>.Success("test");

        // Act & Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.SuccessValue, Is.EqualTo("test"));
    }

    /// <summary>
    /// 测试访问Success的错误值 - 验证在成功状态下访问错误值抛出异常
    /// </summary>
    [Test]
    public void Result_Success_Error_Access_Should_Throw_Exception()
    {
        // Arrange
        var result = Result<string, string>.Success("success");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
        {
            var _ = result.ErrorValue;
        });
    }

    /// <summary>
    /// 测试创建Failure实例 - 验证能正确创建失败结果
    /// </summary>
    [Test]
    public void Result_Failure_Should_Create_With_Error()
    {
        // Arrange
        var error = "Something went wrong";

        // Act
        var result = Result<string, string>.Failure(error);

        // Assert
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.ErrorValue, Is.EqualTo(error));
    }

    /// <summary>
    /// 测试访问Failure值 - 验证能正确获取错误值
    /// </summary>
    [Test]
    public void Result_Failure_Value_Access_Should_Work()
    {
        // Arrange
        var result = Result<string, string>.Failure("error");

        // Act & Assert
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.ErrorValue, Is.EqualTo("error"));
    }

    /// <summary>
    /// 测试访问Failure的成功值 - 验证在失败状态下访问成功值抛出异常
    /// </summary>
    [Test]
    public void Result_Failure_Success_Access_Should_Throw_Exception()
    {
        // Arrange
        var result = Result<string, string>.Failure("error");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
        {
            var _ = result.SuccessValue;
        });
    }
}