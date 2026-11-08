// Copyright (c) 2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Ecs.Arch.Abstractions;

/// <summary>The native system callback measured or reported as failed.</summary>
public enum ArchUpdatePhase
{
    /// <summary>The pre-update callback.</summary>
    BeforeUpdate,
    /// <summary>The main update callback.</summary>
    Update,
    /// <summary>The post-update callback.</summary>
    AfterUpdate,
}
