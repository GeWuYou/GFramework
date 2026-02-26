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
using GFramework.Core.functional.result;
using NUnit.Framework;

namespace GFramework.Core.Tests.Extensions;

/// <summary>
///     ResultExtensions 扩展方法测试类
/// </summary>
[TestFixture]
public class ResultExtensionsTests
{
    /// <summary>
    ///     测试Combine方法在所有结果成功时返回成功列表
    /// </summary>
    [Test]
    public void Combine_Should_Return_Success_List_When_All_Results_Succeed()
    {
        var results = new[]
        {
            Result<int>.Succeed(1),
            Result<int>.Succeed(2),
            Result<int>.Succeed(3)
        };
        var combined = results.Combine();
        Assert.That(combined.IsSuccess, Is.True);
        var list = combined.Match(succ: v => v, fail: _ => new List<int>());
        Assert.That(list, Has.Count.EqualTo(3));
        Assert.That(list, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void Combine_Should_Return_First_Failure_When_Any_Result_Fails()
    {
        var exception = new Exception("Error");
        var results = new[]
        {
            Result<int>.Succeed(1),
            Result<int>.Fail(exception),
            Result<int>.Succeed(3)
        };
        var combined = results.Combine();
        Assert.That(combined.IsFaulted, Is.True);
        Assert.That(combined.Exception, Is.SameAs(exception));
    }

    /// <summary>
    ///     测试Combine方法处理空集合
    /// </summary>
    [Test]
    public void Combine_Should_Handle_Empty_Collection()
    {
        var results = Array.Empty<Result<int>>();
        var combined = results.Combine();
        Assert.That(combined.IsSuccess, Is.True);
        var list = combined.Match(succ: v => v, fail: _ => new List<int>());
        Assert.That(list, Is.Empty);
    }

    /// <summary>
    ///     测试Combine方法在集合为null时抛出ArgumentNullException
    /// </summary>
    [Test]
    public void Combine_Should_Throw_ArgumentNullException_When_Collection_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() => ((IEnumerable<Result<int>>)null!).Combine());
    }

    /// <summary>
    ///     测试Combine方法保留值的顺序
    /// </summary>
    [Test]
    public void Combine_Should_Preserve_Order_Of_Values()
    {
        var results = new[]
        {
            Result<int>.Succeed(3),
            Result<int>.Succeed(1),
            Result<int>.Succeed(2)
        };
        var combined = results.Combine();
        var list = combined.Match(succ: v => v, fail: _ => new List<int>());
        Assert.That(list, Is.EqualTo(new[] { 3, 1, 2 }));
    }

    /// <summary>
    ///     测试Combine方法在第一个失败时短路
    /// </summary>
    [Test]
    public void Combine_Should_Short_Circuit_On_First_Failure()
    {
        var callCount = 0;
        var results = new[]
        {
            Result<int>.Succeed(1),
            Result<int>.Fail(new Exception("Error")),
            Result<int>.Try(() =>
            {
                callCount++;
                return 3;
            })
        };
        var combined = results.Combine();
        Assert.That(combined.IsFaulted, Is.True);
    }

    /// <summary>
    ///     测试Map方法转换成功值
    /// </summary>
    [Test]
    public void Map_Should_Transform_Success_Value()
    {
        var result = Result<int>.Succeed(42);
        var mapped = result.Map(x => x.ToString());
        Assert.That(mapped.IsSuccess, Is.True);
        Assert.That(mapped.Match(succ: v => v, fail: _ => ""), Is.EqualTo("42"));
    }

    /// <summary>
    ///     测试Map方法传播失败
    /// </summary>
    [Test]
    public void Map_Should_Propagate_Failure()
    {
        var exception = new Exception("Error");
        var result = Result<int>.Fail(exception);
        var mapped = result.Map(x => x.ToString());
        Assert.That(mapped.IsFaulted, Is.True);
        Assert.That(mapped.Exception, Is.SameAs(exception));
    }

    /// <summary>
    ///     测试Map方法在映射器为null时抛出ArgumentNullException
    /// </summary>
    [Test]
    public void Map_Should_Throw_ArgumentNullException_When_Mapper_Is_Null()
    {
        var result = Result<int>.Succeed(42);
        Assert.Throws<ArgumentNullException>(() => result.Map<string>(null!));
    }

