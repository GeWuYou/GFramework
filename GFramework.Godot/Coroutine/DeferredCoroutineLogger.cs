// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;

namespace GFramework.Godot.Coroutine;

/// <summary>Defers logging-provider materialization until a coroutine actually uses its logging surface.</summary>
/// <remarks>Creating or prewarming Timing must leave GodotLog configuration available until logging begins.</remarks>
internal sealed class DeferredCoroutineLogger : ILogger
{
    private readonly Lazy<ILogger> _logger;

    /// <summary>Retains a factory without resolving global provider state during host initialization.</summary>
    /// <param name="factory">Creates the target logger once, on the first logging operation.</param>
    internal DeferredCoroutineLogger(Func<ILogger> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _logger = new Lazy<ILogger>(factory);
    }

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => _logger.Value.BeginScope(state);

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => _logger.Value.IsEnabled(logLevel);

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter) => _logger.Value.Log(logLevel, eventId, state, exception, formatter);
}
