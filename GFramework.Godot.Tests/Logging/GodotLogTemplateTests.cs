// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using GFramework.Core.Abstractions.Logging;
using GFramework.Godot.Logging;

namespace GFramework.Godot.Tests.Logging;

[TestFixture]
public sealed class GodotLogTemplateTests
{
    [Test]
    public void Render_Should_Format_Timestamp_Level_Color_Category_And_Message()
    {
        var template = GodotLogTemplate.Parse("[{timestamp:yyyyMMdd}] [color={color}]{level:u3}[/color] [{category:l16}] {message}");
        var context = new GodotLogRenderContext(
            new DateTime(2026, 5, 2, 1, 2, 3, DateTimeKind.Utc),
            LogLevel.Warning,
            "Game.Services.Inventory",
            "Loaded",
            "orange",
            string.Empty);

        var result = template.Render(context);

        Assert.That(result, Is.EqualTo("[20260502] [color=orange]WRN[/color] [G.S.Inventory   ] Loaded"));
    }

    [Test]
    public void Render_Should_Support_Lowercase_Level_Format()
    {
        var template = GodotLogTemplate.Parse("{level:l3}:{message}");
        var context = new GodotLogRenderContext(
            new DateTime(2026, 5, 2, 1, 2, 3, DateTimeKind.Utc),
            LogLevel.Debug,
            "Game",
            "Ready",
            "cyan",
            string.Empty);

        var result = template.Render(context);

        Assert.That(result, Is.EqualTo("dbg:Ready"));
    }

    [Test]
    public void Render_Should_Right_Align_Category()
    {
        var template = GodotLogTemplate.Parse("[{category:r10}]");
        var context = new GodotLogRenderContext(
            new DateTime(2026, 5, 2, 1, 2, 3, DateTimeKind.Utc),
            LogLevel.Info,
            "UI",
            "Ready",
            "white",
            string.Empty);

        var result = template.Render(context);

        Assert.That(result, Is.EqualTo("[        UI]"));
    }

    [Test]
    public void Render_Should_Preserve_Unknown_Placeholders()
    {
        var template = GodotLogTemplate.Parse("{message} {unknown}");
        var context = new GodotLogRenderContext(
            new DateTime(2026, 5, 2, 1, 2, 3, DateTimeKind.Utc),
            LogLevel.Info,
            "Game",
            "Ready",
            "white",
            string.Empty);

        var result = template.Render(context);

        Assert.That(result, Is.EqualTo("Ready {unknown}"));
    }

    [Test]
    public void Render_Should_Format_Padded_Level()
    {
        var template = GodotLogTemplate.Parse("{level:padded}");
        var context = new GodotLogRenderContext(
            new DateTime(2026, 5, 2, 1, 2, 3, DateTimeKind.Utc),
            LogLevel.Info,
            "Game",
            "Ready",
            "white",
            string.Empty);

        var result = template.Render(context);

        Assert.That(result, Is.EqualTo("INFO   "));
    }

    [Test]
    public void Render_Should_Append_Properties_Placeholder()
    {
        var template = GodotLogTemplate.Parse("{message}{properties}");
        var context = new GodotLogRenderContext(
            new DateTime(2026, 5, 2, 1, 2, 3, DateTimeKind.Utc),
            LogLevel.Info,
            "Game",
            "Ready",
            "white",
            " | Scene=Boot");

        var result = template.Render(context);

        Assert.That(result, Is.EqualTo("Ready | Scene=Boot"));
    }

    [Test]
    public void Options_ForMinimumLevel_Should_Preserve_Fixed_Minimum_Level()
    {
        var options = GodotLoggerOptions.ForMinimumLevel(LogLevel.Warning);

        Assert.That(options.Mode, Is.EqualTo(GodotLoggerMode.Debug));
        Assert.That(options.DebugMinLevel, Is.EqualTo(LogLevel.Warning));
        Assert.That(options.ReleaseMinLevel, Is.EqualTo(LogLevel.Warning));
    }

    [Test]
    public void Options_Should_Use_Default_Color_When_Configured_Color_Is_Missing()
    {
        var options = new GodotLoggerOptions();
        options.Colors.Remove(LogLevel.Error);

        var result = options.GetColor(LogLevel.Error);

        Assert.That(result, Is.EqualTo("red"));
    }

    [Test]
    public void Options_Should_Use_White_Color_When_Level_Is_Not_Defined()
    {
        var options = new GodotLoggerOptions();

        var result = options.GetColor((LogLevel)999);

        Assert.That(result, Is.EqualTo("white"));
    }
}
