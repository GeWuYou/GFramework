// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.Enums;

/// <summary>Observable architecture operation state; component order is independent of business naming.</summary>
public enum ArchitecturePhase
{
    /// <summary>No initialization has been attempted.</summary>
    Created,
    /// <summary>Only service descriptions may be added.</summary>
    Configuring,
    /// <summary>The unique provider exists and explicit participants are starting.</summary>
    Initializing,
    /// <summary>Components and host are ready to accept work.</summary>
    Ready,
    /// <summary>New external resolutions are rejected and cleanup is in progress.</summary>
    Stopping,
    /// <summary>All cleanup attempts have completed successfully.</summary>
    Stopped,
    /// <summary>Startup or shutdown failed; this architecture cannot restart.</summary>
    Faulted,
}
