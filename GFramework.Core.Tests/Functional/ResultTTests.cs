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

using System.Globalization;
using GFramework.Core.Functional;
using NUnit.Framework;

namespace GFramework.Core.Tests.Functional;

/// <summary>
///     Result&lt;A&gt; 泛型类型测试类
/// </summary>
[TestFixture]
public class ResultTTests
{
    /// <summary>
    ///     测试构造函数使用值创建成功结果
    /// </summary>
    [Test]
    public void Constructor_WithValue_Should_Create_Success_Result()
    {
        var result = Result<int>.Success(42);
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.IsFailure, Is.False);
    }

    /// <summary>
    ///     测试构造函数使用异常创建失败结果
    /// </summary>
    [Test]
    public void Constructor_WithException_Should_Create_Faulted_Result()
    {
        var exception = new InvalidOperationException("Test error");
        var result = Result<int>.Failure(exception);
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.SameAs(exception));
    }

    /// <summary>
    ///     测试构造函数使用null异常时抛出ArgumentNullException
    /// </summary>
    [Test]
    public void Constructor_WithNullException_Should_Throw_ArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Result<int>.Failure((Exception)null!));
    }

    /// <summary>
    ///     测试默认构造函数创建DefaultFailure状态
    /// </summary>
    [Test]
    public void DefaultConstructor_Should_Create_DefaultFailure_State()
    {
        var result = new Result<int>();
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.IsFailure, Is.True);
    }

    /// <summary>
    ///     测试隐式转换创建成功结果
    /// </summary>
    [Test]
    public void ImplicitConversion_Should_Create_Success_Result()
    {
        Result<int> result = 42;
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Match(succ: v => v, fail: _ => 0), Is.EqualTo(42));
    }

    /// <summary>
    ///     测试IsSuccess属性在成功时返回true
    /// </summary>
    [Test]
    public void IsSuccess_Should_Return_True_When_Result_Is_Successful()
    {
        var result = Result<int>.Success(42);
        Assert.That(result.IsSuccess, Is.True);
    }

    /// <summary>
    ///     测试IsFaulted属性在失败时返回true
    /// </summary>
    [Test]
    public void IsFaulted_Should_Return_True_When_Result_Is_Faulted()
    {
        var result = Result<int>.Failure(new Exception("Error"));
        Assert.That(result.IsFailure, Is.True);
    }


    /// <summary>
    ///     测试Exception属性在失败时返回异常
    /// </summary>
    [Test]
    public void Exception_Should_Return_Exception_When_Faulted()
    {
        var exception = new InvalidOperationException("Test");
        var result = Result<int>.Failure(exception);
        Assert.That(result.Error, Is.SameAs(exception));
    }

    /// <summary>
    ///     测试Exception属性在DefaultFailure状态时返回InvalidOperationException
    /// </summary>
    [Test]
    public void Exception_Should_Return_InvalidOperationException_When_DefaultFailure()
    {
        var result = default(Result<int>);
        Assert.That(result.Error, Is.TypeOf<InvalidOperationException>());
    }

    /// <summary>
    ///     测试Exception属性在成功时返回InvalidOperationException
    /// </summary>
    [Test]
    public void Exception_Should_Return_InvalidOperationException_When_Success()
    {
        var result = Result<int>.Success(42);
        Assert.Throws<InvalidOperationException>(() => _ = result.Error);
    }


    /// <summary>
    ///     测试Success方法创建成功结果
    /// </summary>
    [Test]
    public void Success_Should_Create_Successful_Result()
    {
        var result = Result<int>.Success(42);
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Match(succ: v => v, fail: _ => 0), Is.EqualTo(42));
    }


    /// <summary>
    ///     测试Failure方法使用异常创建失败结果
    /// </summary>
    [Test]
    public void Failure_WithException_Should_Create_Faulted_Result()
    {
        var exception = new Exception("Error");
        var result = Result<int>.Failure(exception);
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Is.SameAs(exception));
    }

    /// <summary>
    ///     测试Failure方法使用消息创建失败结果
    /// </summary>
    [Test]
    public void Failure_WithMessage_Should_Create_Faulted_Result()
    {
        var message = "Test error";
        var result = Result<int>.Failure(message);
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error.Message, Is.EqualTo(message));
    }

    /// <summary>
    ///     测试Try方法在函数成功时返回成功结果
    /// </summary>
    [Test]
    public void Try_Should_Return_Success_When_Function_Succeeds()
    {
        var result = Result<int>.Try(() => 42);
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Match(succ: v => v, fail: _ => 0), Is.EqualTo(42));
    }

    /// <summary>
    ///     测试Try方法在函数抛出异常时返回失败结果
    /// </summary>
    [Test]
    public void Try_Should_Return_Failure_When_Function_Throws()
    {
        var result = Result<int>.Try(() => throw new InvalidOperationException("Error"));
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Is.TypeOf<InvalidOperationException>());
    }

    /// <summary>
    ///     测试IfFail方法使用默认值在成功时返回值
    /// </summary>
    [Test]
    public void IfFail_WithDefaultValue_Should_Return_Value_When_Success()
    {
        var result = Result<int>.Success(42);
        var value = result.IfFail(0);
        Assert.That(value, Is.EqualTo(42));
    }

    /// <summary>
    ///     测试IfFail方法使用默认值在失败时返回默认值
    /// </summary>
    [Test]
    public void IfFail_WithDefaultValue_Should_Return_Default_When_Faulted()
    {
        var result = Result<int>.Failure(new Exception("Error"));
        var value = result.IfFail(99);
        Assert.That(value, Is.EqualTo(99));
    }

    /// <summary>
    ///     测试IfFail方法使用函数在成功时返回值
    /// </summary>
    [Test]
    public void IfFail_WithFunction_Should_Return_Value_When_Success()
    {
        var result = Result<int>.Success(42);
        var value = result.IfFail(_ => 0);
        Assert.That(value, Is.EqualTo(42));
    }

    /// <summary>
    ///     测试IfFail方法使用函数在失败时执行函数
    /// </summary>
    [Test]
    public void IfFail_WithFunction_Should_Execute_Function_When_Faulted()
    {
        var exception = new Exception("Error");
        var result = Result<int>.Failure(exception);
        var executed = false;
        var value = result.IfFail(ex =>
        {
            executed = true;
            return 99;
        });
        Assert.That(executed, Is.True);
        Assert.That(value, Is.EqualTo(99));
    }

    /// <summary>
    ///     测试IfFail方法使用Action在失败时执行操作
    /// </summary>
    [Test]
    public void IfFail_WithAction_Should_Execute_Action_When_Faulted()
    {
        var result = Result<int>.Failure(new Exception("Error"));
        var executed = false;
        result.IfFail(_ => executed = true);
        Assert.That(executed, Is.True);
    }

    /// <summary>
    ///     测试IfSucc方法在成功时执行操作
    /// </summary>
    [Test]
    public void IfSucc_Should_Execute_Action_When_Success()
    {
        var result = Result<int>.Success(42);
        var executed = false;
        result.IfSucc(_ => executed = true);
        Assert.That(executed, Is.True);
    }

    /// <summary>
    ///     测试IfSucc方法在失败时不执行操作
    /// </summary>
    [Test]
    public void IfSucc_Should_Not_Execute_Action_When_Faulted()
    {
        var result = Result<int>.Failure(new Exception("Error"));
        var executed = false;
        result.IfSucc(_ => executed = true);
        Assert.That(executed, Is.False);
    }

    /// <summary>
    ///     测试Map方法在成功时转换值
    /// </summary>
    [Test]
    public void Map_Should_Transform_Value_When_Success()
    {
        var result = Result<int>.Success(42);
        var mapped = result.Map(x => x.ToString(CultureInfo.InvariantCulture));
        Assert.That(mapped.IsSuccess, Is.True);
        Assert.That(mapped.Match(succ: v => v, fail: _ => ""), Is.EqualTo("42"));
    }

    /// <summary>
    ///     测试Map方法在失败时传播异常
    /// </summary>
    [Test]
    public void Map_Should_Propagate_Exception_When_Faulted()
    {
        var exception = new Exception("Error");
        var result = Result<int>.Failure(exception);
        var mapped = result.Map(x => x.ToString(CultureInfo.InvariantCulture));
        Assert.That(mapped.IsFailure, Is.True);
        Assert.That(mapped.Error, Is.SameAs(exception));
    }

    /// <summary>
    ///     测试Map方法处理null结果
    /// </summary>
    [Test]
    public void Map_Should_Handle_Null_Result_From_Mapper()
    {
        var result = Result<int>.Success(42);
        var mapped = result.Map<string>(_ => null!);
        Assert.That(mapped.IsSuccess, Is.True);
    }

    /// <summary>
    ///     测试Map方法在映射器返回null时不抛出异常
    /// </summary>
    [Test]
    public void Map_Should_Not_Throw_When_Mapper_Returns_Null()
    {
        var result = Result<string>.Success("test");
        Assert.DoesNotThrow(() => { _ = result.Map<string?>(_ => null); });
    }

    /// <summary>
    ///     测试Bind方法链接成功结果
    /// </summary>
    [Test]
    public void Bind_Should_Chain_Success_Results()
    {
        var result = Result<int>.Success(42);
        var bound = result.Bind(x => Result<string>.Success(x.ToString(CultureInfo.InvariantCulture)));
        Assert.That(bound.IsSuccess, Is.True);
        Assert.That(bound.Match(succ: v => v, fail: _ => ""), Is.EqualTo("42"));
    }

    /// <summary>
    ///     测试Bind方法传播第一个失败
    /// </summary>
    [Test]
    public void Bind_Should_Propagate_First_Failure()
    {
        var exception = new Exception("Error");
        var result = Result<int>.Failure(exception);
        var bound = result.Bind(x => Result<string>.Success(x.ToString(CultureInfo.InvariantCulture)));
        Assert.That(bound.IsFailure, Is.True);
        Assert.That(bound.Error, Is.SameAs(exception));
    }

    /// <summary>
    ///     测试Bind方法传播第二个失败
    /// </summary>
    [Test]
    public void Bind_Should_Propagate_Second_Failure()
    {
        var result = Result<int>.Success(42);
        var exception = new Exception("Bind error");
        var bound = result.Bind(x => Result<string>.Failure(exception));
        Assert.That(bound.IsFailure, Is.True);
        Assert.That(bound.Error, Is.SameAs(exception));
    }

    /// <summary>
    ///     测试Bind方法处理复杂链接
    /// </summary>
    [Test]
    public void Bind_Should_Handle_Complex_Chaining()
    {
        var result = Result<int>.Success(10)
            .Bind(x => Result<int>.Success(x * 2))
            .Bind(x => Result<int>.Success(x + 5));
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Match(succ: v => v, fail: _ => 0), Is.EqualTo(25));
    }

    /// <summary>
    ///     测试MapAsync方法在成功时异步转换值
    /// </summary>
    [Test]
    public async Task MapAsync_Should_Transform_Value_When_Success()
    {
        var result = Result<int>.Success(42);
        var mapped = await result.MapAsync(async x =>
        {
            await Task.Delay(1).ConfigureAwait(false);
            return x.ToString(CultureInfo.InvariantCulture);
        }).ConfigureAwait(false);
        Assert.That(mapped.IsSuccess, Is.True);
        Assert.That(mapped.Match(succ: v => v, fail: _ => ""), Is.EqualTo("42"));
    }

    /// <summary>
    ///     测试MapAsync方法在失败时传播异常
    /// </summary>
    [Test]
    public async Task MapAsync_Should_Propagate_Exception_When_Faulted()
    {
        var exception = new Exception("Error");
        var result = Result<int>.Failure(exception);
        var mapped = await result.MapAsync(async x =>
        {
            await Task.Delay(1).ConfigureAwait(false);
            return x.ToString(CultureInfo.InvariantCulture);
        }).ConfigureAwait(false);
        Assert.That(mapped.IsFailure, Is.True);
        Assert.That(mapped.Error, Is.SameAs(exception));
    }

    /// <summary>Async mapping propagates delegate exceptions just like synchronous mapping.</summary>
    [Test]
    public async Task MapAsync_Should_Propagate_Delegate_Exception()
    {
        var error = new InvalidOperationException("mapper failed");
        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => Result<int>.Success(1)
            .MapAsync<int>(_ => Task.FromException<int>(error)));
        Assert.That(actual, Is.SameAs(error));
        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <summary>
    ///     测试Match方法使用函数在成功时执行succ函数
    /// </summary>
    [Test]
    public void Match_WithFunctions_Should_Execute_Succ_When_Success()
    {
        var result = Result<int>.Success(42);
        var value = result.Match(succ: x => x * 2, fail: _ => 0);
        Assert.That(value, Is.EqualTo(84));
    }

    /// <summary>
    ///     测试Match方法使用函数在失败时执行fail函数
    /// </summary>
    [Test]
    public void Match_WithFunctions_Should_Execute_Fail_When_Faulted()
    {
        var result = Result<int>.Failure(new Exception("Error"));
        var value = result.Match(succ: x => x, fail: _ => 99);
        Assert.That(value, Is.EqualTo(99));
    }

    /// <summary>
    ///     测试Match方法使用Action在成功时执行succ操作
    /// </summary>
    [Test]
    public void Match_WithActions_Should_Execute_Succ_When_Success()
    {
        var result = Result<int>.Success(42);
        var executed = false;
        result.Match(succ: _ => executed = true, fail: _ => { });
        Assert.That(executed, Is.True);
    }

    /// <summary>
    ///     测试Match方法使用Action在失败时执行fail操作
    /// </summary>
    [Test]
    public void Match_WithActions_Should_Execute_Fail_When_Faulted()
    {
        var result = Result<int>.Failure(new Exception("Error"));
        var executed = false;
        result.Match(succ: _ => { }, fail: _ => executed = true);
        Assert.That(executed, Is.True);
    }

    /// <summary>
    ///     测试Match方法正确处理DefaultFailure状态
    /// </summary>
    [Test]
    public void Match_Should_Handle_DefaultFailure_State_Correctly()
    {
        var result = default(Result<int>);
        var value = result.Match(succ: x => x, fail: _ => 99);
        Assert.That(value, Is.EqualTo(99));
    }

    /// <summary>
    ///     测试Equals方法在两个成功值相同时返回true
    /// </summary>
    [Test]
    public void Equals_Should_Return_True_When_Both_Success_With_Same_Value()
    {
        var result1 = Result<int>.Success(42);
        var result2 = Result<int>.Success(42);
        Assert.That(result1.Equals(result2), Is.True);
    }

    /// <summary>
    ///     测试Equals方法在成功值不同时返回false
    /// </summary>
    [Test]
    public void Equals_Should_Return_False_When_Success_Values_Differ()
    {
        var result1 = Result<int>.Success(42);
        var result2 = Result<int>.Success(43);
        Assert.That(result1.Equals(result2), Is.False);
    }

    /// <summary>
    ///     测试Equals方法在两个失败且异常相同时返回true
    /// </summary>
    [Test]
    public void Equals_Should_Return_True_When_Both_Faulted_With_Same_Exception()
    {
        var result1 = Result<int>.Failure(new InvalidOperationException("Error"));
        var result2 = Result<int>.Failure(new InvalidOperationException("Error"));
        Assert.That(result1.Equals(result2), Is.True);
    }

    /// <summary>
    ///     测试Equals方法在异常类型不同时返回false
    /// </summary>
    [Test]
    public void Equals_Should_Return_False_When_Exception_Types_Differ()
    {
        var result1 = Result<int>.Failure(new InvalidOperationException("Error"));
        var result2 = Result<int>.Failure(new InvalidCastException("Error"));
        Assert.That(result1.Equals(result2), Is.False);
    }

    /// <summary>
    ///     测试Equals方法在两个都是DefaultFailure时返回true
    /// </summary>
    [Test]
    public void Equals_Should_Return_True_When_Both_DefaultFailure()
    {
        var result1 = default(Result<int>);
        var result2 = new Result<int>();
        Assert.That(result1.Equals(result2), Is.True);
    }

    /// <summary>
    ///     测试Equals方法在状态不同时返回false
    /// </summary>
    [Test]
    public void Equals_Should_Return_False_When_States_Differ()
    {
        var result1 = Result<int>.Success(42);
        var result2 = Result<int>.Failure(new Exception("Error"));
        Assert.That(result1.Equals(result2), Is.False);
    }

    /// <summary>
    ///     测试GetHashCode方法在相等结果时返回一致的哈希码
    /// </summary>
    [Test]
    public void GetHashCode_Should_Be_Consistent_For_Equal_Results()
    {
        var result1 = Result<int>.Success(42);
        var result2 = Result<int>.Success(42);
        Assert.That(result1.GetHashCode(), Is.EqualTo(result2.GetHashCode()));
    }

    /// <summary>
    ///     测试==操作符正确工作
    /// </summary>
    [Test]
    public void OperatorEquals_Should_Work_Correctly()
    {
        var result1 = Result<int>.Success(42);
        var result2 = Result<int>.Success(42);
        var result3 = Result<int>.Success(43);
        Assert.That(result1 == result2, Is.True);
        Assert.That(result1 == result3, Is.False);
    }

    /// <summary>
    ///     测试!=操作符正确工作
    /// </summary>
    [Test]
    public void OperatorNotEquals_Should_Work_Correctly()
    {
        var result1 = Result<int>.Success(42);
        var result2 = Result<int>.Success(43);
        Assert.That(result1 != result2, Is.True);
    }











    /// <summary>
    ///     测试ToString方法在成功时返回值字符串
    /// </summary>
    [Test]
    public void ToString_Should_Return_Value_String_When_Success()
    {
        var result = Result<int>.Success(42);
        Assert.That(result.ToString(), Is.EqualTo("Success(42)"));
    }

    /// <summary>
    ///     测试ToString方法在失败时返回失败消息
    /// </summary>
    [Test]
    public void ToString_Should_Return_Fail_Message_When_Faulted()
    {
        var result = Result<int>.Failure(new Exception("Test error"));
        Assert.That(result.ToString(), Is.EqualTo("Failure(Test error)"));
    }

    /// <summary>
    ///     测试ToString方法在DefaultFailure时返回DefaultFailure
    /// </summary>
    [Test]
    public void ToString_Should_Return_DefaultFailure_When_DefaultFailure()
    {
        var result = default(Result<int>);
        Assert.That(result.ToString(), Is.EqualTo("Failure(The result has not been initialized.)"));
    }

    /// <summary>
    ///     测试Success方法使用null值创建有效结果
    /// </summary>
    [Test]
    public void Success_WithNullValue_Should_Create_Valid_Result()
    {
        var result = Result<string?>.Success(value: null);
        Assert.That(result.IsSuccess, Is.True);
    }

    /// <summary>
    ///     测试Map方法正确处理null值
    /// </summary>
    [Test]
    public void Map_WithNullValue_Should_Handle_Correctly()
    {
        var result = Result<string?>.Success(value: null);
        var mapped = result.Map(x => x?.Length ?? 0);
        Assert.That(mapped.IsSuccess, Is.True);
        Assert.That(mapped.Match(succ: v => v, fail: _ => -1), Is.EqualTo(0));
    }

    /// <summary>
    ///     测试Equals方法null值正确工作
    /// </summary>
    [Test]
    public void Equals_WithNullValues_Should_Work_Correctly()
    {
        var result1 = Result<string?>.Success(value: null);
        var result2 = Result<string?>.Success(value: null);
        Assert.That(result1.Equals(result2), Is.True);
    }

    /// <summary>
    ///     测试隐式转换null创建成功的null值
    /// </summary>
    [Test]
    public void ImplicitConversion_WithNull_Should_Create_Success_With_Null()
    {
        Result<string?> result = null;
        Assert.That(result.IsSuccess, Is.True);
    }

    /// <summary>
    ///     测试DefaultFailure是只读和不可变的
    /// </summary>
    [Test]
    public void DefaultFailure_Should_Be_Readonly_And_Immutable()
    {
        var bottom1 = default(Result<int>);
        var bottom2 = default(Result<int>);
        Assert.That(bottom1.Equals(bottom2), Is.True);
    }
}
