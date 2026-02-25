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
///     表示一个无值的操作结果，仅包含成功或失败状态
/// </summary>
public readonly struct Result : IEquatable<Result>
{
    private readonly Exception? _exception;
    private readonly bool _isSuccess;

    /// <summary>
    ///     私有构造函数，用于创建 Result 实例
    /// </summary>
    /// <param name="isSuccess">是否为成功状态</param>
    /// <param name="exception">失败时的异常信息</param>
    private Result(bool isSuccess, Exception? exception)
    {
        _isSuccess = isSuccess;
        _exception = exception;
    }

    /// <summary>
    ///     判断结果是否为成功状态
    /// </summary>
    [Pure]
    public bool IsSuccess => _isSuccess;

    /// <summary>
    ///     判断结果是否为失败状态
    /// </summary>
    [Pure]
    public bool IsFailure => !_isSuccess;

    /// <summary>
    ///     获取失败时的异常信息，若为成功状态则抛出 InvalidOperationException
    /// </summary>
    [Pure]
    public Exception Error => IsFailure
        ? _exception!
        : throw new InvalidOperationException("Cannot access Error on a successful Result.");

    /// <summary>
    ///     创建成功结果
    /// </summary>
    /// <returns>成功结果</returns>
    [Pure]
    public static Result Success() => new(true, null);

    /// <summary>
    ///     创建失败结果
    /// </summary>
    /// <param name="ex">失败的异常</param>
    /// <returns>失败结果</returns>
    [Pure]
    public static Result Failure(Exception ex)
    {
        ArgumentNullException.ThrowIfNull(ex);
        return new(false, ex);
    }

    /// <summary>
    ///     根据错误消息创建失败结果
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <returns>失败结果</returns>
    [Pure]
    public static Result Failure(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return new(false, new Exception(message));
    }

    /// <summary>
    ///     根据成功或失败状态分别执行不同的处理逻辑
    /// </summary>
    /// <typeparam name="R">返回值类型</typeparam>
    /// <param name="onSuccess">成功时执行的函数</param>
    /// <param name="onFailure">失败时执行的函数</param>
    /// <returns>处理后的结果</returns>
    public R Match<R>(Func<R> onSuccess, Func<Exception, R> onFailure) =>
        IsSuccess ? onSuccess() : onFailure(_exception!);

    /// <summary>
    ///     将当前无值的 Result 提升为带值的 Result
    /// </summary>
    /// <typeparam name="A">值的类型</typeparam>
    /// <param name="value">成功时关联的值</param>
    /// <returns>带值的 Result</returns>
    [Pure]
    public Result<A> ToResult<A>(A value) =>
        IsSuccess ? Result<A>.Success(value) : Result<A>.Failure(_exception!);

    /// <summary>
    ///     判断当前 Result 是否与另一个 Result 相等
    /// </summary>
    /// <param name="other">另一个 Result</param>
    /// <returns>若相等返回 true，否则返回 false</returns>
    [Pure]
    public bool Equals(Result other)
    {
        // 比较状态和异常信息
        return _isSuccess == other._isSuccess &&
               (!IsFailure || (_exception?.GetType() == other._exception?.GetType() &&
                               _exception?.Message == other._exception?.Message));
    }

    /// <summary>
    ///     判断当前对象是否与另一个对象相等
    /// </summary>
    /// <param name="obj">另一个对象</param>
    /// <returns>若相等返回 true，否则返回 false</returns>
    [Pure]
    public override bool Equals(object? obj) => obj is Result other && Equals(other);

    /// <summary>
    ///     获取当前 Result 的哈希码
    /// </summary>
    /// <returns>哈希码</returns>
    [Pure]
    public override int GetHashCode()
    {
        // 根据状态和异常信息生成哈希码
        return IsSuccess
            ? HashCode.Combine(true)
            : HashCode.Combine(false, _exception?.GetType(), _exception?.Message);
    }

    /// <summary>
    ///     判断两个 Result 是否相等
    /// </summary>
    /// <param name="a">第一个 Result</param>
    /// <param name="b">第二个 Result</param>
    /// <returns>若相等返回 true，否则返回 false</returns>
    [Pure]
    public static bool operator ==(Result a, Result b) => a.Equals(b);

    /// <summary>
    ///     判断两个 Result 是否不相等
    /// </summary>
    /// <param name="a">第一个 Result</param>
    /// <param name="b">第二个 Result</param>
    /// <returns>若不相等返回 true，否则返回 false</returns>
    [Pure]
    public static bool operator !=(Result a, Result b) => !a.Equals(b);

    /// <summary>
    ///     返回当前 Result 的字符串表示
    /// </summary>
    /// <returns>Result 的字符串表示</returns>
    [Pure]
    public override string ToString() =>
        IsSuccess ? "Success" : $"Fail({_exception?.Message ?? "Unknown"})";
}