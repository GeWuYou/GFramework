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

public static class ResultExtensions
{
    public static Result<TResult, TError> Map<TSuccess, TResult, TError>(
        this Result<TSuccess, TError> result,
        Func<TSuccess, TResult> mapper)
    {
        return result.IsSuccess
            ? Result<TResult, TError>.Success(mapper(result.SuccessValue))
            : Result<TResult, TError>.Failure(result.ErrorValue);
    }

    public static Result<TResult, TError> Bind<TSuccess, TResult, TError>(
        this Result<TSuccess, TError> result,
        Func<TSuccess, Result<TResult, TError>> binder)
    {
        return result.IsSuccess
            ? binder(result.SuccessValue)
            : Result<TResult, TError>.Failure(result.ErrorValue);
    }

    public static Result<TSuccess, TNewError> MapError<TSuccess, TError, TNewError>(
        this Result<TSuccess, TError> result,
        Func<TError, TNewError> mapper)
    {
        return result.IsFailure
            ? Result<TSuccess, TNewError>.Failure(mapper(result.ErrorValue))
            : Result<TSuccess, TNewError>.Success(result.SuccessValue);
    }

    public static TResult Match<TSuccess, TError, TResult>(
        this Result<TSuccess, TError> result,
        Func<TSuccess, TResult> onSuccess,
        Func<TError, TResult> onFailure)
    {
        return result.IsSuccess
            ? onSuccess(result.SuccessValue)
            : onFailure(result.ErrorValue);
    }
}