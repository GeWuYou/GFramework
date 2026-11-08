// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Architectures;

/// <summary>Exposes standard root resolution only inside the owner's valid access window.</summary>
/// <param name="owner">The architecture controlling availability.</param>
internal sealed class ArchitectureServiceAccess(Architecture owner) : IServiceProvider
{
    /// <inheritdoc />
    public object? GetService(Type serviceType)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        return owner.ResolveService(serviceType);
    }
}
