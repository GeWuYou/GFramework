// Copyright (c) 2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Ecs.Arch.Abstractions;

/// <summary>Immutable measurements of one top-level system; Group timings include its children.</summary>
/// <param name="index">Stable runtime position, distinguishing repeated system types.</param>
/// <param name="systemType">The system type name without retaining a plugin Type object.</param>
/// <param name="beforeUpdateDuration">Elapsed pre-update time, or null when not invoked.</param>
/// <param name="updateDuration">Elapsed update time, or null when not invoked.</param>
/// <param name="afterUpdateDuration">Elapsed post-update time, or null when not invoked.</param>
/// <param name="failedPhase">The callback that failed, or null.</param>
public sealed class ArchSystemStatistics(int index, string systemType, TimeSpan? beforeUpdateDuration,
    TimeSpan? updateDuration, TimeSpan? afterUpdateDuration, ArchUpdatePhase? failedPhase)
{
    /// <summary>The stable position in the ordered top-level system list.</summary>
    public int Index { get; } = index;
    /// <summary>The system type name; nested Group children are not separately instrumented.</summary>
    public string SystemType { get; } = systemType;
    /// <summary>Elapsed pre-update time, including a failing callback, or null when skipped.</summary>
    public TimeSpan? BeforeUpdateDuration { get; } = beforeUpdateDuration;
    /// <summary>Elapsed update time, including a failing callback, or null when skipped.</summary>
    public TimeSpan? UpdateDuration { get; } = updateDuration;
    /// <summary>Elapsed post-update time, including a failing callback, or null when skipped.</summary>
    public TimeSpan? AfterUpdateDuration { get; } = afterUpdateDuration;
    /// <summary>The failed phase, or null when this system has not failed.</summary>
    public ArchUpdatePhase? FailedPhase { get; } = failedPhase;
    /// <summary>Whether all three phases completed successfully in the measured frame.</summary>
    public bool Completed => AfterUpdateDuration.HasValue && FailedPhase is null;
}
