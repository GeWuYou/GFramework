// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Coroutine;
using GFramework.Core.Coroutine.Extensions;
using GFramework.Core.Coroutine.Instructions;
using GFramework.Godot.Coroutine;
using Godot;

namespace GodotIntegration;

public partial class DemoRoot
{
    /// <summary>Owns the native probe's nodes, task handles and observable cleanup state.</summary>
    private sealed class CoroutineSmokeProbe
    {
        /// <summary>The node whose scope owns the parent, descendants and physics tasks.</summary>
        public Node Owner { get; } = new() { Name = "CoroutineLifetimeProbe" };
        /// <summary>A separate lifetime used to cancel blocked wrappers.</summary>
        public Node SecondaryOwner { get; } = new() { Name = "CoroutineSecondaryProbe" };
        /// <summary>The external cancellation used while physics is paused.</summary>
        public CancellationTokenSource PausedCancellation { get; } = new();
        /// <summary>Every explicitly started task, retained for failure-path cleanup.</summary>
        public List<CoroutineHandle> Handles { get; } = new();
        /// <summary>The node-owned parent flow.</summary>
        public CoroutineHandle Parent { get; set; }
        /// <summary>The independently started node-bound wrapper.</summary>
        public CoroutineHandle Wrapper { get; set; }
        /// <summary>The node-bound wrapper produced inside a context factory.</summary>
        public CoroutineHandle FactoryWrapper { get; set; }
        /// <summary>The scene-pausable physics task.</summary>
        public CoroutineHandle GamePhysics { get; set; }
        /// <summary>The ignore-pause task after it switches into physics.</summary>
        public CoroutineHandle UiPhysics { get; set; }
        /// <summary>Whether the parent's actual iterator finally block ran.</summary>
        public bool ParentDisposed { get; set; }
        /// <summary>Whether the owned child's actual iterator finally block ran.</summary>
        public bool ChildDisposed { get; set; }
        /// <summary>Whether the direct wrapper released its underlying iterator.</summary>
        public bool WrapperDisposed { get; set; }
        /// <summary>Whether the factory-returned wrapper released its underlying iterator.</summary>
        public bool FactoryWrapperDisposed { get; set; }
        /// <summary>Game physics steps observed after the initial yield.</summary>
        public int GamePhysicsSteps { get; set; }
        /// <summary>Ignore-pause physics steps observed after the initial yield.</summary>
        public int UiPhysicsSteps { get; set; }
        /// <summary>Scope and child starts whose additional node lifetime must cancel their blocked waits.</summary>
        public List<CoroutineHandle> IntrinsicHandles { get; } = new();
        /// <summary>Actual iterator finally blocks executed by those intrinsic lifetime cancellations.</summary>
        public int IntrinsicDisposed { get; set; }
        /// <summary>Success continuations that must never run after their bound node exits.</summary>
        public int IntrinsicContinuations { get; set; }
    }

    /// <summary>Checks native stage transitions, paused physics, nested ownership and iterator cleanup.</summary>
    private async Task VerifyCoroutineRuntimeAsync()
    {
        Timing.Prewarm();
        var probe = new CoroutineSmokeProbe();
        AddChild(probe.Owner);
        AddChild(probe.SecondaryOwner);
        var wasPaused = GetTree().Paused;
        try
        {
            await VerifyCoroutineStageSwitchesAsync(probe).ConfigureAwait(true);
            StartCoroutineOwnershipProbe(probe);
            StartCoroutineIntrinsicLifetimeProbe(probe);
            await VerifyPausedCoroutineCleanupAsync(probe).ConfigureAwait(true);
            await VerifyCoroutineOwnerExitAsync(probe).ConfigureAwait(true);
            GD.Print("GFRAMEWORK_SMOKE_CHECKS coroutine-stages,pause,ownership,finally: PASS");
        }
        finally
        {
            GetTree().Paused = wasPaused;
            CleanupCoroutineProbe(probe);
        }
    }

    /// <summary>Uses actual engine callbacks to cross physics, idle dispatch and process in sequence.</summary>
    /// <param name="probe">The isolated native owner and failure-path handle collection.</param>
    private async Task VerifyCoroutineStageSwitchesAsync(CoroutineSmokeProbe probe)
    {
        var order = new List<string>();
        probe.Handles.Add(probe.Owner.RunCoroutine(CoroutineStageFlow(order), tag: "smoke-stage-flow"));
        await WaitSecondsAsync(0.15).ConfigureAwait(true);
        Require(order.SequenceEqual(new[] { "physics", "deferred", "update" }, StringComparer.Ordinal),
            "coroutine did not resume across native physics, deferred and process stages");
    }

