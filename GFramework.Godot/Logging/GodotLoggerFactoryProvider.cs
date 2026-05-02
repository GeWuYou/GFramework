using System;
using System.Collections.Concurrent;
using GFramework.Core.Abstractions.Logging;

namespace GFramework.Godot.Logging;

/// <summary>
///     Provides cached Godot logger instances.
/// </summary>
public sealed class GodotLoggerFactoryProvider : ILoggerFactoryProvider
{
    private readonly ConcurrentDictionary<string, ILogger> _loggers = new(StringComparer.Ordinal);
    private readonly Func<GodotLoggerSettings> _settingsProvider;

    /// <summary>
    ///     Initializes a Godot logger provider with the default logger factory.
    /// </summary>
    public GodotLoggerFactoryProvider()
        : this(static () => GodotLoggerSettings.Default)
    {
    }

    /// <summary>
    ///     Initializes a Godot logger provider with Godot-specific formatting options.
    /// </summary>
    /// <param name="options">The logger options.</param>
    public GodotLoggerFactoryProvider(GodotLoggerOptions options)
        : this(CreateStaticSettingsProvider(options))
    {
    }

    internal GodotLoggerFactoryProvider(Func<GodotLoggerSettings> settingsProvider)
    {
        _settingsProvider = settingsProvider ?? throw new ArgumentNullException(nameof(settingsProvider));
    }

    /// <summary>
    ///     Gets or sets the provider minimum level.
    /// </summary>
    public LogLevel MinLevel { get; set; }

    /// <summary>
    ///     Creates a cached logger with the specified name.
    /// </summary>
    /// <param name="name">The logger name.</param>
    /// <returns>A logger configured with <see cref="MinLevel"/>.</returns>
    public ILogger CreateLogger(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        return _loggers.GetOrAdd(
            name,
            static (loggerName, provider) => new GodotLogger(
                loggerName,
                provider.GetOptions,
                () => provider.GetEffectiveMinLevel(loggerName)),
            this);
    }

    private GodotLoggerOptions GetOptions()
    {
        return _settingsProvider().Options;
    }

    private LogLevel GetEffectiveMinLevel(string categoryName)
    {
        return _settingsProvider().GetEffectiveMinLevel(categoryName, MinLevel);
    }

    private static Func<GodotLoggerSettings> CreateStaticSettingsProvider(GodotLoggerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var settings = GodotLoggerSettings.FromOptions(options);
        return () => settings;
    }
}
