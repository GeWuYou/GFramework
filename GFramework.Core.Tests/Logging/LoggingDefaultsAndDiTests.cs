// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GFramework.Core.Abstractions.Resource;
using GFramework.Core.Logging;
using GFramework.Core.Pause;
using GFramework.Core.Resource;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace GFramework.Core.Tests.Logging;

/// <summary>Empty logging configuration and ordinary DI activation preserve actual configured output.</summary>
[TestFixture]
[NonParallelizable]
public sealed class LoggingDefaultsAndDiTests
{
    /// <summary>All documented empty configuration forms retain console fallback instead of rejecting an empty provider.</summary>
    [TestCase(null)]
    [TestCase("{}")]
    [TestCase("{\"appenders\":null,\"loggerLevels\":null}")]
    public void EmptyConfiguration_UsesConsoleOutput(string? json)
    {
        var configuration = json == null ? new LoggingConfiguration() : LoggingConfigurationLoader.LoadFromJsonString(json);
        var previous = Console.Out;
        using var output = new StringWriter();
        try
        {
            Console.SetOut(output);
            using var factory = LoggingConfigurationLoader.CreateFactory(configuration);
            factory.CreateLogger("fallback").LogInformation("default logging output");
            Assert.That(output.ToString(), Does.Contain("default logging output"));
        }
        finally
        {
            Console.SetOut(previous);
        }
    }

    /// <summary>Explicit DI factories select manager categories while preserving their published untyped logger constructors.</summary>
    [Test]
    public async Task DiActivatedPauseManager_UsesHostLoggingProvider()
    {
        var capture = new TestLoggerProvider();
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace).AddProvider(capture));
        services.AddSingleton(provider => new PauseStackManager(provider.GetService<ILogger<PauseStackManager>>()));
        await using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<PauseStackManager>().Push("test logging");
        var categories = capture.Logs.Select(entry => entry.LoggerName).ToArray();
        Assert.That(categories, Does.Contain(typeof(PauseStackManager).FullName));
    }

}
