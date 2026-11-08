// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Functional;

/// <summary>Represents an explicit successful value or a failure carrying its original exception.</summary>
/// <typeparam name="T">The successful value type, including nullable values when explicitly permitted.</typeparam>
/// <remarks>
///     Default is a failure, never an additional Bottom state. Map and Bind propagate delegate exceptions;
///     explicit Try/TryAsync capture ordinary exceptions while cancellation propagates. Failures compare by
///     exception type and ordinal message, without imposing an arbitrary order on results.
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0018",
    Justification = "Typed success and failure factories construct the closed result value and form its explicit value-type API.")]
public readonly struct Result<T> : IEquatable<Result<T>>
{
    private readonly T? _value;
    private readonly Exception? _error;

    /// <summary>Creates a value through unambiguous explicit factories, including when T is Exception.</summary>
    private Result(bool isSuccess, T? value, Exception? error)
    {
        IsSuccess = isSuccess;
        _value = value;
        _error = error;
    }

    /// <summary>Gets whether a successful value is present.</summary>
    public bool IsSuccess
    {
        get;
    }

    /// <summary>Gets whether the result is failed, including an uninitialized default value.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>Gets the failure cause; successful results reject error access.</summary>
    /// <exception cref="InvalidOperationException">The result is successful.</exception>
    public Exception Error => IsFailure ? _error ?? ResultError.Uninitialized :
        throw new InvalidOperationException("A successful result has no error.");

    /// <summary>Constructs a success; the value may be null when T permits it.</summary>
    /// <param name="value">The successful value.</param>
    /// <returns>A successful result.</returns>
    public static Result<T> Success(T value) => new(isSuccess: true, value, error: null);

    /// <summary>Constructs a failure while preserving exception identity.</summary>
    /// <param name="error">The nonnull failure cause.</param>
    /// <returns>A failed result.</returns>
    public static Result<T> Failure(Exception error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result<T>(isSuccess: false, value: default, error);
    }

    /// <summary>Constructs a failure describing an invalid operation.</summary>
    /// <param name="message">The nonblank failure description.</param>
    /// <returns>A failed result.</returns>
    public static Result<T> Failure(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return Failure(new InvalidOperationException(message));
    }

    /// <summary>Converts a value to a success, including a value whose type is Exception.</summary>
    public static implicit operator Result<T>(T value) => Success(value);

    /// <summary>Returns the successful value or an explicit fallback.</summary>
    /// <param name="defaultValue">The fallback value.</param>
    /// <returns>The successful value or fallback.</returns>
    public T IfFail(T defaultValue) => IsSuccess ? _value! : defaultValue;

    /// <summary>Returns the successful value or derives a fallback from its failure.</summary>
    /// <param name="fallback">The failure fallback.</param>
    /// <returns>The successful value or fallback.</returns>
    public T IfFail(Func<Exception, T> fallback)
    {
        ArgumentNullException.ThrowIfNull(fallback);
        return IsSuccess ? _value! : fallback(Error);
    }

    /// <summary>Observes any failure, including a default result.</summary>
    /// <param name="action">The observer, whose exceptions propagate.</param>
    public void IfFail(Action<Exception> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (IsFailure)
            action(Error);
    }

    /// <summary>Observes a successful value.</summary>
    /// <param name="action">The observer, whose exceptions propagate.</param>
    public void IfSucc(Action<T> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (IsSuccess)
            action(_value!);
    }

    /// <summary>Maps the successful value without capturing mapper exceptions.</summary>
    /// <typeparam name="TResult">The mapped value type.</typeparam>
    /// <param name="mapper">The successful transformation.</param>
    /// <returns>The mapped success or original failure.</returns>
    public Result<TResult> Map<TResult>(Func<T, TResult> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        return IsSuccess ? Result<TResult>.Success(mapper(_value!)) : Result<TResult>.Failure(Error);
    }

    /// <summary>Chains a result-returning continuation without capturing its exceptions.</summary>
    /// <typeparam name="TResult">The chained value type.</typeparam>
    /// <param name="binder">The successful continuation.</param>
    /// <returns>The continuation result or original failure.</returns>
    public Result<TResult> Bind<TResult>(Func<T, Result<TResult>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);
        return IsSuccess ? binder(_value!) : Result<TResult>.Failure(Error);
    }

    /// <summary>Maps asynchronously with the same exception propagation as synchronous Map.</summary>
    /// <typeparam name="TResult">The mapped value type.</typeparam>
    /// <param name="mapper">The asynchronous transformation.</param>
    /// <returns>The mapped success or original failure.</returns>
    public async Task<Result<TResult>> MapAsync<TResult>(Func<T, Task<TResult>> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        return IsSuccess
            ? Result<TResult>.Success(await mapper(_value!).ConfigureAwait(false))
            : Result<TResult>.Failure(Error);
    }

    /// <summary>Chains asynchronously with the same exception propagation as synchronous Bind.</summary>
    /// <typeparam name="TResult">The chained value type.</typeparam>
    /// <param name="binder">The asynchronous continuation.</param>
    /// <returns>The continuation result or original failure.</returns>
    public async Task<Result<TResult>> BindAsync<TResult>(Func<T, Task<Result<TResult>>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);
        return IsSuccess ? await binder(_value!).ConfigureAwait(false) : Result<TResult>.Failure(Error);
    }

    /// <summary>Chooses one branch, with default results always selecting failure.</summary>
    /// <typeparam name="TResult">The branch return type.</typeparam>
    /// <param name="succ">The successful branch.</param>
    /// <param name="fail">The failed branch.</param>
    /// <returns>The selected branch result.</returns>
    public TResult Match<TResult>(Func<T, TResult> succ, Func<Exception, TResult> fail)
    {
        ArgumentNullException.ThrowIfNull(succ);
        ArgumentNullException.ThrowIfNull(fail);
        return IsSuccess ? succ(_value!) : fail(Error);
    }

    /// <summary>Executes one branch; delegate exceptions propagate.</summary>
    /// <param name="succ">The successful branch.</param>
    /// <param name="fail">The failed branch.</param>
    public void Match(Action<T> succ, Action<Exception> fail)
    {
        ArgumentNullException.ThrowIfNull(succ);
        ArgumentNullException.ThrowIfNull(fail);
        if (IsSuccess)
            succ(_value!);
        else
            fail(Error);
    }

    /// <summary>Explicitly captures ordinary function exceptions; cancellation propagates.</summary>
    /// <param name="function">The operation to execute.</param>
    /// <returns>Its successful value or captured failure.</returns>
    public static Result<T> Try(Func<T> function)
    {
        ArgumentNullException.ThrowIfNull(function);
        try
        {
            return Success(function());
        }
        catch (Exception error) when (error is not OperationCanceledException) { return Failure(error); }
    }

    /// <summary>Explicitly captures ordinary asynchronous function exceptions; cancellation propagates.</summary>
    /// <param name="function">The operation to execute.</param>
    /// <returns>Its successful value or captured failure.</returns>
    public static async Task<Result<T>> TryAsync(Func<Task<T>> function)
    {
        ArgumentNullException.ThrowIfNull(function);
        try
        {
            return Success(await function().ConfigureAwait(false));
        }
        catch (Exception error) when (error is not OperationCanceledException) { return Failure(error); }
    }

    /// <summary>Compares successful values, or failure type and ordinal message.</summary>
    /// <param name="other">The other result.</param>
    /// <returns>Whether both results have the same value semantics.</returns>
    public bool Equals(Result<T> other) => IsSuccess == other.IsSuccess && (IsSuccess
        ? EqualityComparer<T>.Default.Equals(_value!, other._value!)
        : ResultError.Equals(Error, other.Error));

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Result<T> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => IsSuccess ? HashCode.Combine(1, _value) : ResultError.GetHashCode(Error);

    /// <summary>Compares result values for equality.</summary>
    public static bool operator ==(Result<T> left, Result<T> right) => left.Equals(right);

    /// <summary>Compares result values for inequality.</summary>
    public static bool operator !=(Result<T> left, Result<T> right) => !left.Equals(right);

    /// <inheritdoc />
    public override string ToString() => IsSuccess ? $"Success({_value})" : $"Failure({Error.Message})";
}
