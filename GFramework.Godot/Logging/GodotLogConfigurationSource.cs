// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using GFramework.Core.Abstractions.Logging;

namespace GFramework.Godot.Logging;

/// <summary>
///     Owns discovery, loading, hot reload, and publication of the current Godot logger settings snapshot.
/// </summary>
/// <remarks>
///     Construction follows a fixed lifecycle: discover the configuration path, perform an initial strict load, then
///     subscribe a <see cref="FileSystemWatcher"/> when a concrete file exists. <see cref="CurrentSettings"/> is
///     published through <see cref="Volatile"/> so cached loggers can read a last-good immutable snapshot without
///     locking. Hot reload keeps the previous settings when a transient parse or file-system error occurs.
/// </remarks>
internal sealed class GodotLogConfigurationSource : IDisposable
{
    private readonly Action<GodotLoggerOptions>? _configure;
    private readonly FileSystemWatcher? _watcher;
    private GodotLoggerSettings _currentSettings = GodotLoggerSettings.Default;

    /// <summary>
    ///     Initializes the configuration source and starts watching the discovered file when one is available.
    /// </summary>
    /// <param name="configure">Optional imperative option overrides applied after file settings are loaded.</param>
    /// <exception cref="IOException">Thrown during initial loading when the configuration file cannot be read.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown during initial loading when the configuration file is locked.</exception>
    /// <remarks>
    ///     Initial loading uses retry/backoff and propagates the final error because startup configuration failures should
    ///     be visible. Watcher callbacks use the hot-reload path and preserve the previous snapshot on failure.
    /// </remarks>
    public GodotLogConfigurationSource(Action<GodotLoggerOptions>? configure)
    {
        _configure = configure;

        // Discovery is done before the first strict reload so startup reports invalid files immediately.
        ConfigurationPath = GodotLoggerSettingsLoader.DiscoverConfigurationPath();
        Reload(throwOnError: true);
        _watcher = CreateWatcher(ConfigurationPath);
    }

    /// <summary>
    ///     Gets the discovered configuration file path, or null when no supported location contains a file.
    /// </summary>
    public string? ConfigurationPath { get; }

    /// <summary>
    ///     Gets the last successfully loaded settings snapshot.
    /// </summary>
    /// <remarks>
    ///     The snapshot is read through <c>Volatile.Read</c> so logger instances running on other
    ///     threads observe settings published by reload callbacks without taking the configuration lock.
    /// </remarks>
    public GodotLoggerSettings CurrentSettings => Volatile.Read(ref _currentSettings);

    /// <summary>
    ///     Stops the file watcher before the source is abandoned.
    /// </summary>
    /// <remarks>
    ///     Disposal does not clear <see cref="CurrentSettings"/>; existing loggers can continue using the last published
    ///     snapshot after watcher notifications have been stopped.
    /// </remarks>
    public void Dispose()
    {
        _watcher?.Dispose();
    }

    /// <summary>
    ///     Creates the watcher that drives hot reload for the discovered configuration file.
    /// </summary>
    /// <param name="configurationPath">The configuration file to watch.</param>
    /// <returns>A configured watcher, or null when no stable directory and file name can be resolved.</returns>
    private FileSystemWatcher? CreateWatcher(string? configurationPath)
    {
        if (string.IsNullOrWhiteSpace(configurationPath))
        {
            return null;
        }

        var directory = Path.GetDirectoryName(configurationPath);
        var fileName = Path.GetFileName(configurationPath);
        if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        // FileSystemWatcher raises callbacks on thread-pool threads; callbacks keep reload work short and non-blocking.
        var watcher = new FileSystemWatcher(directory, fileName)
        {
            EnableRaisingEvents = true,
            NotifyFilter = NotifyFilters.CreationTime
                           | NotifyFilters.FileName
                           | NotifyFilters.LastWrite
                           | NotifyFilters.Size
        };

        watcher.Changed += OnConfigurationChanged;
        watcher.Created += OnConfigurationChanged;
        watcher.Deleted += OnConfigurationChanged;
        watcher.Renamed += OnConfigurationRenamed;
        return watcher;
    }

