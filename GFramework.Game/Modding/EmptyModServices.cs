// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Modding;

/// <summary>Default provider for hosts that expose no game-specific extension services.</summary>
internal sealed class EmptyModServices : IServiceProvider
{
    /// <summary>Gets the stateless provider.</summary>
    internal static EmptyModServices Instance { get; } = new();
    /// <inheritdoc />
    public object? GetService(Type serviceType)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        return null;
    }
}