    /// <summary>Creates owned descendants, independent node wrappers and physics tasks with different pause policies.</summary>
    /// <param name="probe">The isolated native probe.</param>
    private static void StartCoroutineOwnershipProbe(CoroutineSmokeProbe probe)
    {
        probe.Parent = probe.Owner.RunCoroutine(context => CoroutineParentFlow(context, probe), tag: "smoke-owned-parent");
        probe.Handles.Add(probe.Parent);
        Require(Timing.GetOwnedCoroutineCount(probe.Owner) == 2,
            "node-owned scope did not include the structured descendant");
        probe.Wrapper = Timing.Instance.RunCoroutineOnInstance(
            CoroutineBlockedWait(() => probe.WrapperDisposed = true).CancelWith(probe.SecondaryOwner),
            tag: "smoke-bound-wait");
        probe.Handles.Add(probe.Wrapper);
        probe.FactoryWrapper = probe.Owner.RunCoroutine(_ =>
            CoroutineBlockedWait(() => probe.FactoryWrapperDisposed = true).CancelWith(probe.SecondaryOwner),
            tag: "smoke-factory-bound-wait");
        probe.Handles.Add(probe.FactoryWrapper);
        probe.GamePhysics = probe.Owner.RunCoroutine(
            CoroutinePhysicsCounter(() => probe.GamePhysicsSteps++, switchFromUpdate: false),
            Segment.PhysicsProcess, cancellationToken: probe.PausedCancellation.Token);
        probe.Handles.Add(probe.GamePhysics);
        probe.UiPhysics = probe.Owner.RunCoroutine(
            CoroutinePhysicsCounter(() => probe.UiPhysicsSteps++, switchFromUpdate: true), Segment.ProcessIgnorePause);
        probe.Handles.Add(probe.UiPhysics);
    }

    /// <summary>Starts node-bound waits through raw scopes and child contexts, without Timing's run facades.</summary>
    /// <param name="probe">The native owners and observable disposal state.</param>
    private static void StartCoroutineIntrinsicLifetimeProbe(CoroutineSmokeProbe probe)
    {
        var scope = probe.Owner.CreateCoroutineScope();
        probe.IntrinsicHandles.Add(scope.Run(
            CoroutineBlockedWait(() => probe.IntrinsicDisposed++).CancelWith(probe.SecondaryOwner),
            stage: CoroutineExecutionStage.FixedUpdate));
        probe.IntrinsicHandles.Add(scope.Run(_ =>
            CoroutineBlockedWait(() => probe.IntrinsicDisposed++).CancelWith(probe.SecondaryOwner).CancelWith(probe.Owner),
            stage: CoroutineExecutionStage.FixedUpdate));
        probe.IntrinsicHandles.Add(scope.Run(
            CoroutineBlockedWait(() => probe.IntrinsicDisposed++).CancelWith(probe.SecondaryOwner)
                .Then(() => probe.IntrinsicContinuations++),
            stage: CoroutineExecutionStage.FixedUpdate));
        probe.Handles.Add(probe.Owner.RunCoroutine(context => CoroutineIntrinsicChildFlow(context, probe),
            Segment.PhysicsProcess));
        probe.Handles.AddRange(probe.IntrinsicHandles);
    }

    /// <summary>Separates scene pause, external token cancellation and native node-exit cancellation during long waits.</summary>
    /// <param name="probe">The native probe and its observable counters.</param>
    private async Task VerifyPausedCoroutineCleanupAsync(CoroutineSmokeProbe probe)
    {
        await WaitSecondsAsync(0.1).ConfigureAwait(true);
        GetTree().Paused = true;
        var frozenGameSteps = probe.GamePhysicsSteps;
        var priorUiSteps = probe.UiPhysicsSteps;
        await WaitSecondsAsync(0.12).ConfigureAwait(true);
        Require(probe.GamePhysicsSteps == frozenGameSteps, "game physics coroutine advanced while scene-paused");
        Require(probe.UiPhysicsSteps > priorUiSteps, "ignore-pause coroutine lost its policy after switching to physics");
        probe.PausedCancellation.Cancel();
        probe.SecondaryOwner.QueueFree();
        await WaitFramesAsync(3).ConfigureAwait(true);
        Require(!Timing.TryGetCoroutineSnapshot(probe.GamePhysics, out _),
            "external cancellation was not drained while physics was paused");
        Require(probe.WrapperDisposed && !Timing.TryGetCoroutineSnapshot(probe.Wrapper, out _),
            "CancelWith did not cancel a long wait and dispose its underlying iterator");
        Require(probe.FactoryWrapperDisposed && !Timing.TryGetCoroutineSnapshot(probe.FactoryWrapper, out _),
            "a factory-returned CancelWith iterator did not cancel during its long wait");
        Require(probe.IntrinsicDisposed == 6 && probe.IntrinsicContinuations == 0 &&
            probe.IntrinsicHandles.All(handle => !Timing.TryGetCoroutineSnapshot(handle, out _)),
            "scope or child starts did not honor CancelWith during a paused long wait");
        GD.Print("GFRAMEWORK_SMOKE_CHECKS coroutine-intrinsic-scope-child-lifetimes: PASS");
    }

