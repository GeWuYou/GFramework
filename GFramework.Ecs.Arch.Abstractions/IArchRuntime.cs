// Copyright (c) 2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Ecs.Arch.Abstractions;

/// <summary>Drives one borrowed World and its ordered native systems on the owning host thread.</summary>
/// <remarks>
/// The runtime owns business start/stop, not final resource disposal. A composition root owns the World
/// and systems. Instances cannot restart after failure or stop; callers must join frame work before stopping.
/// </remarks>
public interface IArchRuntime
{
    /// <summary>Initializes once in stable priority order; failed attempts still participate in Stop.</summary>
    void Initialize();

    /// <summary>Runs all BeforeUpdate, then all Update, then all AfterUpdate callbacks.</summary>
    /// <param name="deltaTime">The host frame step.</param>
    /// <remarks>Failure propagates and permits only cleanup. Calls before startup or after stop do no work.</remarks>
    void Update(float deltaTime);

    /// <summary>Stops attempted systems in reverse order, aggregating errors without disposing dependencies.</summary>
    void Stop();

    /// <summary>Copies the latest frame measurements; disabled statistics return null without reading a timer.</summary>
    /// <returns>An immutable snapshot, or null when statistics were not enabled at construction.</returns>
    /// <remarks>Read on the owning thread; the returned snapshot may be shared across threads.</remarks>
    ArchStatisticsSnapshot? GetStatistics();
}
