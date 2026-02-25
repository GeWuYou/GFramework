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

using System.Diagnostics.Contracts;

namespace GFramework.Core.Functional;

/// <summary>
///     表示一个操作的结果，可能是成功值或异常
/// </summary>
public readonly struct Result<A> : IEquatable<Result<A>>, IComparable<Result<A>>
{
    private readonly A? _value;
    private readonly Exception? _exception;

    // ------------------------------------------------------------------ 状态枚举

    /// <summary>
    ///     结果状态枚举，表示结果的不同状态
    ///     排序: Bottom < Faulted < Success
    /// </summary>
    private enum ResultState : byte
    {
        Bottom,
        Faulted,
        Success
    }

    private readonly ResultState _state;

    // ------------------------------------------------------------------ 静态默认值

    /// <summary>
    ///     表示未初始化的 Bottom 状态
    /// </summary>
    public static readonly Result<A> Bottom = default;

    // ------------------------------------------------------------------ 构造器

    /// <summary>
    ///     构造成功结果
    /// </summary>
    /// <param name="value">成功的值</param>
    public Result(A value)
    {
        _state = ResultState.Success;
        _value = value;
        _exception = null;
    }

    /// <summary>
    ///     构造失败结果
    /// </summary>
    /// <param name="exception">失败的异常</param>
    public Result(Exception exception)
    {
        _state = ResultState.Faulted;
        _exception = exception ?? throw new ArgumentNullException(nameof(exception));
        _value = default;
    }

    // ------------------------------------------------------------------ 隐式转换

    /// <summary>
    ///     隐式将值转换为成功结果
    /// </summary>
    /// <param name="value">要转换的值</param>
    /// <returns>成功结果</returns>
    [Pure]
    public static implicit operator Result<A>(A value) => new(value);

    // ------------------------------------------------------------------ 状态属性

    /// <summary>
    ///     判断结果是否为成功状态
    /// </summary>
    [Pure]
    public bool IsSuccess => _state == ResultState.Success;

    /// <summary>
    ///     判断结果是否为失败状态
    /// </summary>
    [Pure]
    public bool IsFaulted => _state == ResultState.Faulted;


    /// <summary>
    ///     判断结果是否为未初始化的 Bottom 状态
    /// </summary>
    [Pure]
    public bool IsBottom => _state == ResultState.Bottom;

    /// <summary>
    ///     获取内部异常：
    ///     - 若为 Failure 状态，则返回内部异常
    ///     - 若为 Bottom 状态，则返回带有 "Result is in Bottom state." 消息的 InvalidOperationException
    ///     - 若为 Success 状态，则返回带有 "Cannot access Exception on a successful Result." 消息的 InvalidOperationException
    /// </summary>
    [Pure]
    public Exception Exception => _exception
                                  ?? (IsBottom
                                      ? new InvalidOperationException("Result is in Bottom state.")
                                      : new InvalidOperationException(
                                          "Cannot access Exception on a successful Result."));

    // ------------------------------------------------------------------ 取值

    /// <summary>
    ///     若成功则返回值，若失败则返回默认值
    /// </summary>
    /// <param name="defaultValue">失败时返回的默认值</param>
    /// <returns>成功时的值或默认值</returns>
    [Pure]
    public A IfFail(A defaultValue) => IsSuccess ? _value! : defaultValue;

    /// <summary>
    ///     若成功则返回值，若失败则通过委托处理异常
    /// </summary>
    /// <param name="f">处理异常的委托</param>
    /// <returns>成功时的值或委托处理后的结果</returns>
    [Pure]
    public A IfFail(Func<Exception, A> f) => IsSuccess ? _value! : f(Exception);

    /// <summary>
    ///     若失败则执行副作用
    /// </summary>
    /// <param name="f">处理异常的副作用委托</param>
    public void IfFail(Action<Exception> f)
    {
        if (IsFaulted) f(Exception);
    }

    /// <summary>
    ///     若成功则执行副作用
    /// </summary>
    /// <param name="f">处理成功值的副作用委托</param>
    public void IfSucc(Action<A> f)
    {
        if (IsSuccess) f(_value!);
    }

    // ------------------------------------------------------------------ 变换

    /// <summary>
    ///     成功时映射值，失败时透传异常
    /// </summary>
    /// <typeparam name="B">映射后的类型</typeparam>
    /// <param name="f">映射函数</param>
    /// <returns>映射后的结果</returns>
    [Pure]
    public Result<B> Map<B>(Func<A, B> f)
    {
        ArgumentNullException.ThrowIfNull(f);
        return IsSuccess ? new Result<B>(f(_value!)) : new Result<B>(Exception);
    }

    /// <summary>
    ///     成功时绑定到新 Result，失败时透传异常
    /// </summary>
    /// <typeparam name="B">绑定后的类型</typeparam>
    /// <param name="binder">绑定函数</param>
    /// <returns>绑定后的结果</returns>
    [Pure]
    public Result<B> Bind<B>(Func<A, Result<B>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);
        return IsSuccess ? binder(_value!) : new Result<B>(Exception);
    }

    /// <summary>
    ///     异步映射
    /// </summary>
    /// <typeparam name="B">映射后的类型</typeparam>
    /// <param name="f">异步映射函数</param>
    /// <returns>异步映射后的结果</returns>
    [Pure]
    public async Task<Result<B>> MapAsync<B>(Func<A, Task<B>> f)
    {
        ArgumentNullException.ThrowIfNull(f);
        if (!IsSuccess) return new Result<B>(Exception);

        try
        {
            return new Result<B>(await f(_value!));
        }
        catch (Exception ex)
        {
            return new Result<B>(ex);
        }
    }

    // ------------------------------------------------------------------ 模式匹配

    /// <summary>
    ///     对成功/失败两种情况分别处理并返回值
    /// </summary>
    /// <typeparam name="R">返回值类型</typeparam>
    /// <param name="succ">处理成功值的函数</param>
    /// <param name="fail">处理异常的函数</param>
    /// <returns>处理后的结果</returns>
    [Pure]
    public R Match<R>(Func<A, R> succ, Func<Exception, R> fail) =>
        IsSuccess ? succ(_value!) : fail(Exception);

    /// <summary>
    ///     对成功/失败两种情况分别执行副作用
    /// </summary>
    /// <param name="succ">处理成功值的副作用委托</param>
    /// <param name="fail">处理异常的副作用委托</param>
    public void Match(Action<A> succ, Action<Exception> fail)
    {
        if (IsSuccess) succ(_value!);
        else fail(Exception);
    }

    // ------------------------------------------------------------------ 静态工厂（语义更清晰）

    /// <summary>
    ///     创建成功结果
    /// </summary>
    /// <param name="value">成功的值</param>
    /// <returns>成功结果</returns>
    [Pure]
    public static Result<A> Succeed(A value) => new(value);

    /// <summary>
    ///     创建成功结果（别名）
    /// </summary>
    /// <param name="value">成功的值</param>
    /// <returns>成功结果</returns>
    [Pure]
    public static Result<A> Success(A value) => new(value);

    /// <summary>
    ///     创建失败结果
    /// </summary>
    /// <param name="ex">失败的异常</param>
    /// <returns>失败结果</returns>
    [Pure]
    public static Result<A> Fail(Exception ex) => new(ex);

    /// <summary>
    ///     创建失败结果（别名）
    /// </summary>
    /// <param name="ex">失败的异常</param>
    /// <returns>失败结果</returns>
    [Pure]
    public static Result<A> Failure(Exception ex) => new(ex);

    /// <summary>
    ///     根据错误消息创建失败结果
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <returns>失败结果</returns>
    [Pure]
    public static Result<A> Failure(string message) => new(new Exception(message));

    /// <summary>
    ///     安全执行委托，自动捕获异常
    /// </summary>
    /// <param name="f">要执行的委托</param>
    /// <returns>执行结果</returns>
    public static Result<A> Try(Func<A> f)
    {
        ArgumentNullException.ThrowIfNull(f);
        try
        {
            return new Result<A>(f());
        }
        catch (Exception ex)
        {
            return new Result<A>(ex);
        }
    }

    // ------------------------------------------------------------------ 相等 / 比较

    /// <summary>
    ///     判断两个结果是否相等
    /// </summary>
    /// <param name="other">另一个结果</param>
    /// <returns>若相等返回 true，否则返回 false</returns>
    [Pure]
    public bool Equals(Result<A> other)
    {
        if (_state != other._state) return false;
        if (IsSuccess) return EqualityComparer<A>.Default.Equals(_value, other._value);
        if (IsFaulted)
            return Exception.GetType() == other.Exception.GetType()
                   && Exception.Message == other.Exception.Message;
        return true; // both Bottom
    }

    /// <summary>
    ///     判断对象是否与当前结果相等
    /// </summary>
    /// <param name="obj">要比较的对象</param>
    /// <returns>若相等返回 true，否则返回 false</returns>
    [Pure]
    public override bool Equals(object? obj) => obj is Result<A> other && Equals(other);

    /// <summary>
    ///     获取结果的哈希码
    /// </summary>
    /// <returns>哈希码</returns>
    [Pure]
    public override int GetHashCode() => IsSuccess
        ? HashCode.Combine(0, _value)
        : HashCode.Combine(1, Exception.GetType(), Exception.Message);

    /// <summary>
    ///     比较两个结果的大小
    /// </summary>
    /// <param name="other">另一个结果</param>
    /// <returns>比较结果</returns>
    [Pure]
    public int CompareTo(Result<A> other)
    {
        // Bottom < Faulted < Success
        if (_state != other._state) return _state.CompareTo(other._state);
        if (!IsSuccess) return 0;

        try
        {
            return Comparer<A>.Default.Compare(_value, other._value);
        }
        catch (ArgumentException)
        {
            // 类型不可比较时返回 0
            return 0;
        }
    }

    /// <summary>
    ///     判断两个结果是否相等
    /// </summary>
    /// <param name="a">第一个结果</param>
    /// <param name="b">第二个结果</param>
    /// <returns>若相等返回 true，否则返回 false</returns>
    [Pure]
    public static bool operator ==(Result<A> a, Result<A> b) => a.Equals(b);

    /// <summary>
    ///     判断两个结果是否不相等
    /// </summary>
    /// <param name="a">第一个结果</param>
    /// <param name="b">第二个结果</param>
    /// <returns>若不相等返回 true，否则返回 false</returns>
    [Pure]
    public static bool operator !=(Result<A> a, Result<A> b) => !a.Equals(b);

    /// <summary>
    ///     判断第一个结果是否小于第二个结果
    /// </summary>
    /// <param name="a">第一个结果</param>
    /// <param name="b">第二个结果</param>
    /// <returns>若小于返回 true，否则返回 false</returns>
    [Pure]
    public static bool operator <(Result<A> a, Result<A> b) => a.CompareTo(b) < 0;

    /// <summary>
    ///     判断第一个结果是否小于等于第二个结果
    /// </summary>
    /// <param name="a">第一个结果</param>
    /// <param name="b">第二个结果</param>
    /// <returns>若小于等于返回 true，否则返回 false</returns>
    [Pure]
    public static bool operator <=(Result<A> a, Result<A> b) => a.CompareTo(b) <= 0;

    /// <summary>
    ///     判断第一个结果是否大于第二个结果
    /// </summary>
    /// <param name="a">第一个结果</param>
    /// <param name="b">第二个结果</param>
    /// <returns>若大于返回 true，否则返回 false</returns>
    [Pure]
    public static bool operator >(Result<A> a, Result<A> b) => a.CompareTo(b) > 0;

    /// <summary>
    ///     判断第一个结果是否大于等于第二个结果
    /// </summary>
    /// <param name="a">第一个结果</param>
    /// <param name="b">第二个结果</param>
    /// <returns>若大于等于返回 true，否则返回 false</returns>
    [Pure]
    public static bool operator >=(Result<A> a, Result<A> b) => a.CompareTo(b) >= 0;

    // ------------------------------------------------------------------ 调试

    /// <summary>
    ///     返回结果的字符串表示
    /// </summary>
    /// <returns>结果的字符串表示</returns>
    [Pure]
    public override string ToString() => _state switch
    {
        ResultState.Success => _value?.ToString() ?? "(null)",
        ResultState.Faulted => $"Fail({Exception.Message})",
        _ => "(Bottom)"
    };
}