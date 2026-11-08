// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace GFramework.Tests.Common;

/// <summary>Test arrangement around a real ServiceCollection/provider; no alternate activation or ownership semantics.</summary>
public sealed class ServiceTestHost : IServiceProvider, IDisposable, IAsyncDisposable
{
    private ServiceProvider? _provider;
    /// <summary>The standard descriptions used by production registration extensions.</summary>
    public IServiceCollection Services { get; } = new ServiceCollection();
    /// <summary>Builds the final standard provider once.</summary>
    public void Freeze() => _provider = Services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    /// <summary>Resolves from the final provider.</summary>
    public object? GetService(Type serviceType) => (_provider ?? throw new InvalidOperationException("Build the test provider first.")).GetService(serviceType);
    /// <summary>Retrieves a singleton test fixture by contract.</summary>
    public T? Get<T>() where T : class => (T?)GetService(typeof(T));
    /// <summary>Retrieves a required test service.</summary>
    public T GetRequired<T>() where T : class => this.GetRequiredService<T>();
    /// <summary>Resolves all standard registrations in order.</summary>
    public IReadOnlyList<T> GetAll<T>() where T : class => this.GetServices<T>().ToArray();
    /// <summary>Resolves all runtime-typed registrations in order.</summary>
    public IReadOnlyList<object> GetAll(Type type) => this.GetServices(type).OfType<object>().ToArray();
    /// <summary>Registers a caller-owned test fixture instance.</summary>
    public void Register<T>(T instance) where T : class => Services.AddSingleton(instance);
    /// <summary>Registers a set of borrowed fixtures under one contract.</summary>
    public void Register(Type type, params object[] instances)
    {
        foreach (var instance in instances)
            Services.AddSingleton(type, instance);
    }
    /// <summary>Registers a borrowed singleton fixture.</summary>
    public void RegisterSingleton<TService>(TService instance) where TService : class => Services.AddSingleton(instance);
    /// <summary>Registers a standard singleton implementation.</summary>
    public void RegisterSingleton<TService, TImplementation>() where TService : class where TImplementation : class, TService => Services.AddSingleton<TService, TImplementation>();
    /// <summary>Registers a standard transient implementation.</summary>
    public void RegisterTransient<TService, TImplementation>() where TService : class where TImplementation : class, TService => Services.AddTransient<TService, TImplementation>();
    /// <summary>Registers a standard scoped implementation.</summary>
    public void RegisterScoped<TService, TImplementation>() where TService : class where TImplementation : class, TService => Services.AddScoped<TService, TImplementation>();
    /// <summary>Checks descriptors without constructing services.</summary>
    public bool HasRegistration(Type type) => Services.Any(descriptor => descriptor.ServiceType == type);
    /// <summary>Runs standard registration code before building.</summary>
    public void ExecuteServicesHook(Action<IServiceCollection> configure) => configure(Services);
    /// <summary>Creates a real request scope.</summary>
    public IServiceScope CreateScope() => (_provider ?? throw new InvalidOperationException("Build first.")).CreateScope();
    /// <inheritdoc />
    public void Dispose() => _provider?.Dispose();
    /// <inheritdoc />
    public ValueTask DisposeAsync() => _provider?.DisposeAsync() ?? ValueTask.CompletedTask;
}
