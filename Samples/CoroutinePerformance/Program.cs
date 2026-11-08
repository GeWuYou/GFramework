// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Coroutine;
using GFramework.Core.Coroutine.Instructions;

namespace CoroutinePerformance;

/// <summary>
///     Measures bounded scheduler workloads independently of rendering and game business logic.
/// </summary>
internal static class Program
{
    private const int WarmupTicks = 2_000;
    private const int MeasuredTicks = 10_000;
    private const int ChurnStarts = 100_000;
    private static readonly IYieldInstruction Ready = new ProbeInstruction(done: true);
    private static readonly IYieldInstruction Waiting = new ProbeInstruction(done: false);
    private static readonly JsonSerializerOptions ReportJsonOptions = new() { WriteIndented = true };

    /// <summary>
    ///     Runs three repeats and emits machine-readable observations; threshold failures return a nonzero exit code.
    /// </summary>
    private static int Main()
    {
        var observations = new List<Observation>();
        for (var repetition = 1; repetition <= 3; repetition++)
        {
            foreach (var count in new[] { 100, 1_000, 10_000 })
            {
                observations.Add(Measure("waiting", count, repetition, advance: false));
                observations.Add(Measure("mixed-priority-ready", count, repetition, advance: true));
            }

            observations.Add(MeasureChurn(repetition));
        }

        var passed = observations.TrueForAll(static result => result.Passed);
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            Passed = passed,
            Runtime = RuntimeInformation.FrameworkDescription,
            OperatingSystem = RuntimeInformation.OSDescription,
            Environment.ProcessorCount,
            WarmupTicks,
            MeasuredTicks,
            ChurnStarts,
            Observations = observations,
        }, ReportJsonOptions));
        return passed ? 0 : 1;
    }

    /// <summary>
    ///     Measures scheduler work with preallocated instructions and enumerators, excluding setup and sorting samples.
    /// </summary>
    private static Observation Measure(string workload, int count, int repetition, bool advance)
    {
        using var scheduler = new CoroutineScheduler(new ProbeTimeSource(), initialCapacity: 16);
        for (var index = 0; index < count; index++)
        {
            scheduler.Run(Repeat(advance ? Ready : Waiting), priority: (CoroutinePriority)(index % 5));
        }

        var observation = MeasureTicks(scheduler, workload, count, repetition);
        scheduler.Clear();
        return observation;
    }

    /// <summary>
    ///     Repeatedly replaces short-lived nested flows at a fixed concurrent peak, then measures the idle scheduler.
    /// </summary>
    private static Observation MeasureChurn(int repetition)
    {
        const int peak = 1_000;
        using var scheduler = new CoroutineScheduler(new ProbeTimeSource(), initialCapacity: 16);
        for (var batch = 0; batch < ChurnStarts / (peak * 2); batch++)
        {
            for (var index = 0; index < peak; index++)
            {
                scheduler.Run(Parent());
            }

            // Parent and owned child each start once. Every batch must fully drain before the next one.
            for (var tick = 0; scheduler.ActiveCoroutineCount > 0 && tick < 10; tick++)
            {
                scheduler.Update();
            }

            if (scheduler.ActiveCoroutineCount != 0)
            {
                throw new InvalidOperationException("Nested churn did not drain within ten ticks.");
            }
        }

        var observation = MeasureTicks(scheduler, "post-churn-idle", 0, repetition);
        return observation with
        {
            Passed = observation.Passed && scheduler.AllocatedSlotCount <= peak * 2 &&
                     scheduler.SlotCapacity <= peak * 4,
        };
    }

    /// <summary>
    ///     Records per-update wall-clock samples and thread-local allocation after warming all hot paths.
    /// </summary>
    private static Observation MeasureTicks(CoroutineScheduler scheduler, string workload, int count, int repetition)
    {
        for (var tick = 0; tick < WarmupTicks; tick++)
        {
            scheduler.Update();
        }

        var samples = new double[MeasuredTicks];
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        for (var tick = 0; tick < samples.Length; tick++)
        {
            var start = Stopwatch.GetTimestamp();
            scheduler.Update();
            samples[tick] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        Array.Sort(samples);
        var p50 = samples[samples.Length / 2];
        var p95 = samples[(samples.Length * 95 / 100) - 1];
        var p99 = samples[(samples.Length * 99 / 100) - 1];
        var withinBudget = count != 1_000 || (p95 <= 1.0 && p99 <= 2.0);
        return new Observation(workload, count, repetition, p50, p95, p99, allocated,
            scheduler.SlotCapacity, scheduler.AllocatedSlotCount, allocated == 0 && withinBudget);
    }

    /// <summary>
    ///     Yields a shared stateless instruction so measured allocation belongs to the scheduler itself.
    /// </summary>
    private static IEnumerator<IYieldInstruction> Repeat(IYieldInstruction instruction)
    {
        while (true)
        {
            yield return instruction;
        }
    }

    /// <summary>
    ///     Models a short flow waiting for an owned child that advances once.
    /// </summary>
    private static IEnumerator<IYieldInstruction> Parent()
    {
        yield return new WaitForCoroutine(Child());
    }

    /// <summary>
    ///     Provides a one-tick child without measuring user instruction allocation.
    /// </summary>
    private static IEnumerator<IYieldInstruction> Child()
    {
        yield return Ready;
    }

    /// <summary>
    ///     Supplies deterministic deltas while measurements use a separate monotonic wall clock.
    /// </summary>
    private sealed class ProbeTimeSource : ITimeSource
    {
        /// <inheritdoc />
        public double CurrentTime
        {
            get; private set;
        }

        /// <inheritdoc />
        public double DeltaTime => 1.0 / 60;

        /// <inheritdoc />
        public void Update() => CurrentTime += DeltaTime;
    }

    /// <summary>
    ///     A stateless instruction shared only to isolate scheduler overhead from business allocation.
    /// </summary>
    private sealed class ProbeInstruction(bool done) : IYieldInstruction
    {
        /// <inheritdoc />
        public bool IsDone => done;

        /// <inheritdoc />
        public void Update(double deltaTime)
        {
        }
    }

    /// <summary>
    ///     Captures one repeat; values describe this machine and workload rather than a hardware-independent promise.
    /// </summary>
    private sealed record Observation(string Workload, int ActiveCount, int Repetition,
        double P50Milliseconds, double P95Milliseconds, double P99Milliseconds, long AllocatedBytes,
        int SlotCapacity, int AllocatedSlotCount, bool Passed);
}
