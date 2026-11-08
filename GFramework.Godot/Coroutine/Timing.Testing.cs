// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Coroutine;
using Microsoft.Extensions.Logging;

namespace GFramework.Godot.Coroutine;

public partial class Timing
{
    private Func<double>? _testProcessDelta;
    private Func<double>? _testPhysicsDelta;
    private Func<double>? _testDeferredDelta;

    /// <summary>Initializes only managed state, without constructing Godot's native Node base.</summary>
    /// <param name="processDeltaProvider">The controlled Update increment.</param>
    /// <param name="physicsDeltaProvider">The controlled physics increment.</param>
    /// <param name="deferredDeltaProvider">The controlled deferred increment.</param>
    /// <param name="logger">An optional managed logger for observing initialization without native logging.</param>
    internal void InitializeForTests(Func<double>? processDeltaProvider = null,
        Func<double>? physicsDeltaProvider = null, Func<double>? deferredDeltaProvider = null, ILogger? logger = null)
    {
        _isStopping = false;
        _ownerScopes ??= new Dictionary<ulong, OwnedCoroutineScope>();
        _testProcessDelta = processDeltaProvider ?? DefaultDeltaProvider;
        _testPhysicsDelta = physicsDeltaProvider ?? DefaultDeltaProvider;
        _testDeferredDelta = deferredDeltaProvider ?? _testProcessDelta;
        InitializeScheduler(new GodotTimeSource(_testProcessDelta), logger);
    }

    /// <summary>Advances controlled Update then Deferred ticks, including paused cancellation draining.</summary>
    /// <param name="paused">Whether game tasks are frozen; ignore-pause tasks retain their own policy.</param>
    internal void AdvanceProcessFrameForTests(bool paused)
    {
        AdvanceStageForTests(CoroutineExecutionStage.Update, _testProcessDelta, paused);
        AdvanceStageForTests(CoroutineExecutionStage.Deferred, _testDeferredDelta, paused);
    }

    /// <summary>Advances a controlled physics tick with the same pause rule as production.</summary>
    /// <param name="paused">Whether game physics tasks are frozen.</param>
    internal void AdvancePhysicsFrameForTests(bool paused = false) =>
        AdvanceStageForTests(CoroutineExecutionStage.FixedUpdate, _testPhysicsDelta, paused);

    /// <summary>Exposes the unified scheduler for observable results and snapshots.</summary>
    /// <param name="segment">A validated public segment; all segments share one scheduler.</param>
    /// <returns>The single Core scheduler.</returns>
    internal CoroutineScheduler GetSchedulerForTests(Segment segment)
    {
        _ = GetStage(segment);
        return Scheduler;
    }

    /// <summary>Cleans only managed state so pure tests never call native Godot APIs.</summary>
    internal void DisposeForTests()
    {
        DisposeScheduler();
        _testProcessDelta = null;
        _testPhysicsDelta = null;
        _testDeferredDelta = null;
    }

    /// <summary>Runs one synthetic stage using identical scaled and unscaled increments.</summary>
    /// <param name="stage">The stage under observation.</param>
    /// <param name="provider">The controlled increment.</param>
    /// <param name="paused">The controlled scene-pause state.</param>
    private void AdvanceStageForTests(CoroutineExecutionStage stage, Func<double>? provider, bool paused)
    {
        var delta = (provider ?? DefaultDeltaProvider)();
        Scheduler.Update(stage, delta, delta, paused);
    }

    /// <summary>Provides a deterministic synthetic 60 Hz host increment.</summary>
    /// <returns>One sixtieth of a second.</returns>
    private static double DefaultDeltaProvider() => 1.0 / 60.0;
}
