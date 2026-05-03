using System;
using System.Collections.Generic;
using GFramework.Core.Abstractions.Logging;
using GFramework.Godot.Logging;

namespace GFramework.Godot.Tests.Logging;

/// <summary>
///     Verifies the Godot appender edge that adapts Core log entries to Godot output rendering.
/// </summary>
[TestFixture]
public sealed class GodotLogAppenderTests
{
    /// <summary>
    ///     Verifies that the appender renders Core log entry data and merged structured properties.
    /// </summary>
    [Test]
    public void Render_Should_Use_Core_LogEntry_And_Merged_Properties()
    {
        LogContext.Clear();
        using var sceneContext = LogContext.Push("Scene", "Boot");
        var appender = new GodotLogAppender(new GodotLoggerOptions
        {
            Mode = GodotLoggerMode.Release,
            ReleaseOutputTemplate = "{timestamp:yyyyMMdd}|{level:u3}|{category}|{message}{properties}"
        });
        var entry = new LogEntry(
            new DateTime(2026, 5, 3, 4, 5, 6, DateTimeKind.Utc),
            LogLevel.Info,
            "Game.Services.Inventory",
            "Ready",
            null,
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["   "] = "ignored",
                ["Score"] = 12.5m
            });

        var result = appender.Render(entry);

        Assert.Multiple(() =>
        {
            Assert.That(result, Does.StartWith("20260503|INF|Game.Services.Inventory|Ready | "));
            Assert.That(result, Does.Contain("Scene=Boot"));
            Assert.That(result, Does.Contain("Score=12.5"));
        });
    }

    /// <summary>
    ///     Verifies that dynamic option providers are evaluated for each rendered log entry.
    /// </summary>
    [Test]
    public void Render_Should_Use_Latest_Options_From_Provider()
    {
        var options = new GodotLoggerOptions
        {
            Mode = GodotLoggerMode.Release,
            ReleaseOutputTemplate = "[release] {message}"
        };
        var appender = new GodotLogAppender(() => options);
        var entry = new LogEntry(
            DateTime.UtcNow,
            LogLevel.Warning,
            "Game",
            "Reloaded",
            null,
            null);

        var releaseResult = appender.Render(entry);

        options = new GodotLoggerOptions
        {
            Mode = GodotLoggerMode.Debug,
            DebugOutputTemplate = "[debug] {message}"
        };

        var debugResult = appender.Render(entry);

        Assert.Multiple(() =>
        {
            Assert.That(releaseResult, Is.EqualTo("[release] Reloaded"));
            Assert.That(debugResult, Is.EqualTo("[debug] Reloaded"));
        });
    }
}
