// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using GFramework.Core.Abstractions.Logging;
using Godot;

namespace GFramework.Godot.Logging;

/// <summary>
///     Discovers and parses Godot logging configuration documents.
/// </summary>
/// <remarks>
///     The loader treats JSON as external input: enum values are validated, nullable serializer output is normalized,
///     and unsupported values produce clear exceptions before a settings snapshot reaches runtime log rendering.
/// </remarks>
internal static class GodotLoggerSettingsLoader
{
    /// <summary>
    ///     Names the environment variable that can point to an explicit Godot logging configuration file.
    /// </summary>
    internal const string ConfigEnvironmentVariableName = "GODOT_LOGGER_CONFIG";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        Converters =
        {
            new GodotLogLevelJsonConverter(),
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true)
        }
    };

    /// <summary>
    ///     Finds the first supported configuration file location.
    /// </summary>
    /// <param name="environmentPath">Optional explicit path used instead of reading the environment variable.</param>
    /// <param name="processPath">Optional process path used when checking the executable directory.</param>
    /// <param name="projectPathResolver">Optional resolver for Godot <c>res://</c> paths.</param>
    /// <returns>The first existing configuration path, or null when none exists.</returns>
    public static string? DiscoverConfigurationPath(
        string? environmentPath = null,
        string? processPath = null,
        Func<string, string?>? projectPathResolver = null)
    {
        var envPath = environmentPath ?? System.Environment.GetEnvironmentVariable(ConfigEnvironmentVariableName);
        if (!string.IsNullOrWhiteSpace(envPath) && File.Exists(envPath))
        {
            return envPath;
        }

        var resolvedProcessPath = processPath ?? System.Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(resolvedProcessPath))
        {
            var executableDirectory = Path.GetDirectoryName(resolvedProcessPath);
            if (!string.IsNullOrWhiteSpace(executableDirectory))
            {
                var candidate = Path.Combine(executableDirectory, "appsettings.json");
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        var resolver = projectPathResolver ?? SafeGlobalizeProjectPath;
        var projectCandidate = resolver("res://appsettings.json");
        if (!string.IsNullOrWhiteSpace(projectCandidate) && File.Exists(projectCandidate))
        {
            return projectCandidate;
        }

        return null;
    }

    /// <summary>
    ///     Loads a settings snapshot from a JSON file.
    /// </summary>
    /// <param name="filePath">The configuration file path.</param>
    /// <returns>The parsed and normalized settings snapshot.</returns>
    /// <exception cref="FileNotFoundException">Thrown when <paramref name="filePath"/> does not exist.</exception>
    public static GodotLoggerSettings LoadFromJsonFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Configuration file not found: {filePath}", filePath);
        }

        return LoadFromJsonString(File.ReadAllText(filePath));
    }

    /// <summary>
    ///     Parses a settings snapshot from a JSON string.
    /// </summary>
    /// <param name="json">The JSON configuration content.</param>
    /// <returns>The parsed and normalized settings snapshot.</returns>
    /// <exception cref="JsonException">Thrown when an unsupported log level or malformed document is encountered.</exception>
    public static GodotLoggerSettings LoadFromJsonString(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        var root = JsonSerializer.Deserialize<GodotLoggerSettingsDocument>(json, JsonOptions)
                   ?? throw new InvalidOperationException("Failed to deserialize Godot logging configuration.");

        var logging = root.Logging;
        var options = (logging?.GodotLogger ?? new GodotLoggerOptions()).CreateNormalizedCopy();

        LogLevel? defaultLogLevel = null;
        var loggerLevels = new Dictionary<string, LogLevel>(StringComparer.Ordinal);

        if (logging?.LogLevel != null)
        {
            foreach (var pair in logging.LogLevel)
            {
                if (string.Equals(pair.Key, "Default", StringComparison.OrdinalIgnoreCase))
                {
                    defaultLogLevel = pair.Value;
                }
                else
                {
                    loggerLevels[pair.Key] = pair.Value;
                }
            }
        }

        return new GodotLoggerSettings(options, defaultLogLevel, loggerLevels);
    }

    private static string? SafeGlobalizeProjectPath(string path)
    {
        try
        {
            return ProjectSettings.GlobalizePath(path);
        }
        catch
        {
            return null;
        }
    }

    private sealed class GodotLoggerSettingsDocument
    {
        public LoggingDocument? Logging { get; set; }
    }

    private sealed class LoggingDocument
    {
        public GodotLoggerOptions? GodotLogger { get; set; }

        public Dictionary<string, LogLevel>? LogLevel { get; set; }
    }

    private sealed class GodotLogLevelJsonConverter : JsonConverter<LogLevel>
    {
        public override LogLevel Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var numericValue))
            {
                if (!Enum.IsDefined(typeof(LogLevel), numericValue))
                {
                    throw new JsonException(
                        $"Unsupported numeric {nameof(LogLevel)} value '{numericValue}'. Expected a defined {nameof(LogLevel)} value.");
                }

                return (LogLevel)numericValue;
            }

            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException($"Unexpected token {reader.TokenType} when parsing {nameof(LogLevel)}.");
            }

            return Parse(reader.GetString());
        }

        public override void Write(Utf8JsonWriter writer, LogLevel value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }

        public override LogLevel ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert,
            JsonSerializerOptions options)
        {
            return Parse(reader.GetString());
        }

        public override void WriteAsPropertyName(Utf8JsonWriter writer, LogLevel value, JsonSerializerOptions options)
        {
            writer.WritePropertyName(value.ToString());
        }

        private static LogLevel Parse(string? value)
        {
            return value?.Trim() switch
            {
                "Trace" or "trace" => LogLevel.Trace,
                "Debug" or "debug" => LogLevel.Debug,
                "Info" or "info" or "Information" or "information" => LogLevel.Info,
                "Warning" or "warning" or "Warn" or "warn" => LogLevel.Warning,
                "Error" or "error" => LogLevel.Error,
                "Fatal" or "fatal" or "Critical" or "critical" => LogLevel.Fatal,
                _ => throw new JsonException($"Unsupported log level '{value}'.")
            };
        }
    }
}
