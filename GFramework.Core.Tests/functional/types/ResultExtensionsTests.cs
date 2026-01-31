using GFramework.Core.functional.types;
using NUnit.Framework;

namespace GFramework.Core.Tests.functional.types;

/// <summary>
/// ResultExtensions扩展方法测试类，用于验证Result类型的功能扩展方法
/// 包括映射、绑定、错误映射和匹配操作等功能的测试
/// </summary>
[TestFixture]
public class ResultExtensionsTests
{
    /// <summary>
    /// 测试Map方法 - 验证Success值能正确映射
    /// </summary>
    [Test]
    public void Result_Map_Should_Transform_Success_Value()
    {
        // Arrange
        var result = Result<string, string>.Success("hello");

        // Act
        var mappedResult = result.Map(s => s.Length);

        // Assert
        Assert.That(mappedResult.IsSuccess, Is.True);
        Assert.That(mappedResult.SuccessValue, Is.EqualTo(5));
    }

    /// <summary>
    /// 测试Map方法 - 验证Failure映射后仍保持Failure状态
    /// </summary>
    [Test]
    public void Result_Map_Should_Keep_Failure_For_Failure()
    {
        // Arrange
        var result = Result<string, string>.Failure("error occurred");

        // Act
        var mappedResult = result.Map(s => s.Length);

        // Assert
        Assert.That(mappedResult.IsFailure, Is.True);
        Assert.That(mappedResult.ErrorValue, Is.EqualTo("error occurred"));
    }

    /// <summary>
    /// 测试Bind方法 - 验证Success值能正确绑定到另一个Result
    /// </summary>
    [Test]
    public void Result_Bind_Should_Transform_Success_To_Another_Result()
    {
        // Arrange
        var result = Result<string, string>.Success("hello");

        // Act
        var boundResult = result.Bind(s =>
            s.Length > 3 ? Result<int, string>.Success(s.Length) : Result<int, string>.Failure("Length too small"));

        // Assert
        Assert.That(boundResult.IsSuccess, Is.True);
        Assert.That(boundResult.SuccessValue, Is.EqualTo(5));
    }

    /// <summary>
    /// 测试Bind方法 - 验证Failure绑定后仍保持Failure状态
    /// </summary>
    [Test]
    public void Result_Bind_Should_Keep_Failure_For_Failure()
    {
        // Arrange
        var result = Result<string, string>.Failure("initial error");

        // Act
        var boundResult = result.Bind(s => Result<int, string>.Success(s.Length));

        // Assert
        Assert.That(boundResult.IsFailure, Is.True);
        Assert.That(boundResult.ErrorValue, Is.EqualTo("initial error"));
    }

    /// <summary>
    /// 测试Bind方法 - 验证Success值绑定到Failure的情况
    /// </summary>
    [Test]
    public void Result_Bind_Should_Allow_Transition_To_Failure()
    {
        // Arrange
        var result = Result<string, string>.Success("hi"); // 长度小于3

        // Act
        var boundResult = result.Bind(s =>
            s.Length > 3 ? Result<int, string>.Success(s.Length) : Result<int, string>.Failure("Length too small"));

        // Assert
        Assert.That(boundResult.IsFailure, Is.True);
        Assert.That(boundResult.ErrorValue, Is.EqualTo("Length too small"));
    }

    /// <summary>
    /// 测试MapError方法 - 验证Failure错误值能正确映射
    /// </summary>
    [Test]
    public void Result_MapError_Should_Transform_Failure_Error()
    {
        // Arrange
        var result = Result<string, string>.Failure("original error");

        // Act
        var mappedErrorResult = result.MapError(err => $"Mapped: {err}");

        // Assert
        Assert.That(mappedErrorResult.IsFailure, Is.True);
        Assert.That(mappedErrorResult.ErrorValue, Is.EqualTo("Mapped: original error"));
    }

    /// <summary>
    /// 测试MapError方法 - 验证Success映射错误后仍保持Success状态
    /// </summary>
    [Test]
    public void Result_MapError_Should_Keep_Success_For_Success()
    {
        // Arrange
        var result = Result<string, string>.Success("success data");

        // Act
        var mappedErrorResult = result.MapError(err => $"Mapped: {err}");

        // Assert
        Assert.That(mappedErrorResult.IsSuccess, Is.True);
        Assert.That(mappedErrorResult.SuccessValue, Is.EqualTo("success data"));
    }

    /// <summary>
    /// 测试Match方法 - 验证Success值执行onSuccess分支
    /// </summary>
    [Test]
    public void Result_Match_Should_Execute_OnSuccess_Branch_For_Success()
    {
        // Arrange
        var result = Result<string, string>.Success("success data");

        // Act
        var matchedResult = result.Match(
            onSuccess: data => $"Success: {data}",
            onFailure: error => $"Error: {error}"
        );

        // Assert
        Assert.That(matchedResult, Is.EqualTo("Success: success data"));
    }

    /// <summary>
    /// 测试Match方法 - 验证Failure值执行onFailure分支
    /// </summary>
    [Test]
    public void Result_Match_Should_Execute_OnFailure_Branch_For_Failure()
    {
        // Arrange
        var result = Result<string, string>.Failure("something failed");

        // Act
        var matchedResult = result.Match(
            onSuccess: data => $"Success: {data}",
            onFailure: error => $"Error: {error}"
        );

        // Assert
        Assert.That(matchedResult, Is.EqualTo("Error: something failed"));
    }
}