// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Enums;

namespace GFramework.Core.Abstractions.Architectures;

/// <summary>An observation of one architecture's operation state.</summary>
/// <param name="phase">The newly entered state.</param>
public sealed class ArchitecturePhaseChangedEventArgs(ArchitecturePhase phase) : EventArgs
{
    /// <summary>The newly entered state.</summary>
    public ArchitecturePhase Phase { get; } = phase;
}
