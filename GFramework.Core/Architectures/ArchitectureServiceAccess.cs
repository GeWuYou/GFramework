// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Architectures;

/// <summary>Exposes standard root resolution only inside the owner's valid access window.</summary>
/// <param name="owner">The architecture controlling availability.</param>
internal sealed class ArchitectureServiceAccess(Architecture owner) : IKeyedServiceProvider
{
    /// <inheritdoc />
    public object? GetService(Type serviceType)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        return owner.ResolveService(serviceType);
    }

    /// <inheritdoc />
    public object? GetKeyedService(Type serviceType, object? serviceKey)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        return owner.ResolveKeyedService(serviceType, serviceKey);
    }

    /// <inheritdoc />
    public object GetRequiredKeyedService(Type serviceType, object? serviceKey)
    {
        return GetKeyedService(serviceType, serviceKey) ??
            throw new InvalidOperationException($"No keyed service for type '{serviceType}' is registered.");
    }
}
