// Copyright (c) 2025 GeWuYou
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using GFramework.Core.Functional;
using NUnit.Framework;

namespace GFramework.Core.Tests.Functional;

/// <summary>
///     Result 类型测试类
/// </summary>
[TestFixture]
public class ResultTests
{
    /// <summary>
    ///     测试 Success 方法应该创建成功结果
    /// </summary>
    [Test]
    public void Success_Should_Create_Successful_Result()
    {
        // Arrange & Act
        var result = Result.Success();

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.IsFailure, Is.False);
    }

    /// <summary>
    ///     测试 Failure 方法使用异常应该创建失败结果
    /// </summary>
    [Test]
    public void Failure_WithException_Should_Create_Failed_Result()
    {
        // Arrange
        var exception = new InvalidOperationException("Test error");

        // Act
        var result = Result.Failure(exception);

        // Assert
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.SameAs(exception));
    }

    /// <summary>
    ///     测试 Failure 方法使用消息应该创建带异常的失败结果
    /// </summary>
    [Test]
    public void Failure_WithMessage_Should_Create_Failed_Result_With_Exception()
    {
        // Arrange
        var message = "Test error message";

        // Act
        var result = Result.Failure(message);

        // Assert
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error.Message, Is.EqualTo(message));
    }

    /// <summary>
    ///     测试 Failure 方法使用 null 异常应该抛出 ArgumentNullException
    /// </summary>
    [Test]
    public void Failure_WithNullException_Should_Throw_ArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Result.Failure((Exception)null!));
    }

    /// <summary>
    ///     测试 Failure 方法使用 null 或空消息应该抛出 ArgumentException
    /// </summary>
    [Test]
    public void Failure_WithNullOrEmptyMessage_Should_Throw_ArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Result.Failure(string.Empty));
        Assert.Throws<ArgumentException>(() => Result.Failure(""));
    }

    /// <summary>
    ///     测试 IsSuccess 属性在结果成功时应该返回 true
    /// </summary>
    [Test]
    public void IsSuccess_Should_Return_True_When_Result_Is_Successful()
    {
        // Arrange
        var result = Result.Success();

        // Act & Assert
        Assert.That(result.IsSuccess, Is.True);
    }

    /// <summary>
    ///     测试 IsFailure 属性在结果失败时应该返回 true
    /// </summary>
    [Test]
    public void IsFailure_Should_Return_True_When_Result_Is_Failed()
    {
        // Arrange
        var result = Result.Failure(new Exception("Error"));

        // Acert
        Assert.That(result.IsFailure, Is.True);
    }

    /// <summary>
    ///     测试 Error 属性在结果失败时应该返回异常
    /// </summary>
    [Test]
    public void Error_Should_Return_Exception_When_Result_Is_Failed()
    {
        // Arrange
        var exception = new InvalidOperationException("Test");
        var result = Result.Failure(exception);

        // Act
        var error = result.Error;

        // Assert
        Assert.That(error, Is.SameAs(exception));
    }

    /// <summary>
    ///     测试 Error 属性在结果成功时应该抛出 InvalidOperationException
    /// </summary>
    [Test]
    public void Error_Should_Throw_InvalidOperationException_When_Result_Is_Successful()
    {
        // Arrange
        var result = Result.Success();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _ = result.Error);
    }

    /// <summary>
    ///     测试 Match 方法在结果成功时应该执行 onSuccess 委托
    /// </summary>
    [Test]
    public void Match_Should_Execute_OnSuccess_When_Result_Is_Successful()
    {
        // Arrange
        var result = Result.Success();
        var executed = false;

        // Act
        var value = result.Match(
            onSuccess: () =>
            {
                executed = true;
                return "success";
            },
            onFailure: _ => "failure"
        );

        // Assert
        Assert.That(executed, Is.True);
        Assert.That(value, Is.EqualTo("success"));
    }

    /// <summary>
    ///     测试 Match 方法在结果失败时应该执行 onFailure 委托
    /// </summary>
    [Test]
    public void Match_Should_Execute_OnFailure_WhenFailed()
    {
        // Arrange
        var exception = new Exception("Error");
        var result = Result.Failure(exception);
        var executed = false;

        // Act
        var value = result.Match(
            onSuccess: () => "success",
            onFailure: _ =>
            {
                executed = true;
                return "failure";
            }
        );

        // Assert
        Assert.That(executed, Is.True);
        Assert.That(value, Is.EqualTo("failure"));
    }

    /// <summary>
    ///     测试 Match 方法应该将异常传递给 onFailure 处理器
    /// </summary>
    [Test]
    public void Match_Should_Pass_Exception_To_OnFailure_Handler()
    {
        // Arrange
        var exception = new InvalidOperationException("Test error");
        var result = Result.Failure(exception);
        Exception? capturedEx = null;

        // Act
        result.Match(
            onSuccess: () => 0,
            onFailure: ex =>
            {
                capturedEx = ex;
                return 1;
            }
        );

        // Assert
        Assert.That(capturedEx, Is.SameAs(exception));
    }

    /// <summary>
    ///     测试 ToResult 方法应该将成功结果转换为泛型成功结果
    /// </summary>
    [Test]
    public void ToResult_Should_Convert_Success_To_Generic_Success()
    {
        // Arrange
        var result = Result.Success();

        // Act
        var genericResult = result.ToResult(42);

        // Assert
        Assert.That(genericResult.IsSuccess, Is.True);
        Assert.That(genericResult.Match(succ: v => v, fail: _ => 0), Is.EqualTo(42));
    }

    /// <summary>
    ///     测试 ToResult 方法应该将失败结果转换为泛型失败结果
    /// </summary>
    [Test]
    public void ToResult_Should_Convert_Failure_To_Generic_Failure()
    {
        // Arrange
        var exception = new Exception("Error");
        var result = Result.Failure(exception);

        // Act
        var genericResult = result.ToResult(42);

        // Assert
        Assert.That(genericResult.IsFaulted, Is.True);
        Assert.That(genericResult.Exception, Is.SameAs(exception));
    }

    /// <summary>
    ///     测试 ToResult 方法应该保留异常信息
    /// </summary>
    [Test]
    public void ToResult_Should_Preserve_Exception_Information()
    {
        // Arrange
        var exception = new InvalidOperationException("Original error");
        var result = Result.Failure(exception);

        // Act
        var genericResult = result.ToResult("value");

        // Assert
        Assert.That(genericResult.Exception.Message, Is.EqualTo("Original error"));
        Assert.That(genericResult.Exception, Is.TypeOf<InvalidOperationException>());
    }

    /// <summary>
    ///     测试 Equals 方法在两个结果都成功时应该返回 true
    /// </summary>
    [Test]
    public void Equals_Should_Return_True_When_Both_Are_Successful()
    {
        // Arrange
        var result1 = Result.Success();
        var result2 = Result.Success();

        // Act & Assert
        Assert.That(result1.Equals(result2), Is.True);
        Assert.That(result1 == result2, Is.True);
    }

    /// <summary>
    ///     测试 Equals 方法在两个结果都失败且异常类型和消息相同时应该返回 true
    /// </summary>
    [Test]
    public void Equals_Should_Return_True_When_Both_Failed_With_Same_Exception_Type_And_Message()
    {
        // Arrange
        var result1 = Result.Failure(new InvalidOperationException("Error"));
        var result2 = Result.Failure(new InvalidOperationException("Error"));

        // Act & Assert
        Assert.That(result1.Equals(result2), Is.True);
    }

    /// <summary>
    ///     测试 Equals 方法在状态不同时应该返回 false
    /// </summary>
    [Test]
    public void Equals_Should_Return_False_When_States_Differ()
    {
        // Arrange
        var result1 = Result.Success();
        var result2 = Result.Failure(new Exception("Error"));

        // Act & Assert
        Assert.That(result1.Equals(result2), Is.False);
        Assert.That(result1 != result2, Is.True);
    }

    /// <summary>
    ///     测试 Equals 方法在异常类型不同时应该返回 false
    /// </summary>
    [Test]
    public void Equals_Should_Return_False_When_Exception_Types_Differ()
    {
        // Arrange
        var result1 = Result.Failure(new InvalidOperationException("Error"));
        var result2 = Result.Failure(new ArgumentException("Error"));

        // Act & Assert
        Assert.That(result1.Equals(result2), Is.False);
    }

    /// <summary>
    ///     测试 Equals 方法在异常消息不同时应该返回 false
    /// </summary>
    [Test]
    public void Equals_Should_Return_False_When_Exception_Messages_Differ()
    {
        // Arrange
        var result1 = Result.Failure(new Exception("Error1"));
        var result2 = Result.Failure(new Exception("Error2"));

        // Act & Assert
        Assert.That(result1.Equals(result2), Is.False);
    }

    /// <summary>
    ///     测试 GetHashCode 方法对于相等的结果应该返回一致的哈希码
    /// </summary>
    [Test]
    public void GetHashCode_Should_Be_Consistent_For_Equal_Results()
    {
        // Arrange
        var result1 = Result.Success();
        var result2 = Result.Success();

        // Act
        var hash1 = result1.GetHashCode();
        var hash2 = result2.GetHashCode();

        // Assert
        Assert.That(hash1, Is.EqualTo(hash2));
    }

    /// <summary>
    ///     测试 == 操作符应该正确工作
    /// </summary>
    [Test]
    public void OperatorEquals_Should_Work_Correctly()
    {
        // Arrange
        var result1 = Result.Success();
        var result2 = Result.Success();
        var result3 = Result.Failure(new Exception("Error"));

        // Act & Assert
        Assert.That(result1 == result2, Is.True);
        Assert.That(result1 == result3, Is.False);
    }

    /// <summary>
    ///     测试 != 操作符应该正确工作
    /// </summary>
    [Test]
    public void OperatorNotEquals_Should_Work_Correctly()
    {
        // Arrange
        var result1 = Result.Success();
        var result2 = Result.Failure(new Exception("Error"));

        // Act & Assert
        Assert.That(result1 != result2, Is.True);
        Assert.That(result1 != Result.Success(), Is.False);
    }

    /// <summary>
    ///     测试 ToString 方法在结果成功时应该返回 "Success"
    /// </summary>
    [Test]
    public void ToString_Should_Return_Success_When_Successful()
    {
        // Arrange
        var result = Result.Success();

        // Act
        var str = result.ToString();

        // Assert
        Assert.That(str, Is.EqualTo("Success"));
    }

    /// <summary>
    ///     测试 ToString 方法在结果失败时应该返回带消息的 "Fail"
    /// </summary>
    [Test]
    public void ToString_Should_Return_Fail_With_Message_When_Failed()
    {
        // Arrange
        var result = Result.Failure(new Exception("Test error"));

        // Act
        var str = result.ToString();

        // Assert
        Assert.That(str, Is.EqualTo("Fail(Test error)"));
    }
}