    /// <summary>Releases an owner while paused and verifies actual descendant finally execution across stages.</summary>
    /// <param name="probe">The owner and descendant observations.</param>
    private async Task VerifyCoroutineOwnerExitAsync(CoroutineSmokeProbe probe)
    {
        probe.Owner.QueueFree();
        await WaitFramesAsync(3).ConfigureAwait(true);
        Require(probe.ParentDisposed && probe.ChildDisposed,
            "node exit did not dispose the parent and nested child iterators");
        Require(!Timing.TryGetCoroutineSnapshot(probe.Parent, out _) && !Timing.TryGetCoroutineSnapshot(probe.UiPhysics, out _),
            "node exit left an owned task alive in another stage");
    }

    /// <summary>Restores failure-path resources without touching ordinary gameplay nodes or user data.</summary>
    /// <param name="probe">The isolated resources to release.</param>
    private static void CleanupCoroutineProbe(CoroutineSmokeProbe probe)
    {
        foreach (var handle in probe.Handles)
            Timing.KillCoroutine(handle);
        probe.PausedCancellation.Dispose();
        if (GodotObject.IsInstanceValid(probe.Owner) && !probe.Owner.IsQueuedForDeletion())
            probe.Owner.QueueFree();
        if (GodotObject.IsInstanceValid(probe.SecondaryOwner) && !probe.SecondaryOwner.IsQueuedForDeletion())
            probe.SecondaryOwner.QueueFree();
    }

    /// <summary>Resumes only after each requested engine stage has been reached.</summary>
    /// <param name="order">The observed stage order.</param>
    /// <returns>The stage-switching flow.</returns>
    private static IEnumerator<IYieldInstruction> CoroutineStageFlow(List<string> order)
    {
        yield return new SwitchTo(CoroutineExecutionStage.FixedUpdate);
        order.Add("physics");
        yield return new SwitchTo(CoroutineExecutionStage.Deferred);
        order.Add("deferred");
        yield return new SwitchTo(CoroutineExecutionStage.Update);
        order.Add("update");
    }

    /// <summary>Owns a child in a different current stage until its native node exits.</summary>
    /// <param name="context">The parent's explicit child lifetime boundary.</param>
    /// <param name="probe">The disposal observations.</param>
    /// <returns>The parent flow.</returns>
    private static IEnumerator<IYieldInstruction> CoroutineParentFlow(CoroutineContext context, CoroutineSmokeProbe probe)
    {
        try
        {
            context.RunChild(CoroutineBlockedWait(() => probe.ChildDisposed = true));
            yield return new SwitchTo(CoroutineExecutionStage.FixedUpdate);
            yield return new Delay(100);
        }
        finally
        {
            probe.ParentDisposed = true;
        }
    }

    /// <summary>Starts child iterators and factories with node lifetimes while leaving their parent independent.</summary>
    /// <param name="context">The explicit parent-child ownership boundary.</param>
    /// <param name="probe">The native owners and child observations.</param>
    /// <returns>The parent blocked until its ordinary owner exits.</returns>
    private static IEnumerator<IYieldInstruction> CoroutineIntrinsicChildFlow(CoroutineContext context, CoroutineSmokeProbe probe)
    {
        probe.IntrinsicHandles.Add(context.RunChild(
            CoroutineBlockedWait(() => probe.IntrinsicDisposed++).CancelWith(probe.SecondaryOwner)));
        probe.IntrinsicHandles.Add(context.RunChild(_ =>
            CoroutineBlockedWait(() => probe.IntrinsicDisposed++).CancelWith(probe.SecondaryOwner).CancelWith(probe.Owner)));
        probe.IntrinsicHandles.Add(context.RunChild(
            CoroutineBlockedWait(() => probe.IntrinsicDisposed++).CancelWith(probe.SecondaryOwner)
                .Then(() => probe.IntrinsicContinuations++)));
        yield return new Delay(100);
    }

    /// <summary>Exposes whether cancellation actually disposes the user iterator during a long wait.</summary>
    /// <param name="onDisposed">The observable finally callback.</param>
    /// <returns>The blocked iterator.</returns>
    private static IEnumerator<IYieldInstruction> CoroutineBlockedWait(Action onDisposed)
    {
        try
        {
            yield return new Delay(100);
        }
        finally
        {
            onDisposed();
        }
    }

    /// <summary>Counts actual physics resumptions after optionally switching from an ignore-pause Update task.</summary>
    /// <param name="onStep">Records one resumed physics step.</param>
    /// <param name="switchFromUpdate">Whether this task starts in Update.</param>
    /// <returns>The physics counter iterator.</returns>
    private static IEnumerator<IYieldInstruction> CoroutinePhysicsCounter(Action onStep, bool switchFromUpdate)
    {
        if (switchFromUpdate)
            yield return new SwitchTo(CoroutineExecutionStage.FixedUpdate);
        while (true)
        {
            yield return new WaitOneFrame();
            onStep();
        }
    }
}