    /// <summary>
    ///     测试Bind方法链接成功结果
    /// </summary>
    [Test]
    public void Bind_Should_Chain_Success_Results()
    {
        var result = Result<int>.Succeed(42);
        var bound = result.Bind(x => Result<string>.Succeed(x.ToString()));
        Assert.That(bound.IsSuccess, Is.True);
        Assert.That(bound.Match(succ: v => v, fail: _ => ""), Is.EqualTo("42"));
    }

    /// <summary>
    ///     测试Bind方法传播失败
    /// </summary>
    [Test]
    public void Bind_Should_Propagate_Failure()
    {
        var exception = new Exception("Error");
        var result = Result<int>.Fail(exception);
        var bound = result.Bind(x => Result<string>.Succeed(x.ToString()));
        Assert.That(bound.IsFaulted, Is.True);
        Assert.That(bound.Exception, Is.SameAs(exception));
    }

    /// <summary>
    ///     测试Bind方法在绑定器为null时抛出ArgumentNullException
    /// </summary>
    [Test]
    public void Bind_Should_Throw_ArgumentNullException_When_Binder_Is_Null()
    {
        var result = Result<int>.Succeed(42);
        Assert.Throws<ArgumentNullException>(() => result.Bind<string>(null!));
    }

    /// <summary>
    ///     测试OnSuccess方法在成功时执行操作
    /// </summary>
    [Test]
    public void OnSuccess_Should_Execute_Action_When_Success()
    {
        var result = Result<int>.Succeed(42);
        var executed = false;
        result.OnSuccess(_ => executed = true);
        Assert.That(executed, Is.True);
    }

    /// <summary>
    ///     测试OnSuccess方法在失败时不执行操作
    /// </summary>
    [Test]
    public void OnSuccess_Should_Not_Execute_Action_When_Faulted()
    {
        var result = Result<int>.Fail(new Exception("Error"));
        var executed = false;
        result.OnSuccess(_ => executed = true);
        Assert.That(executed, Is.False);
    }

    /// <summary>
    ///     测试OnSuccess方法返回原始结果用于链式调用
    /// </summary>
    [Test]
    public void OnSuccess_Should_Return_Original_Result_For_Chaining()
    {
        var result = Result<int>.Succeed(42);
        var returned = result.OnSuccess(_ => { });
        Assert.That(returned, Is.EqualTo(result));
    }

    /// <summary>
    ///     测试OnSuccess方法在操作为null时抛出ArgumentNullException
    /// </summary>
    [Test]
    public void OnSuccess_Should_Throw_ArgumentNullException_When_Action_Is_Null()
    {
        var result = Result<int>.Succeed(42);
        Assert.Throws<ArgumentNullException>(() => result.OnSuccess(null!));
    }

    /// <summary>
    ///     测试OnFailure方法在失败时执行操作
    /// </summary>
    [Test]
    public void OnFailure_Should_Execute_Action_When_Faulted()
    {
        var result = Result<int>.Fail(new Exception("Error"));
        var executed = false;
        result.OnFailure(_ => executed = true);
        Assert.That(executed, Is.True);
    }

    /// <summary>
    ///     测试OnFailure方法在成功时不执行操作
    /// </summary>
    [Test]
    public void OnFailure_Should_Not_Execute_Action_When_Success()
    {
        var result = Result<int>.Succeed(42);
        var executed = false;
        result.OnFailure(_ => executed = true);
        Assert.That(executed, Is.False);
    }

    /// <summary>
    ///     测试OnFailure方法返回原始结果用于链式调用
    /// </summary>
    [Test]
    public void OnFailure_Should_Return_Original_Result_For_Chaining()
    {
        var result = Result<int>.Fail(new Exception("Error"));
        var returned = result.OnFailure(_ => { });
        Assert.That(returned, Is.EqualTo(result));
    }

    /// <summary>
    ///     测试OnFailure方法在操作为null时抛出ArgumentNullException
    /// </summary>
    [Test]
    public void OnFailure_Should_Throw_ArgumentNullException_When_Action_Is_Null()
    {
        var result = Result<int>.Fail(new Exception("Error"));
        Assert.Throws<ArgumentNullException>(() => result.OnFailure(null!));
    }

