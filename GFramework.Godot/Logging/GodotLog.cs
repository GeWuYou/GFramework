// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace GFramework.Godot.Logging;

/// <summary>
///     Static Godot logging composition entry with auto-discovered configuration and deferred provider materialization.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="GodotLog"/> owns the <see cref="GodotLogProvider"/> and the configuration source
///         (appsettings.json discovery plus hot reload). DI hosts attach the provider to their own standard
///         logger factory, for example <c>LoggerFactory.Create(builder =&gt; builder.AddProvider(GodotLog.Provider))</c>,
///         and pass that factory to the architecture; non-DI hosts can call <see cref="CreateLogger(string)"/> directly.
///     </para>
///     <para>
///         There is deliberately no process-level default-provider override: two architecture instances must not
///         pollute each other's logs, so every host composes its own factory. The provider resolves settings lazily,
///         which keeps <see cref="Configure"/> usable until the first log call even when loggers were created earlier.
///     </para>
/// </remarks>
public static class GodotLog
{
#if NET9_0_OR_GREATER
    private static readonly System.Threading.Lock ConfigureLock = new();
#else
    private static readonly object ConfigureLock = new();
#endif
    private static Action<GodotLoggerOptions>? _configure;
    private static bool _configurationStarted;

    private static readonly Lazy<GodotLogConfigurationSource> LazyConfigurationSource = new(
        CreateConfigurationSource,
        LazyThreadSafetyMode.ExecutionAndPublication);

    private static readonly Lazy<GodotLogProvider> LazyProvider = new(
        static () => new GodotLogProvider(() => LazyConfigurationSource.Value.CurrentSettings),
        LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    ///     Applies imperative option overrides until logging settings are first read, including after logger creation.
    /// </summary>
    /// <param name="configure">The options mutator.</param>
    /// <exception cref="InvalidOperationException">
    ///     Thrown once configuration-source creation has started.
    /// </exception>
    public static void Configure(Action<GodotLoggerOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        lock (ConfigureLock)
        {
            if (_configurationStarted)
            {
                throw new InvalidOperationException(
                    "GodotLog.Configure must be called before logging configuration is first read.");
            }

            _configure = configure;
        }
    }

    /// <summary>
    ///     Gets the Godot <see cref="ILoggerProvider"/> that renders standard log calls to Godot's output APIs.
    /// </summary>
    /// <remarks>
    ///     Accessing this property materializes the provider but not the configuration source, so the provider
    ///     still picks up later <see cref="Configure"/> calls and hot-reloaded settings on the log path.
    /// </remarks>
    public static ILoggerProvider Provider
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
    ///     Creates a standard logger for the specified category without materializing the configuration source.
    /// </summary>
    /// <param name="category">The logger category name.</param>
    /// <returns>A standard logger rendering through the Godot provider.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the provider has been disposed by <see cref="Shutdown"/>.</exception>
    public static ILogger CreateLogger(string category)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        return Provider.CreateLogger(category);
    }

    /// <summary>
    ///     Creates a standard logger for the specified type without materializing the configuration source.
    /// </summary>
    /// <typeparam name="T">The type whose full name becomes the logger category.</typeparam>
    /// <returns>A standard logger rendering through the Godot provider.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the provider has been disposed by <see cref="Shutdown"/>.</exception>
    public static ILogger CreateLogger<T>()
    {
        return CreateLogger(GetCategoryName(typeof(T)));
    }

    /// <summary>
    ///     Stops the file watcher owned by the materialized configuration source and disposes the provider, if materialized.
    /// </summary>
    /// <remarks>
    ///     Godot hosts often keep logging for the whole game lifetime. Dedicated servers and tests can call this method
    ///     during teardown to release the watcher handle deterministically. The static lazy instances are not reset:
    ///     <see cref="Provider"/> and <see cref="CreateLogger(string)"/> throw <see cref="ObjectDisposedException"/>
    ///     afterwards, while logger instances created earlier keep emitting with the last published settings snapshot
    ///     but no longer receive reload notifications.
    /// </remarks>
    public static void Shutdown()
    {
        if (LazyProvider.IsValueCreated)
        {
            LazyProvider.Value.Dispose();
        }

        if (LazyConfigurationSource.IsValueCreated)
        {
            LazyConfigurationSource.Value.Dispose();
        }
    }

    private static GodotLogConfigurationSource CreateConfigurationSource()
    {
        lock (ConfigureLock)
        {
            // Publish the boundary while holding the same lock as Configure; Lazy.IsValueCreated alone has a publication gap.
            _configurationStarted = true;
            return new GodotLogConfigurationSource(_configure);
        }
    }

    private static string GetCategoryName(Type type)
    {
        return type.FullName ?? type.Name;
    }
}
