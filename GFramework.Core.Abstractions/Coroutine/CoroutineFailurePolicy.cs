// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.Coroutine;

/// <summary>Failure propagation between tasks with a common owner; cancellation always flows downwards.</summary>
public enum CoroutineFailurePolicy
{
    /// <summary>A failing child fails its flow and cancels its siblings.</summary>
    FailFast,
    /// <summary>Independent tasks retain their results; one failure does not cancel siblings.</summary>
    Supervisor,
}
