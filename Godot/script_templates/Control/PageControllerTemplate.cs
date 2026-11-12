// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

// meta-name: UI 视图生命周期
// meta-description: 为 Control 根场景添加可选业务订阅与清理，由 UiRegistry 注册后使用。
using System.Threading;
using System.Threading.Tasks;
using GFramework.Godot.UI;
using Godot;

/// <summary>Owns view subscriptions on a Control scene root; the UI service owns the node lifetime.</summary>
public partial class _CLASS_ : _BASE_, IGodotUiLifecycle
{
    /// <summary>Prepares business state after mounting and before display.</summary>
    /// <param name="cancellationToken">Opening cancellation.</param>
    /// <returns>Preparation completion.</returns>
    public Task OpenAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    /// <summary>Releases business subscriptions before the service detaches and frees this view.</summary>
    /// <param name="cancellationToken">Cleanup token.</param>
    /// <returns>Cleanup completion.</returns>
    public Task CloseAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
