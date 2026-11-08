// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.CompilerServices;
using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Coroutine.Instructions;
using GFramework.Godot.Coroutine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GFramework.Godot.Tests.Coroutine;

/// <summary>Verifies lazy provider creation without reading or mutating process-global GodotLog configuration.</summary>
[TestFixture]
[NonParallelizable]
public sealed class DeferredCoroutineLoggerTests
{
    /// <summary>Logger construction leaves its factory untouched, and all logging operations reuse one target.</summary>
    [Test]
    public void Logger_Should_Materialize_Only_On_First_Use_And_Cache_Target()
    {
        var calls = 0;
        var logger = new DeferredCoroutineLogger(() => { calls++; return NullLogger.Instance; });
        Assert.That(calls, Is.Zero);
        Assert.That(logger.IsEnabled(LogLevel.Error), Is.False);
        Assert.That(calls, Is.EqualTo(1));
        using var scope = logger.BeginScope("probe");
        logger.LogError("probe");
        Assert.That(calls, Is.EqualTo(1));
    }

    /// <summary>Initializing Timing and starting a task leave configuration open until a real coroutine failure is logged.</summary>
    [Test]
    public void Timing_Initialization_Should_Not_Resolve_Logger_Factory()
    {
        var calls = 0;
        var logger = new DeferredCoroutineLogger(() => { calls++; return NullLogger.Instance; });
        var timing = (Timing)RuntimeHelpers.GetUninitializedObject(typeof(Timing));
        timing.InitializeForTests(logger: logger);
        try
        {
            var handle = timing.RunCoroutineOnInstance(Failing());
            Assert.That(calls, Is.Zero);
            timing.AdvanceProcessFrameForTests(paused: false);
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(Timing.TryGetCoroutineResult(handle, out var result), Is.True);
            Assert.That(result.Status, Is.EqualTo(CoroutineCompletionStatus.Faulted));
            logger.LogWarning("second operation");
            Assert.That(calls, Is.EqualTo(1));
        }
        finally
        {
            timing.DisposeForTests();
        }
    }

    /// <summary>Fails only after startup, allowing initialization and actual failure logging to be distinguished.</summary>
    /// <returns>The failing test iterator.</returns>
    private static IEnumerator<IYieldInstruction> Failing()
    {
        yield return new WaitOneFrame();
        throw new InvalidOperationException("expected logger probe failure");
    }
}
