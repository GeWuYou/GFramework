// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Functional;

/// <summary>Aggregates, validates and observes result values without hiding exceptions from application delegates.</summary>
public static class ResultExtensions
{
    /// <summary>Combines successful values in enumeration order and returns the first failure, including defaults.</summary>
    /// <typeparam name="T">The successful value type.</typeparam>
    /// <param name="results">The results to combine.</param>
    /// <returns>The accumulated values, or the first failure.</returns>
    public static Result<List<T>> Combine<T>(this IEnumerable<Result<T>> results)
    {
        ArgumentNullException.ThrowIfNull(results);
        var values = new List<T>();
        foreach (var result in results)
        {
            if (result.IsFailure)
                return Result<List<T>>.Failure(result.Error);
            values.Add(result.Match(static value => value, static error => throw error));
        }
        return Result<List<T>>.Success(values);
    }

    /// <summary>Observes a success and returns the same result for composition.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result being observed.</param>
    /// <param name="action">The successful observer, whose exceptions propagate.</param>
    /// <returns>The original result.</returns>
    public static Result<T> OnSuccess<T>(this Result<T> result, Action<T> action)
    {
        result.IfSucc(action);
        return result;
    }

    /// <summary>Observes a failure, including a default result, and returns the same result.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result being observed.</param>
    /// <param name="action">The failed observer, whose exceptions propagate.</param>
    /// <returns>The original result.</returns>
    public static Result<T> OnFailure<T>(this Result<T> result, Action<Exception> action)
    {
        result.IfFail(action);
        return result;
    }

    /// <summary>Checks a successful value without changing an existing failure.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to validate.</param>
    /// <param name="predicate">The condition for a valid value.</param>
    /// <param name="errorMessage">The nonblank failure description.</param>
    /// <returns>The original value or a failed validation.</returns>
    public static Result<T> Ensure<T>(this Result<T> result, Func<T, bool> predicate, string errorMessage)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);
        return result.Ensure(predicate, _ => new ArgumentException(errorMessage, nameof(result)));
    }

    /// <summary>Checks a successful value; predicate and error-factory exceptions propagate.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to validate.</param>
    /// <param name="predicate">The condition for a valid value.</param>
    /// <param name="exceptionFactory">The failure cause for an invalid value.</param>
    /// <returns>The original value or a failed validation.</returns>
    public static Result<T> Ensure<T>(this Result<T> result, Func<T, bool> predicate, Func<T, Exception> exceptionFactory)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(exceptionFactory);
        return result.IsFailure ? result : result.Bind(value => predicate(value)
            ? result
            : Result<T>.Failure(exceptionFactory(value)));
    }

    /// <summary>Converts a successful value type to a nullable value and a failure to null.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <returns>The successful value, or null.</returns>
    public static T? ToNullable<T>(this Result<T> result) where T : struct =>
        result.Match(static value => (T?)value, static _ => null);

    /// <summary>Converts a nonnull reference to success and null to an explicit failure.</summary>
    /// <typeparam name="T">The reference type.</typeparam>
    /// <param name="value">The potentially absent reference.</param>
    /// <param name="errorMessage">The absence description.</param>
    /// <returns>A successful reference or failure.</returns>
    public static Result<T> ToResult<T>(this T? value, string errorMessage = "Value is null") where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);
        return value != null ? Result<T>.Success(value) : Result<T>.Failure(new ArgumentNullException(nameof(value), errorMessage));
    }

    /// <summary>Converts a present nullable value to success and null to an explicit failure.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The potentially absent value.</param>
    /// <param name="errorMessage">The absence description.</param>
    /// <returns>A successful value or failure.</returns>
    public static Result<T> ToResult<T>(this T? value, string errorMessage = "Value is null") where T : struct
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);
        return value.HasValue ? Result<T>.Success(value.Value) : Result<T>.Failure(new ArgumentNullException(nameof(value), errorMessage));
    }
}
