// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using GFramework.Core.Abstractions.Architectures;

namespace GFramework.Cqrs;

/// <summary>Explicit configuration-only CQRS module; the runtime remains usable without Architecture.</summary>
/// <param name="handlerAssemblies">Assemblies explicitly selected by the composition root.</param>
public sealed class CqrsRuntimeModule(params Assembly[] handlerAssemblies) : IArchitectureModule
{
    /// <inheritdoc />
    public void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddCqrsRuntime();
        if (handlerAssemblies.Length > 0)
            services.AddCqrsHandlersFromAssemblies(handlerAssemblies);
    }
}
