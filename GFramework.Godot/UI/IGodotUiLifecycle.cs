// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Godot.UI;

/// <summary>Optional business lifecycle implemented by an ordinary Control scene.</summary>
public interface IGodotUiLifecycle
{
    /// <summary>Prepares a mounted, hidden scene. Subscribe to business events here.</summary>
    /// <param name="cancellationToken">Opening cancellation.</param>
    /// <returns>Preparation completion.</returns>
    Task OpenAsync(CancellationToken cancellationToken = default);
    /// <summary>Unsubscribes business events before the host detaches the scene.</summary>
    /// <param name="cancellationToken">Cleanup token.</param>
    /// <returns>Cleanup completion.</returns>
    Task CloseAsync(CancellationToken cancellationToken = default);
}
