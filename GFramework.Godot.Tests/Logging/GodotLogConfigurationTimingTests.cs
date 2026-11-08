// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Godot.Logging;

namespace GFramework.Godot.Tests.Logging;

/// <summary>Checks early logger composition without materializing native logging settings.</summary>
[TestFixture]
[NonParallelizable]
public sealed class GodotLogConfigurationTimingTests
{
    /// <summary>Creating a static-style logger before startup must leave configuration available until first settings use.</summary>
    [Test]
    public void CreateLoggerBeforeStartup_DoesNotBlockConfigure()
    {
        var logger = GodotLog.CreateLogger("configuration-timing-probe");
        Assert.That(logger, Is.Not.Null);
        Assert.DoesNotThrow(() => GodotLog.Configure(static options => options.Mode = GodotLoggerMode.Debug));
    }
}
