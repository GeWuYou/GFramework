// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading;
using GFramework.Core.Abstractions.Logging;

namespace GFramework.Godot.Logging;

/// <summary>
///     Static Godot logging entry point with auto-discovered configuration and deferred logger creation.
/// </summary>
public static class GodotLog
{
#if NET9_0_OR_GREATER
    private static readonly System.Threading.Lock ConfigureLock = new();
#else
    private static readonly object ConfigureLock = new();
#endif
    private static Action<GodotLoggerOptions>? _configure;

    private static readonly Lazy<GodotLogConfigurationSource> LazyConfigurationSource = new(
        CreateConfigurationSource,
        LazyThreadSafetyMode.ExecutionAndPublication);

    private static readonly Lazy<GodotLoggerFactoryProvider> LazyProvider = new(
        static () => new GodotLoggerFactoryProvider(() => LazyConfigurationSource.Value.CurrentSettings),
        LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    ///     Applies imperative option overrides before the global Godot logger provider is materialized.
    /// </summary>
    /// <param name="configure">The options mutator.</param>
    public static void Configure(Action<GodotLoggerOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        lock (ConfigureLock)
        {
            if (LazyProvider.IsValueCreated || LazyConfigurationSource.IsValueCreated)
            {
                throw new InvalidOperationException(
                    "GodotLog.Configure must be called before any GodotLog provider or configuration source is materialized.");
            }

            _configure = configure;
        }
    }

    /// <summary>
    ///     Gets the lazily-configured Godot logger provider.
    /// </summary>
    public static ILoggerFactoryProvider Provider
    {
        get
        {
            lock (ConfigureLock)
            {
                return LazyProvider.Value;
            }
        }
    }

    /// <summary>
    ///     Gets the discovered configuration file path, if any, without materializing the global configuration source.
    /// </summary>
    /// <remarks>
    ///     This property is safe for diagnostics before <see cref="Configure"/> runs. When the source is not created
    ///     yet, it performs discovery directly instead of touching <c>LazyConfigurationSource.Value</c>, so callers do
    ///     not accidentally lock in the default options before configuring <see cref="Provider"/>.
    /// </remarks>
    public static string? ConfigurationPath => LazyConfigurationSource.IsValueCreated
        ? LazyConfigurationSource.Value.ConfigurationPath
        : GodotLoggerSettingsLoader.DiscoverConfigurationPath();

    /// <summary>
    ///     Creates a logger for the specified category without materializing the provider until first use.
    /// </summary>
    public static ILogger CreateLogger(string category)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        return new DeferredLogger(category, static () => Provider);
    }

    /// <summary>
    ///     Creates a logger for the specified type without materializing the provider until first use.
    /// </summary>
    public static ILogger CreateLogger<T>()
    {
        return CreateLogger(GetCategoryName(typeof(T)));
    }

    /// <summary>
    ///     Installs the Godot provider as the current global resolver provider.
    /// </summary>
    public static void UseAsDefaultProvider()
    {
        LoggerFactoryResolver.Provider = Provider;
    }

    /// <summary>
    ///     Stops the file watcher owned by the materialized configuration source, if the source has been created.
    /// </summary>
    /// <remarks>
    ///     Godot hosts often keep process-wide logging for the whole game lifetime. Dedicated servers and tests can call
    ///     this method during teardown to release the watcher handle deterministically. The static lazy source is not
    ///     reset; later logger usage continues with the last published settings snapshot but no longer receives reload
    ///     notifications from the disposed watcher.
    /// </remarks>
    public static void Shutdown()
    {
        if (LazyConfigurationSource.IsValueCreated)
        {
            LazyConfigurationSource.Value.Dispose();
        }
    }

    private static GodotLogConfigurationSource CreateConfigurationSource()
    {
        lock (ConfigureLock)
        {
            return new GodotLogConfigurationSource(_configure);
        }
    }

    private static string GetCategoryName(Type type)
    {
        return type.FullName ?? type.Name;
    }
}
