// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Abstractions.UI;

/// <summary>
/// An owned native view lease. The service calls lifecycle methods on the host dispatcher and disposes the lease once.
/// Implementations release partial native state if their factory throws before returning the lease.
/// </summary>
public interface IUiView : IAsyncDisposable
{
    /// <summary>Gets whether the underlying view remains valid.</summary>
    bool IsAlive
    {
        get;
    }
    /// <summary>Prepares a mounted, hidden view before state is committed.</summary>
    /// <param name="cancellationToken">Cancellation of preparation.</param>
    /// <returns>Preparation completion.</returns>
    Task OpenAsync(CancellationToken cancellationToken = default);
    /// <summary>Stops business subscriptions before detachment and disposal; must tolerate partial opening.</summary>
    /// <param name="cancellationToken">Cleanup token; normal service cleanup supplies an uncancelled token.</param>
    /// <returns>Business cleanup completion.</returns>
    Task CloseAsync(CancellationToken cancellationToken = default);
    /// <summary>Changes native visibility without implicitly activating the view.</summary>
    /// <param name="visible">Whether the view should be visible.</param>
    void SetVisible(bool visible);
    /// <summary>Changes interaction eligibility independently of visibility and gameplay pause.</summary>
    /// <param name="active">Whether the view may receive input.</param>
    void SetActive(bool active);
}
