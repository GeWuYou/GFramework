// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using GFramework.Core.Abstractions.Logging;

namespace GFramework.Godot.Logging;

/// <summary>
///     Creates Godot platform logger instances.
/// </summary>
public sealed class GodotLoggerFactory : ILoggerFactory
{
    private readonly GodotLoggerOptions? _options;

    /// <summary>
    ///     Initializes a factory that preserves the historical fixed-format logger behavior.
    /// </summary>
    public GodotLoggerFactory()
    {
    }

    /// <summary>
    ///     Initializes a factory with Godot-specific formatting options.
    /// </summary>
    /// <param name="options">The logger options.</param>
    public GodotLoggerFactory(GodotLoggerOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    ///     Gets a logger with the specified name.
    /// </summary>
    /// <param name="name">The logger name.</param>
    /// <param name="minLevel">The minimum enabled level.</param>
    /// <returns>A Godot logger instance.</returns>
    public ILogger GetLogger(string name, LogLevel minLevel = LogLevel.Info)
    {
        ArgumentNullException.ThrowIfNull(name);

        if (_options == null)
        {
            return new GodotLogger(name, minLevel);
        }

        return new GodotLogger(name, _options.WithMinimumLevelFloor(minLevel));
    }
}