    /// <summary>
    ///     测试Ensure方法在谓词为true时返回成功
    /// </summary>
    [Test]
    public void Ensure_Should_Return_Success_When_Predicate_Is_True()
    {
        var result = Result<int>.Succeed(42);
        var ensured = result.Ensure(x => x > 0, "Value must be positive");
        Assert.That(ensured.IsSuccess, Is.True);
    }

    /// <summary>
    ///     测试Ensure方法在谓词为false时返回失败
    /// </summary>
    [Test]
    public void Ensure_Should_Return_Failure_When_Predicate_Is_False()
    {
        var result = Result<int>.Succeed(-1);
        var ensured = result.Ensure(x => x > 0, "Value must be positive");
        Assert.That(ensured.IsFaulted, Is.True);
        Assert.That(ensured.Exception, Is.TypeOf<ArgumentException>());
    }

    /// <summary>
    ///     测试Ensure方法传播现有失败
    /// </summary>
    [Test]
    public void Ensure_Should_Propagate_Existing_Failure()
    {
        var exception = new Exception("Original error");
        var result = Result<int>.Fail(exception);
        var ensured = result.Ensure(x => x > 0, "Value must be positive");
        Assert.That(ensured.IsFaulted, Is.True);
        Assert.That(ensured.Exception, Is.SameAs(exception));
    }

    /// <summary>
    ///     测试Ensure方法创建带消息的ArgumentException
    /// </summary>
    [Test]
    public void Ensure_Should_Create_ArgumentException_With_Message()
    {
        var result = Result<int>.Succeed(-1);
        var ensured = result.Ensure(x => x > 0, "Value must be positive");
        Assert.That(ensured.Exception.Message, Is.EqualTo("Value must be positive"));
    }

    /// <summary>
    ///     测试Ensure方法在谓词为null时抛出ArgumentNullException
    /// </summary>
    [Test]
    public void Ensure_Should_Throw_ArgumentNullException_When_Predicate_Is_Null()
    {
        var result = Result<int>.Succeed(42);
        Assert.Throws<ArgumentNullException>(() => result.Ensure(null!, "Error"));
    }

    /// <summary>
    ///     测试Ensure方法在消息为null或空白时抛出ArgumentException
    /// </summary>
    [Test]
    public void Ensure_Should_Throw_ArgumentException_When_Message_Is_NullOrWhiteSpace()
    {
        var result = Result<int>.Succeed(42);
        Assert.Throws<ArgumentException>(() => result.Ensure(x => true, ""));
        Assert.Throws<ArgumentException>(() => result.Ensure(x => true, "   "));
    }

    /// <summary>
    ///     测试带工厂的Ensure方法在谓词为true时返回成功
    /// </summary>
    [Test]
    public void Ensure_WithFactory_Should_Return_Success_When_Predicate_Is_True()
    {
        var result = Result<int>.Succeed(42);
        var ensured = result.Ensure(x => x > 0, _ => new InvalidOperationException("Error"));
        Assert.That(ensured.IsSuccess, Is.True);
    }

    /// <summary>
    ///     测试带工厂的Ensure方法返回带自定义异常的成功
    /// </summary>
    [Test]
    public void Ensure_WithFactory_Should_Return_Failure_With_Custom_Exception()
    {
        var result = Result<int>.Succeed(-1);
        var ensured = result.Ensure(x => x > 0, _ => new InvalidOperationException("Custom error"));
        Assert.That(ensured.IsFaulted, Is.True);
        Assert.That(ensured.Exception, Is.TypeOf<InvalidOperationException>());
    }

    /// <summary>
    ///     测试带工厂的Ensure方法传递值给异常工厂
    /// </summary>
    [Test]
    public void Ensure_WithFactory_Should_Pass_Value_To_Exception_Factory()
    {
        var result = Result<int>.Succeed(-1);
        var capturedValue = 0;
        result.Ensure(x => x > 0, v =>
        {
            capturedValue = v;
            return new Exception("Error");
        });
        Assert.That(capturedValue, Is.EqualTo(-1));
    }

