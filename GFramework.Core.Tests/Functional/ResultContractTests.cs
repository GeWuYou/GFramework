// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading;
using System.Threading.Tasks;
using GFramework.Core.Functional;
using GFramework.Core.Functional.Async;
using GFramework.Core.Functional.Functions;
using NUnit.Framework;

namespace GFramework.Core.Tests.Functional;

/// <summary>Regression coverage for default-value handling and consistent synchronous/asynchronous exception policy.</summary>
[TestFixture]
public sealed class ResultContractTests
{
    /// <summary>Neither result type has an implicit third state or a null error for default values.</summary>
    [Test]
    public void DefaultResults_AreFailuresWithSharedMeaningfulError()
    {
        var unit = default(Result);
        var typed = default(Result<int>);
        Assert.Multiple(() =>
        {
            Assert.That(unit.IsFailure, Is.True);
            Assert.That(typed.IsFailure, Is.True);
            Assert.That(unit.Error, Is.SameAs(typed.Error));
            Assert.That(unit.Error, Is.SameAs(default(Result).Error));
            Assert.That(typed.Error.Message, Does.Contain("not been initialized"));
            Assert.That(unit.Match<Exception?>(() => null, error => error), Is.SameAs(unit.Error));
            Assert.That(unit.ToResult(1).Error, Is.SameAs(unit.Error));
            Assert.That(unit.Map(() => 1).Error, Is.SameAs(unit.Error));
            Assert.That(unit.Bind(() => Result<int>.Success(1)).Error, Is.SameAs(unit.Error));
            Assert.That(typed.Map(value => value + 1).Error, Is.SameAs(typed.Error));
            Assert.That(typed.Bind(value => Result<int>.Success(value + 1)).Error, Is.SameAs(typed.Error));
        });
        Exception? observed = null;
        typed.IfFail(error => { observed = error; });
        Assert.That(observed, Is.SameAs(typed.Error));
    }

    /// <summary>Combining a default result must not silently omit it and manufacture a successful collection.</summary>
    [Test]
    public void Combine_DefaultResultFailsInsteadOfBeingSkipped()
    {
        var combined = new[] { Result<int>.Success(1), default, Result<int>.Success(3) }.Combine();
        Assert.That(combined.IsFailure, Is.True);
        Assert.That(combined.Error, Is.SameAs(default(Result<int>).Error));
        var empty = Array.Empty<Result<int>>().Combine();
        Assert.That(empty.IsSuccess, Is.True);
        Assert.That(empty.IfFail([]), Is.Empty);
    }

    /// <summary>Preexisting failures, including defaults, bypass asynchronous user delegates.</summary>
    [Test]
    public async Task DefaultResult_PropagatesThroughAsyncTransformations()
    {
        var calls = 0;
        var mapped = await default(Result<int>).MapAsync(value => { calls++; return Task.FromResult(value); });
        var bound = await default(Result<int>).BindAsync(value => { calls++; return Task.FromResult(Result<int>.Success(value)); });
        Assert.That(calls, Is.Zero);
        Assert.That(mapped.Error, Is.SameAs(default(Result<int>).Error));
        Assert.That(bound.Error, Is.SameAs(default(Result<int>).Error));
    }

    /// <summary>Map and Bind propagate delegate failures on both synchronous and asynchronous paths.</summary>
    [Test]
    public async Task MapAndBind_DoNotCaptureDelegateExceptions()
    {
        var expected = new InvalidOperationException("application bug");
        var source = Result<int>.Success(1);
        Assert.That(Assert.Throws<InvalidOperationException>(() => source.Map<int>(_ => throw expected)), Is.SameAs(expected));
        Assert.That(Assert.Throws<InvalidOperationException>(() => source.Bind<int>(_ => throw expected)), Is.SameAs(expected));
        var mapError = await Assert.ThrowsAsync<InvalidOperationException>(() => source.MapAsync(_ => Task.FromException<int>(expected)));
        var bindError = await Assert.ThrowsAsync<InvalidOperationException>(() => source.BindAsync(_ => Task.FromException<Result<int>>(expected)));
        Assert.That(mapError, Is.SameAs(expected));
        Assert.That(bindError, Is.SameAs(expected));
    }

    /// <summary>Explicit Try captures the original ordinary exception without changing cancellation into business failure.</summary>
    [Test]
    public async Task TryMethods_CaptureOrdinaryExceptionsAndPropagateCancellation()
    {
        var ordinary = new InvalidOperationException("IO failed");
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var canceled = new OperationCanceledException(cancellation.Token);
        Assert.That(Result.Try(() => throw ordinary).Error, Is.SameAs(ordinary));
        Assert.That(Result<int>.Try(() => throw ordinary).Error, Is.SameAs(ordinary));
        Assert.That((await Result.TryAsync(() => Task.FromException(ordinary))).Error, Is.SameAs(ordinary));
        Assert.That((await Result<int>.TryAsync(() => Task.FromException<int>(ordinary))).Error, Is.SameAs(ordinary));
        Assert.That(Assert.Throws<OperationCanceledException>(() => Result.Try(() => throw canceled)), Is.SameAs(canceled));
        Assert.That(Assert.Throws<OperationCanceledException>(() => Result<int>.Try(() => throw canceled)), Is.SameAs(canceled));
        await Assert.CatchAsync<OperationCanceledException>(() => Result.TryAsync(() => Task.FromCanceled(cancellation.Token)));
        await Assert.CatchAsync<OperationCanceledException>(() => Result<int>.TryAsync(() => Task.FromCanceled<int>(cancellation.Token)));
        await Assert.CatchAsync<OperationCanceledException>(() => ((Func<Task<int>>)(() => Task.FromCanceled<int>(cancellation.Token))).TryAsync());
        Assert.Throws<OperationCanceledException>(() => 1.Try<int, int>(_ => throw canceled));
    }

    /// <summary>Error access distinguishes programmer misuse from ordinary failed result inspection.</summary>
    [Test]
    public void SuccessfulResults_RejectErrorAccess()
    {
        Assert.Throws<InvalidOperationException>(() => _ = Result.Success().Error);
        Assert.Throws<InvalidOperationException>(() => _ = Result<int>.Success(1).Error);
    }

    /// <summary>Success whose value is Exception is unambiguous and distinct from explicit Failure.</summary>
    [Test]
    public void ExceptionValues_AreDistinctFromFailures()
    {
        var exception = new InvalidOperationException("value");
        Result<Exception> implicitValue = exception;
        var value = Result<Exception>.Success(exception);
        var failure = Result<Exception>.Failure(exception);
        Assert.That(implicitValue.IsSuccess, Is.True);
        Assert.That(value.IfFail(new Exception()), Is.SameAs(exception));
        Assert.That(failure.Error, Is.SameAs(exception));
    }

    /// <summary>Default and explicit failures use consistent equality and hashing across repeated reads.</summary>
    [Test]
    public void FailureEquality_AndHashingAreStable()
    {
        var first = Result<int>.Failure(new InvalidOperationException("same"));
        var second = Result<int>.Failure(new InvalidOperationException("same"));
        Assert.That(first, Is.EqualTo(second));
        Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
        Assert.That(default(Result<int>), Is.EqualTo(default(Result<int>)));
        Assert.That(default(Result<int>).GetHashCode(), Is.EqualTo(default(Result<int>).GetHashCode()));
    }
}
