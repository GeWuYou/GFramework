// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Functional;

/// <summary>Represents success without a value, or a failure carrying an exception.</summary>
/// <remarks>
///     The default value is a failure with a shared uninitialized-result error. Mapping and binding propagate
///     delegate exceptions; only explicit Try methods capture ordinary exceptions. Cancellation is never captured.
/// </remarks>
public readonly struct Result : IEquatable<Result>
{
    private readonly Exception? _error;

    /// <summary>Creates a result through the explicit success or failure factories.</summary>
    private Result(bool isSuccess, Exception? error)
    {
        IsSuccess = isSuccess;
        _error = error;
    }

    /// <summary>Gets whether the operation succeeded.</summary>
    public bool IsSuccess
    {
        get;
    }

    /// <summary>Gets whether the operation failed, including an uninitialized default value.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>Gets the failure cause; access on a successful result throws.</summary>
    /// <exception cref="InvalidOperationException">The result is successful.</exception>
    public Exception Error => IsFailure ? _error ?? ResultError.Uninitialized :
        throw new InvalidOperationException("A successful result has no error.");

    /// <summary>Constructs an explicit successful result.</summary>
    /// <returns>A successful result.</returns>
    public static Result Success() => new(isSuccess: true, error: null);

    /// <summary>Constructs a failure without losing its exception identity.</summary>
    /// <param name="error">The nonnull failure cause.</param>
    /// <returns>A failed result.</returns>
    public static Result Failure(Exception error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result(isSuccess: false, error);
    }

    /// <summary>Constructs a failure describing an invalid operation.</summary>
    /// <param name="message">The nonblank failure description.</param>
    /// <returns>A failed result.</returns>
    public static Result Failure(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return Failure(new InvalidOperationException(message));
    }

    /// <summary>Chooses the successful or failed branch; branch exceptions propagate to the caller.</summary>
    /// <typeparam name="TResult">The branch return type.</typeparam>
    /// <param name="onSuccess">The successful branch.</param>
    /// <param name="onFailure">The failure branch.</param>
    /// <returns>The selected branch result.</returns>
    public TResult Match<TResult>(Func<TResult> onSuccess, Func<Exception, TResult> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return IsSuccess ? onSuccess() : onFailure(Error);
    }

    /// <summary>Adds a value to a success while preserving an existing failure.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The successful value, which may be null if T permits it.</param>
    /// <returns>A typed result.</returns>
    public Result<T> ToResult<T>(T value) => IsSuccess ? Result<T>.Success(value) : Result<T>.Failure(Error);

    /// <summary>Maps success to a value; the delegate is not invoked for failure.</summary>
    /// <typeparam name="T">The mapped value type.</typeparam>
    /// <param name="mapper">The successful transformation.</param>
    /// <returns>The mapped result or the original failure.</returns>
    public Result<T> Map<T>(Func<T> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        return IsSuccess ? Result<T>.Success(mapper()) : Result<T>.Failure(Error);
    }

    /// <summary>Chains a result-returning operation without capturing delegate exceptions.</summary>
    /// <typeparam name="T">The chained value type.</typeparam>
    /// <param name="binder">The successful continuation.</param>
    /// <returns>The continuation result or the original failure.</returns>
    public Result<T> Bind<T>(Func<Result<T>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);
        return IsSuccess ? binder() : Result<T>.Failure(Error);
    }

    /// <summary>Explicitly captures ordinary action exceptions; cancellation continues as cancellation.</summary>
    /// <param name="action">The operation to execute.</param>
    /// <returns>Success or the captured failure.</returns>
    public static Result Try(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        try
        {
            action();
            return Success();
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            return Failure(error);
        }
    }

    /// <summary>Explicitly captures an asynchronous operation's ordinary exceptions while preserving cancellation.</summary>
    /// <param name="action">The operation to execute.</param>
    /// <returns>Success or the captured failure.</returns>
    public static async Task<Result> TryAsync(Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        try
        {
            await action().ConfigureAwait(false);
            return Success();
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            return Failure(error);
        }
    }

    /// <summary>Compares successful status, or failure type and ordinal message.</summary>
    /// <param name="other">The other result.</param>
    /// <returns>Whether both results have the same value semantics.</returns>
    public bool Equals(Result other) => IsSuccess == other.IsSuccess &&
        (IsSuccess || ResultError.Equals(Error, other.Error));

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Result other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => IsSuccess ? 1 : ResultError.GetHashCode(Error);

    /// <summary>Compares result values for equality.</summary>
    public static bool operator ==(Result left, Result right) => left.Equals(right);

    /// <summary>Compares result values for inequality.</summary>
    public static bool operator !=(Result left, Result right) => !left.Equals(right);

    /// <inheritdoc />
    public override string ToString() => IsSuccess ? "Success" : $"Failure({Error.Message})";
}
