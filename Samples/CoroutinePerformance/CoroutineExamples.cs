// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Coroutine;
using GFramework.Core.Coroutine.Instructions;

namespace CoroutinePerformance;

/// <summary>
///     Provides compiled examples for the coroutine tutorial without mixing demonstration work into measurements.
/// </summary>
/// <remarks>Declares the phases this host will actually drive.</remarks>
[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)]
internal sealed class CoroutineExamples(ITimeSource timeSource) : IDisposable
{
    #region Host
    private readonly CoroutineScheduler _scheduler = new(timeSource, supportedStages:
        [
            CoroutineExecutionStage.Update,
            CoroutineExecutionStage.FixedUpdate,
            CoroutineExecutionStage.Deferred,
        ]);

    /// <summary>Starts a flow with owned children and default FailFast behavior.</summary>
    public CoroutineHandle Start() => _scheduler.Run(context => PlaySkill(context));

    /// <summary>Called by the host once per matching phase with that phase's own time deltas.</summary>
    public void Tick(CoroutineExecutionStage stage, double scaledDelta, double realDelta, bool paused)
    {
        _scheduler.Update(stage, scaledDelta, realDelta, paused);
    }

    /// <summary>Called by the host's shutdown path before the owning engine objects disappear.</summary>
    public void Dispose() => _scheduler.Dispose();
    #endregion

    #region Flow
    /// <summary>Owns two concurrent steps and waits for them before its final result is available.</summary>
    private static IEnumerator<IYieldInstruction> PlaySkill(CoroutineContext context)
    {
        context.RunChild(PlayEffect(), cancellationToken: global::System.Threading.CancellationToken.None);
        context.RunChild(ApplyDamage(), cancellationToken: global::System.Threading.CancellationToken.None);
        // Completing this iterator does not detach its children: the parent waits for both to finish.
        yield break;
    }

    /// <summary>Runs in the initial phase, then moves to the host's deferred phase.</summary>
    private static IEnumerator<IYieldInstruction> PlayEffect()
    {
        yield return new Delay(0.1);
        yield return new SwitchTo(CoroutineExecutionStage.Deferred);
        Console.WriteLine("Effect step in deferred phase");
    }

    /// <summary>Moves to the next fixed tick and remains there for subsequent ordinary waits.</summary>
    private static IEnumerator<IYieldInstruction> ApplyDamage()
    {
        yield return new SwitchTo(CoroutineExecutionStage.FixedUpdate);
        yield return new WaitOneFrame();
        Console.WriteLine("Damage step in fixed phase");
    }
    #endregion

    #region Observe
    /// <summary>Registers a completion task before cancellation, so bounded history cannot expire its result.</summary>
    public async System.Threading.Tasks.Task CancelAndObserveAsync(CoroutineHandle handle)
    {
        var completion = _scheduler.WaitForResultAsync(handle);
        _scheduler.Kill(handle);
        var result = await completion.ConfigureAwait(false);
        Console.WriteLine(result.Status);
        Console.WriteLine(result.Exception);
    }
    #endregion
}
