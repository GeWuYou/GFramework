// Copyright (c) 2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Linq;

namespace GFramework.Ecs.Arch.Abstractions;

/// <summary>An immutable copy of counters and the latest attempted frame, safe to publish to another thread.</summary>
/// <param name="entityCount">World entity count observed by the owning thread.</param>
/// <param name="completedFrames">Successfully completed frames since initialization.</param>
/// <param name="failedFrames">Failed frames since initialization.</param>
/// <param name="lastFrameDuration">Elapsed time of the most recent attempted frame.</param>
/// <param name="systems">Top-level system measurements in execution order; copied defensively.</param>
public sealed class ArchStatisticsSnapshot(int entityCount, long completedFrames, long failedFrames,
    TimeSpan lastFrameDuration, IEnumerable<ArchSystemStatistics> systems)
{
    /// <summary>Entity count observed when the snapshot was captured, or at stop after runtime shutdown.</summary>
    public int EntityCount { get; } = entityCount;
    /// <summary>Number of systems registered directly with this runtime.</summary>
    public int SystemCount => Systems.Count;
    /// <summary>Frames that completed all three phases.</summary>
    public long CompletedFrames { get; } = completedFrames;
    /// <summary>Frames terminated by a callback exception.</summary>
    public long FailedFrames { get; } = failedFrames;
    /// <summary>Total wall-clock duration of the latest attempted frame.</summary>
    public TimeSpan LastFrameDuration { get; } = lastFrameDuration;
    /// <summary>A defensive read-only copy of the latest phase measurements.</summary>
    public IReadOnlyList<ArchSystemStatistics> Systems
    {
        get;
    } =
        Array.AsReadOnly((systems ?? throw new ArgumentNullException(nameof(systems))).ToArray());
}
