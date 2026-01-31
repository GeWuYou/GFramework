// Copyright (c) 2026 GeWuYou
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

namespace GFramework.Core.functional.types;

/// <summary>
/// 表示一个可能成功也可能失败的计算结果
/// </summary>
/// <typeparam name="TSuccess">成功值的类型</typeparam>
/// <typeparam name="TError">错误值的类型</typeparam>
public readonly struct Result<TSuccess, TError>
{
    private readonly TSuccess _success;
    private readonly TError _error;

    /// <summary>
    /// 获取当前结果是否为成功状态
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// 获取当前结果是否为失败状态
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// 使用成功值初始化Result实例
    /// </summary>
    /// <param name="success">成功值</param>
    private Result(TSuccess success)
    {
        _success = success;
        _error = default!;
        IsSuccess = true;
    }

    /// <summary>
    /// 使用错误值初始化Result实例
    /// </summary>
    /// <param name="error">错误值</param>
    private Result(TError error)
    {
        _error = error;
        _success = default!;
        IsSuccess = false;
    }

    /// <summary>
    /// 创建一个表示成功的Result实例
    /// </summary>
    /// <param name="value">成功值</param>
    /// <returns>包含成功值的Result实例</returns>
    public static Result<TSuccess, TError> Success(TSuccess value)
        => new(value);

    /// <summary>
    /// 创建一个表示失败的Result实例
    /// </summary>
    /// <param name="error">错误值</param>
    /// <returns>包含错误值的Result实例</returns>
    public static Result<TSuccess, TError> Failure(TError error)
        => new(error);

    /// <summary>
    /// 获取成功值，如果结果为失败则抛出异常
    /// </summary>
    /// <exception cref="InvalidOperationException">当结果为失败时抛出</exception>
    public TSuccess SuccessValue =>
        IsSuccess
            ? _success
            : throw new InvalidOperationException("Result is Failure");

    /// <summary>
    /// 获取错误值，如果结果为成功则抛出异常
    /// </summary>
    /// <exception cref="InvalidOperationException">当结果为成功时抛出</exception>
    public TError ErrorValue =>
        IsFailure
            ? _error
            : throw new InvalidOperationException("Result is Success");
}