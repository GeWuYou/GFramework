// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Resource;

/// <summary>One loaded ownership; mutable state is protected by the manager's operation lock.</summary>
internal sealed class ResourceCacheEntry(object resource, Action release)
{
    /// <summary>Gets the resource returned by the original loader.</summary>
    public object Resource { get; } = resource;

    /// <summary>Gets the original loader's matching release operation.</summary>
    public Action Release { get; } = release;

    /// <summary>Gets or sets the number of independent active leases.</summary>
    public int LeaseCount
    {
        get; set;
    }
}
