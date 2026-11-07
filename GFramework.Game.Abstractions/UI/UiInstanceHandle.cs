// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Abstractions.UI;

/// <summary>Identifies an instance within the service that owns its entire lifecycle.</summary>
/// <param name="ServiceId">Identity of the owning service, preventing cross-host operations.</param>
/// <param name="InstanceId">Monotonic identifier within that service.</param>
[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Auto)]
public readonly record struct UiInstanceHandle(Guid ServiceId, long InstanceId);
