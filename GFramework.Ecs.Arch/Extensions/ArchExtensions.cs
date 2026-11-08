// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Threading;
using GFramework.Core.Architectures;
using Microsoft.Extensions.DependencyInjection;

namespace GFramework.Ecs.Arch.Extensions;

/// <summary>Standard composition-root extensions for optional Arch ECS adoption.</summary>
public static class ArchExtensions
{
    /// <summary>Describes one ECS World and its runtime participant.</summary>
    /// <param name="services">The composition root to configure.</param>
    /// <param name="configure">The explicit world and startup options.</param>
    /// <param name="hostContext">Explicit owning context for asynchronous native-host lifecycle; null retains synchronous adoption.</param>
    /// <returns>The original descriptions.</returns>
    public static IServiceCollection AddArch(this IServiceCollection services, Action<ArchOptions>? configure = null,
        SynchronizationContext? hostContext = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        var options = new ArchOptions();
        configure?.Invoke(options);
        return services.AddModule(new ArchEcsModule(options, hostContext: hostContext));
    }
}
