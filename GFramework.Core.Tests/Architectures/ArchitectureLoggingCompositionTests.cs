// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Architectures;
using GFramework.Core.Tests.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace GFramework.Core.Tests.Architectures;

/// <summary>Standard logging composition must remain effective inside the optional architecture.</summary>
[TestFixture]
public sealed class ArchitectureLoggingCompositionTests
{
    /// <summary>Providers and filters configured through AddLogging reach component loggers.</summary>
    [Test]
    public async Task StandardLoggingConfiguration_IsHonored()
    {
        using var capture = new TestLoggerProvider();
        await using var architecture = new ConfiguredArchitecture(capture);
        await architecture.InitializeAsync();
        var logger = architecture.Services.GetRequiredService<ILogger<ConfiguredArchitecture>>();
        logger.LogTrace("configured provider");
        Assert.That(capture.Logs, Has.Count.EqualTo(1));
        Assert.That(capture.Logs[0].LoggerName, Is.EqualTo(typeof(ConfiguredArchitecture).FullName!.Replace('+', '.')));
    }

    /// <summary>Consumer configuration uses the ordinary logging builder, without a separate factory override.</summary>
    private sealed class ConfiguredArchitecture(TestLoggerProvider capture) : Architecture
    {
        /// <inheritdoc />
        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(builder => builder.ClearProviders().SetMinimumLevel(LogLevel.Trace).AddProvider(capture));
        }
    }
}