    private void OnConfigurationChanged(object sender, FileSystemEventArgs e)
    {
        Reload(throwOnError: false);
    }

    private void OnConfigurationRenamed(object sender, RenamedEventArgs e)
    {
        Reload(throwOnError: false);
    }

    /// <summary>
    ///     Reloads settings and publishes them when loading succeeds.
    /// </summary>
    /// <param name="throwOnError">Whether load errors should escape to the caller.</param>
    private void Reload(bool throwOnError)
    {
        try
        {
            var settings = throwOnError ? LoadSettingsWithRetry() : LoadSettings();

            // Volatile publication gives cached loggers a coherent replacement snapshot without per-log locks.
            Volatile.Write(ref _currentSettings, settings);
        }
        catch when (!throwOnError)
        {
            // Ignore transient parse or file-lock failures during hot reload and keep the last good snapshot.
        }
    }

    /// <summary>
    ///     Loads settings with short retry/backoff for startup races with file writers or deployment tools.
    /// </summary>
    /// <returns>The loaded settings snapshot.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no retry produced a usable settings snapshot.</exception>
    private GodotLoggerSettings LoadSettingsWithRetry()
    {
        Exception? lastError = null;

        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                return LoadSettings();
            }
            catch (IOException ex)
            {
                lastError = ex;
            }
            catch (UnauthorizedAccessException ex)
            {
                lastError = ex;
            }

            if (attempt < 2)
            {
                // Startup can race with a writer finishing appsettings.json; keep the retry bounded and deterministic.
                Thread.Sleep(50);
            }
        }

        throw lastError ?? new InvalidOperationException("Failed to load Godot logging configuration.");
    }

    /// <summary>
    ///     Loads settings from disk or defaults, then applies imperative overrides.
    /// </summary>
    /// <returns>The settings snapshot to publish.</returns>
    private GodotLoggerSettings LoadSettings()
    {
        var settings = string.IsNullOrWhiteSpace(ConfigurationPath) || !File.Exists(ConfigurationPath)
            ? GodotLoggerSettings.Default
            : GodotLoggerSettingsLoader.LoadFromJsonFile(ConfigurationPath);

        if (_configure == null)
        {
            return settings;
        }

        var configuredOptions = CloneOptions(settings.Options);
        _configure(configuredOptions);
        return new GodotLoggerSettings(
            configuredOptions.CreateNormalizedCopy(),
            settings.DefaultLogLevel,
            CopyLoggerLevels(settings));
    }

    /// <summary>
    ///     Creates a mutable options copy before user overrides are applied.
    /// </summary>
    /// <param name="options">The options from the file or default settings.</param>
    /// <returns>A normalized mutable copy.</returns>
    private static GodotLoggerOptions CloneOptions(GodotLoggerOptions options)
    {
        return new GodotLoggerOptions
        {
            Mode = options.Mode,
            DebugMinLevel = options.DebugMinLevel,
            ReleaseMinLevel = options.ReleaseMinLevel,
            DebugOutputTemplate = options.DebugOutputTemplate,
            ReleaseOutputTemplate = options.ReleaseOutputTemplate,
            Colors = options.Colors is { } colors
                ? new Dictionary<GFramework.Core.Abstractions.Logging.LogLevel, string>(colors)
                : []
        }.CreateNormalizedCopy();
    }

    /// <summary>
    ///     Copies category log level overrides into an ordinal dictionary.
    /// </summary>
    /// <param name="settings">The source settings snapshot.</param>
    /// <returns>A copy that preserves exact and prefix matching semantics.</returns>
    private static IReadOnlyDictionary<string, LogLevel> CopyLoggerLevels(
        GodotLoggerSettings settings)
    {
        var levels = new Dictionary<string, LogLevel>(StringComparer.Ordinal);
        foreach (var pair in settings.LoggerLevels)
        {
            levels[pair.Key] = pair.Value;
        }

        return levels;
    }
}
