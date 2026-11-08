// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.Architectures;

/// <summary>Groups service descriptions without performing activation, IO, or process-wide registration.</summary>
/// <remarks>Runtime work belongs to explicitly registered components, not to a second module lifecycle.</remarks>
public interface IArchitectureModule
{
    /// <summary>Adds this module's descriptions to the current composition root.</summary>
    /// <param name="services">The collection that the owning architecture will build once.</param>
    void ConfigureServices(IServiceCollection services);
}