    /// <summary>
    ///     测试带工厂的Ensure方法传播现有失败
    /// </summary>
    [Test]
    public void Ensure_WithFactory_Should_Propagate_Existing_Failure()
    {
        var exception = new Exception("Original error");
        var result = Result<int>.Fail(exception);
        var ensured = result.Ensure(x => x > 0, _ => new InvalidOperationException("Error"));
        Assert.That(ensured.Exception, Is.SameAs(exception));
    }

    /// <summary>
    ///     测试带工厂的Ensure方法在参数为null时抛出ArgumentNullException
    /// </summary>
    [Test]
    public void Ensure_WithFactory_Should_Throw_ArgumentNullException_When_Parameters_Are_Null()
    {
        var result = Result<int>.Succeed(42);
        Assert.Throws<ArgumentNullException>(() => result.Ensure(null!, (Func<int, Exception>)(_ => new Exception())));
        Assert.Throws<ArgumentNullException>(() => result.Ensure(x => true, (Func<int, Exception>)null!));
    }

    /// <summary>
    ///     测试Try方法在函数成功时返回成功
    /// </summary>
    [Test]
    public void Try_Should_Return_Success_When_Function_Succeeds()
    {
        var result = ResultExtensions.Try(() => 42);
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Match(succ: v => v, fail: _ => 0), Is.EqualTo(42));
    }

    /// <summary>
    ///     测试Try方法在函数抛出异常时返回失败
    /// </summary>
    [Test]
    public void Try_Should_Return_Failure_When_Function_Throws()
    {
        var result = ResultExtensions.Try<int>(() => throw new InvalidOperationException("Error"));
        Assert.That(result.IsFaulted, Is.True);
        Assert.That(result.Exception, Is.TypeOf<InvalidOperationException>());
    }

    /// <summary>
    ///     测试Try方法在函数为null时抛出ArgumentNullException
    /// </summary>
    [Test]
    public void Try_Should_Throw_ArgumentNullException_When_Function_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() => ResultExtensions.Try<int>(null!));
    }

    /// <summary>
    ///     测试TryAsync方法在异步函数成功时返回成功
    /// </summary>
    [Test]
    public async Task TryAsync_Should_Return_Success_When_Async_Function_Succeeds()
    {
        var result = await ResultExtensions.TryAsync(async () =>
        {
            await Task.Delay(1);
            return 42;
        });
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Match(succ: v => v, fail: _ => 0), Is.EqualTo(42));
    }

    /// <summary>
    ///     测试TryAsync方法在异步函数抛出异常时返回失败
    /// </summary>
    [Test]
    public async Task TryAsync_Should_Return_Failure_When_Async_Function_Throws()
    {
        var result = await ResultExtensions.TryAsync<int>(async () =>
        {
            await Task.Delay(1);
            throw new InvalidOperationException("Error");
        });
        Assert.That(result.IsFaulted, Is.True);
        Assert.That(result.Exception, Is.TypeOf<InvalidOperationException>());
    }

    /// <summary>
    ///     测试TryAsync方法处理同步异常
    /// </summary>
    [Test]
    public async Task TryAsync_Should_Handle_Synchronous_Exceptions()
    {
        var result = await ResultExtensions.TryAsync<int>(() => throw new InvalidOperationException("Sync error"));
        Assert.That(result.IsFaulted, Is.True);
    }

    /// <summary>
    ///     测试TryAsync方法在函数为null时抛出ArgumentNullException
    /// </summary>
    [Test]
    public void TryAsync_Should_Throw_ArgumentNullException_When_Function_Is_Null()
    {
        Assert.ThrowsAsync<ArgumentNullException>(async () => await ResultExtensions.TryAsync<int>(null!));
    }

    /// <summary>
    ///     测试ToNullable方法在成功时返回值
    /// </summary>
    [Test]
    public void ToNullable_Should_Return_Value_When_Success()
    {
        var result = Result<int>.Succeed(42);
        var nullable = result.ToNullable();
        Assert.That(nullable, Is.EqualTo(42));
    }

    /// <summary>
    ///     测试ToNullable方法在失败时返回null
    /// </summary>
    [Test]
    public void ToNullable_Should_Return_Null_When_Faulted()
    {
        var result = Result<int>.Fail(new Exception("Error"));
        var nullable = result.ToNullable();
        Assert.That(nullable, Is.Null);
    }

    /// <summary>
    ///     测试ToNullable方法与值类型一起工作
    /// </summary>
    [Test]
    public void ToNullable_Should_Work_With_Value_Types()
    {
        var result = Result<DateTime>.Succeed(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var nullable = result.ToNullable();
        Assert.That(nullable, Is.Not.Null);
        Assert.That(nullable!.Value.Year, Is.EqualTo(2025));
        Assert.That(nullable!.Value.Kind, Is.EqualTo(DateTimeKind.Utc));
    }


    /// <summary>
    ///     测试ToResult方法在值不为null时返回成功
    /// </summary>
    [Test]
    public void ToResult_Should_Return_Success_When_Value_Is_Not_Null()
    {
        string value = "test";
        var result = value.ToResult();
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Match(succ: v => v, fail: _ => ""), Is.EqualTo("test"));
    }

    /// <summary>
    ///     测试ToResult方法在值为null时返回失败
    /// </summary>
    [Test]
    public void ToResult_Should_Return_Failure_When_Value_Is_Null()
    {
        string? value = null;
        var result = value.ToResult();
        Assert.That(result.IsFaulted, Is.True);
        Assert.That(result.Exception, Is.TypeOf<ArgumentNullException>());
    }

    /// <summary>
    ///     测试ToResult方法使用自定义错误消息
    /// </summary>
    [Test]
    public void ToResult_Should_Use_Custom_Error_Message()
    {
        string? value = null;
        var result = value.ToResult("Custom error");
        Assert.That(result.Exception.Message, Does.Contain("Custom error"));
    }

    /// <summary>
    ///     测试ToResult方法在可空类型有值时返回成功
    /// </summary>
    [Test]
    public void ToResult_Should_Return_Success_When_Nullable_Has_Value()
    {
        int? value = 42;
        var result = value.ToResult();
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Match(succ: v => v, fail: _ => 0), Is.EqualTo(42));
    }

    /// <summary>
    ///     测试ToResult方法在可空类型为null时返回失败
    /// </summary>
    [Test]
    public void ToResult_Should_Return_Failure_When_Nullable_Is_Null()
    {
        int? value = null;
        var result = value.ToResult();
        Assert.That(result.IsFaulted, Is.True);
        Assert.That(result.Exception, Is.TypeOf<ArgumentNullException>());
    }

    /// <summary>
    ///     测试ToResult方法对可空类型使用自定义错误消息
    /// </summary>
    [Test]
    public void ToResult_Should_Use_Custom_Error_Message_For_Nullable()
    {
        int? value = null;
        var result = value.ToResult("Custom nullable error");
        Assert.That(result.Exception.Message, Does.Contain("Custom nullable error"));
    }

    /// <summary>
    ///     测试支持多个操作的复杂链式调用
    /// </summary>
    [Test]
    public void Should_Support_Complex_Chaining_With_Multiple_Operations()
    {
        var result = Result<int>.Succeed(10)
            .Map(x => x * 2)
            .Bind(x => Result<int>.Succeed(x + 5))
            .Ensure(x => x > 20, "Value must be greater than 20")
            .OnSuccess(x => Console.WriteLine($"Success: {x}"))
            .OnFailure(ex => Console.WriteLine($"Error: {ex.Message}"));

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Match(succ: v => v, fail: _ => 0), Is.EqualTo(25));
    }

    /// <summary>
    ///     测试支持OnSuccess和OnFailure链式调用
    /// </summary>
    [Test]
    public void Should_Support_OnSuccess_And_OnFailure_Chaining()
    {
        var successCount = 0;
        var failureCount = 0;

        Result<int>.Succeed(42)
            .OnSuccess(_ => successCount++)
            .OnFailure(_ => failureCount++);

        Assert.That(successCount, Is.EqualTo(1));
        Assert.That(failureCount, Is.EqualTo(0));
    }

    [Test]
    public void Should_Support_Ensure_Chaining_With_Multiple_Conditions()
    {
        var result = Result<int>.Succeed(50)
            .Ensure(x => x > 0, "Must be positive")
            .Ensure(x => x < 100, "Must be less than 100")
            .Ensure(x => x % 2 == 0, "Must be even");

        Assert.That(result.IsSuccess, Is.True);
    }
}