// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;

namespace GFramework.Core.Coroutine;

/// <summary>Opaque scheduler identity. Its generation prevents a released slot's handle from controlling its replacement.</summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct CoroutineHandle
{
    /// <summary>Creates a scheduler-owned identity; callers obtain handles through Run.</summary>
    internal CoroutineHandle(long schedulerId, int slotIndex, ulong generation)
    {
        SchedulerId = schedulerId;
        SlotIndex = slotIndex;
        Generation = generation;
    }

    /// <summary>Identifies the owning scheduler without a host-count limit.</summary>
    public long SchedulerId { get; }
    /// <summary>Identifies the reusable storage slot inside the owning scheduler.</summary>
    internal int SlotIndex { get; }
    /// <summary>Distinguishes successive occupants of the same storage slot.</summary>
    internal ulong Generation { get; }
    /// <summary>Whether the value was issued by a scheduler; a valid handle may already have completed.</summary>
    public bool IsValid => SchedulerId != 0 && Generation != 0;
    /// <summary>Returns a diagnostic identity.</summary>
    public override string ToString() => $"CoroutineHandle({SchedulerId}:{SlotIndex}:{Generation})";
}
