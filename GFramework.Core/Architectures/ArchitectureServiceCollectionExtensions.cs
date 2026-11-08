// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;

namespace GFramework.Core.Architectures;

/// <summary>Describes optional lifecycle participants using standard DI registrations.</summary>
public static class ArchitectureServiceCollectionExtensions
{
    /// <summary>Registers one container-owned singleton participant under its concrete key.</summary>
    /// <typeparam name="T">The concrete participant.</typeparam>
    /// <param name="services">The current composition root.</param>
    /// <param name="order">Ascending startup order; ties preserve registration order.</param>
    /// <returns>The original collection.</returns>
    public static IServiceCollection AddComponent<T>(this IServiceCollection services, int order = 0)
        where T : class => services.AddComponent<T, T>(order);

    /// <summary>Registers one container-owned singleton using one canonical service key.</summary>
    /// <typeparam name="TService">The canonical contract.</typeparam>
    /// <typeparam name="TImplementation">The participant implementation.</typeparam>
    /// <param name="services">The current composition root.</param>
    /// <param name="order">Ascending startup order; ties preserve registration order.</param>
    /// <returns>The original collection.</returns>
    public static IServiceCollection AddComponent<TService, TImplementation>(this IServiceCollection services, int order = 0)
        where TService : class where TImplementation : class, TService
    {
        ArgumentNullException.ThrowIfNull(services);
        EnsureKeyAvailable<TService>(services);
        services.AddSingleton<TService, TImplementation>();
        services.AddSingleton(new ComponentRegistration(typeof(TService), order));
        return services;
    }

    /// <summary>Registers a borrowed singleton participant; the caller retains final Dispose ownership.</summary>
    /// <typeparam name="TService">Its sole canonical key.</typeparam>
    /// <param name="services">The current composition root.</param>
    /// <param name="instance">The externally created participant.</param>
    /// <param name="order">Ascending startup order.</param>
    /// <returns>The original collection.</returns>
    public static IServiceCollection AddComponent<TService>(this IServiceCollection services, TService instance, int order = 0)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(instance);
        EnsureKeyAvailable<TService>(services);
        services.AddSingleton(instance);
        services.AddSingleton(new ComponentRegistration(typeof(TService), order));
        return services;
    }

    /// <summary>Registers a factory-owned participant without adding interface aliases.</summary>
    /// <typeparam name="TService">Its sole canonical key.</typeparam>
    /// <param name="services">The current composition root.</param>
    /// <param name="factory">A factory executed only by the final provider.</param>
    /// <param name="order">Ascending startup order.</param>
    /// <returns>The original collection.</returns>
    public static IServiceCollection AddComponent<TService>(this IServiceCollection services, Func<IServiceProvider, TService> factory, int order = 0)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(factory);
        EnsureKeyAvailable<TService>(services);
        services.AddSingleton(factory);
        services.AddSingleton(new ComponentRegistration(typeof(TService), order));
        return services;
    }

    /// <summary>Executes one explicit module's description phase; it has no separate startup lifetime.</summary>
    /// <param name="services">The current composition root.</param>
    /// <param name="module">The explicitly selected module.</param>
    /// <returns>The original collection.</returns>
    public static IServiceCollection AddModule(this IServiceCollection services, IArchitectureModule module)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(module);
        module.ConfigureServices(services);
        return services;
    }

    /// <summary>Rejects accidental replacement of a canonical participant binding.</summary>
    private static void EnsureKeyAvailable<TService>(IServiceCollection services)
    {
        if (services.Any(descriptor => descriptor.ServiceType == typeof(TService)))
            throw new InvalidOperationException($"Component service {typeof(TService)} is already registered.");
    }
}
