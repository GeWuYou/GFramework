// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using System.Text.Json;
using GFramework.Core.Logging;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace GFramework.Core.Tests.Logging;

/// <summary>
///     测试 LoggingConfiguration 和 LoggingConfigurationLoader 的功能和行为，
///     包括 JSON 反序列化与 <see cref="LoggingConfigurationLoader.CreateFactory" /> 生成的标准工厂行为。
/// </summary>
[TestFixture]
public class LoggingConfigurationTests
{
    [Test]
    public void LoadFromJsonString_WithValidJson_ShouldDeserialize()
    {
        var json = @"{
            ""minLevel"": ""debug"",
            ""appenders"": [
                {
                    ""type"": ""Console"",
                    ""formatter"": ""Default"",
                    ""useColors"": true
                }
            ],
            ""loggerLevels"": {
                ""GFramework.Core"": ""trace"",
                ""MyApp"": ""information""
            }
        }";

        var config = LoggingConfigurationLoader.LoadFromJsonString(json);

        Assert.That(config.MinLevel, Is.EqualTo(LogLevel.Debug));
        Assert.That(config.Appenders.Count, Is.EqualTo(1));
        Assert.That(config.Appenders[0].Type, Is.EqualTo("Console"));
        Assert.That(config.LoggerLevels.Count, Is.EqualTo(2));
        Assert.That(config.LoggerLevels["GFramework.Core"], Is.EqualTo(LogLevel.Trace));
        Assert.That(config.LoggerLevels["MyApp"], Is.EqualTo(LogLevel.Information));
    }

    [Test]
    public void Configuration_Collections_Should_Preserve_Public_Concrete_Types()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                typeof(LoggingConfiguration).GetProperty(nameof(LoggingConfiguration.Appenders))!.PropertyType,
                Is.EqualTo(typeof(List<AppenderConfiguration>)));
            Assert.That(
                typeof(LoggingConfiguration).GetProperty(nameof(LoggingConfiguration.LoggerLevels))!.PropertyType,
                Is.EqualTo(typeof(Dictionary<string, LogLevel>)));
            Assert.That(
                typeof(FilterConfiguration).GetProperty(nameof(FilterConfiguration.Namespaces))!.PropertyType,
                Is.EqualTo(typeof(List<string>)));
            Assert.That(
                typeof(FilterConfiguration).GetProperty(nameof(FilterConfiguration.Filters))!.PropertyType,
                Is.EqualTo(typeof(List<FilterConfiguration>)));
        });
    }

    [Test]
    public void LoggerLevels_Should_Remain_Case_Sensitive_By_Default()
    {
        var config = new LoggingConfiguration();
        config.LoggerLevels["GFramework.Core"] = LogLevel.Information;

        Assert.Multiple(() =>
        {
            Assert.That(config.LoggerLevels.ContainsKey("GFramework.Core"), Is.True);
            Assert.That(config.LoggerLevels["GFramework.Core"], Is.EqualTo(LogLevel.Information));
            Assert.That(config.LoggerLevels.ContainsKey("gframework.core"), Is.False);
        });
    }

    [Test]
    public void LoadFromJsonString_WithInvalidJson_ShouldThrow()
    {
        var invalidJson = "{ invalid json }";

        Assert.Throws<JsonException>(() => LoggingConfigurationLoader.LoadFromJsonString(invalidJson));
    }

    [Test]
    public void CreateFactory_WithConsoleAppender_ShouldCreateStandardFactory()
    {
        var json = @"{
            ""minLevel"": ""information"",
            ""appenders"": [
                {
                    ""type"": ""Console"",
                    ""formatter"": ""Default"",
                    ""useColors"": false
                }
            ]
        }";

        var config = LoggingConfigurationLoader.LoadFromJsonString(json);
        using var factory = LoggingConfigurationLoader.CreateFactory(config);

        Assert.Multiple(() =>
        {
            Assert.That(factory, Is.InstanceOf<ILoggerFactory>());
            Assert.That(factory.CreateLogger("TestLogger"), Is.Not.Null);
        });
    }

    [Test]
    public void CreateFactory_Should_Apply_Global_Minimum_Level()
    {
        var config = new LoggingConfiguration
        {
            MinLevel = LogLevel.Warning,
        };
        config.Appenders.Add(new AppenderConfiguration
        {
            Type = "Console",
            Formatter = "Default",
            UseColors = false,
        });

        using var factory = LoggingConfigurationLoader.CreateFactory(config);

        var logger = factory.CreateLogger("TestLogger");
        Assert.Multiple(() =>
        {
            Assert.That(logger.IsEnabled(LogLevel.Information), Is.False);
            Assert.That(logger.IsEnabled(LogLevel.Warning), Is.True);
            Assert.That(logger.IsEnabled(LogLevel.Error), Is.True);
        });
    }

    [Test]
    public void CreateFactory_Should_Apply_Per_Category_Level_Filters()
    {
        var config = new LoggingConfiguration
        {
            MinLevel = LogLevel.Information,
        };
        config.Appenders.Add(new AppenderConfiguration
        {
            Type = "Console",
            Formatter = "Default",
            UseColors = false,
        });
        config.LoggerLevels["MyApp"] = LogLevel.Critical;

        using var factory = LoggingConfigurationLoader.CreateFactory(config);

        var filteredLogger = factory.CreateLogger("MyApp.Controllers");
        var otherLogger = factory.CreateLogger("OtherNamespace");
        Assert.Multiple(() =>
        {
            Assert.That(filteredLogger.IsEnabled(LogLevel.Warning), Is.False);
            Assert.That(filteredLogger.IsEnabled(LogLevel.Critical), Is.True);
            Assert.That(otherLogger.IsEnabled(LogLevel.Information), Is.True);
        });
    }

    [Test]
    public void CreateFactory_Should_Prefer_Longest_Category_Prefix_Match()
    {
        var config = new LoggingConfiguration
        {
            MinLevel = LogLevel.Information,
        };
        config.Appenders.Add(new AppenderConfiguration
        {
            Type = "Console",
            Formatter = "Default",
            UseColors = false,
        });
        config.LoggerLevels["GFramework"] = LogLevel.Warning;
        config.LoggerLevels["GFramework.Core"] = LogLevel.Trace;

        using var factory = LoggingConfigurationLoader.CreateFactory(config);

        var logger = factory.CreateLogger("GFramework.Core.Logging");
        Assert.Multiple(() =>
        {
            Assert.That(logger.IsEnabled(LogLevel.Trace), Is.True);
            Assert.That(logger.IsEnabled(LogLevel.Debug), Is.True);
        });
    }

    [Test]
    public void CreateFactory_WithFileAppender_ShouldCreateAppenderAndWriteOutput()
    {
        var testFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.log");

        try
        {
            var json = $@"{{
                ""minLevel"": ""information"",
                ""appenders"": [
                    {{
                        ""type"": ""File"",
                        ""filePath"": ""{testFile.Replace("\\", "\\\\", StringComparison.Ordinal)}"",
                        ""formatter"": ""Json""
                    }}
                ]
            }}";

            var config = LoggingConfigurationLoader.LoadFromJsonString(json);
            var factory = LoggingConfigurationLoader.CreateFactory(config);

            var logger = factory.CreateLogger("TestLogger");
            logger.LogInformation("Test message");
            factory.Dispose();

            // 验证配置创建的 appender 已把条目写入文件
            Assert.That(File.Exists(testFile), Is.True);
            var content = File.ReadAllText(testFile);
            Assert.That(content, Does.Contain("Test message"));
        }
        finally
        {
            if (File.Exists(testFile))
            {
                try
                {
                    File.Delete(testFile);
                }
                catch
                {
                }
            }
        }
    }

    [Test]
    public void CreateFactory_WithInvalidAppenderType_ShouldThrowException()
    {
        var json = @"{
            ""minLevel"": ""information"",
            ""appenders"": [
                {
                    ""type"": ""UnsupportedType"",
                    ""formatter"": ""Default""
                }
            ]
        }";

        var config = LoggingConfigurationLoader.LoadFromJsonString(json);
        Assert.Throws<NotSupportedException>(() => LoggingConfigurationLoader.CreateFactory(config));
    }

    [Test]
    public void CreateFactory_WithLogLevelFilter_ShouldApplyFilter()
    {
        var testFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.log");

        try
        {
            var json = $@"{{
                ""minLevel"": ""information"",
                ""appenders"": [
                    {{
                        ""type"": ""File"",
                        ""filePath"": ""{testFile.Replace("\\", "\\\\", StringComparison.Ordinal)}"",
                        ""formatter"": ""Default"",
                        ""filter"": {{
                            ""type"": ""LogLevel"",
                            ""minLevel"": ""warning""
                        }}
                    }}
                ]
            }}";

            var config = LoggingConfigurationLoader.LoadFromJsonString(json);
            var factory = LoggingConfigurationLoader.CreateFactory(config);

            var logger = factory.CreateLogger("TestLogger");
            logger.LogInformation("Info message");
            logger.LogWarning("Warning message");
            factory.Dispose();

            // 只有 Warning 应该被写入
            var content = File.ReadAllText(testFile);
            Assert.That(content, Does.Not.Contain("Info message"));
            Assert.That(content, Does.Contain("Warning message"));
        }
        finally
        {
            if (File.Exists(testFile))
            {
                try
                {
                    File.Delete(testFile);
                }
                catch
                {
                }
            }
        }
    }

    [Test]
    public void CreateFactory_WithNamespaceFilter_ShouldApplyFilter()
    {
        var json = @"{
            ""minLevel"": ""information"",
            ""appenders"": [
                {
                    ""type"": ""Console"",
                    ""formatter"": ""Default"",
                    ""filter"": {
                        ""type"": ""Namespace"",
                        ""namespaces"": [""GFramework""]
                    }
                }
            ]
        }";

        var config = LoggingConfigurationLoader.LoadFromJsonString(json);
        var filter = config.Appenders[0].Filter;
        Assert.That(filter, Is.Not.Null);
        Assert.That(filter!.Type, Is.EqualTo("Namespace"));
    }

    [Test]
    public void LoadFromJsonString_WithComplexConfiguration_ShouldWork()
    {
        var json = @"{
            ""minLevel"": ""information"",
            ""appenders"": [
                {
                    ""type"": ""Console"",
                    ""formatter"": ""Default"",
                    ""useColors"": true
                },
                {
                    ""type"": ""File"",
                    ""filePath"": ""logs/app.log"",
                    ""formatter"": ""Json"",
                    ""filter"": {
                        ""type"": ""LogLevel"",
                        ""minLevel"": ""warning""
                    }
                },
                {
                    ""type"": ""RollingFile"",
                    ""filePath"": ""logs/rolling.log"",
                    ""formatter"": ""Default"",
                    ""maxFileSize"": 10485760,
                    ""maxFileCount"": 5
                }
            ],
            ""loggerLevels"": {
                ""GFramework.Core"": ""debug"",
                ""MyApp.Controllers"": ""information"",
                ""MyApp.Services"": ""warning""
            }
        }";

        var config = LoggingConfigurationLoader.LoadFromJsonString(json);

        Assert.That(config.MinLevel, Is.EqualTo(LogLevel.Information));
        Assert.That(config.Appenders.Count, Is.EqualTo(3));
        Assert.That(config.Appenders[0].Type, Is.EqualTo("Console"));
        Assert.That(config.Appenders[1].Type, Is.EqualTo("File"));
        Assert.That(config.Appenders[1].Filter, Is.Not.Null);
        Assert.That(config.Appenders[2].Type, Is.EqualTo("RollingFile"));
        Assert.That(config.Appenders[2].MaxFileSize, Is.EqualTo(10485760));
        Assert.That(config.LoggerLevels.Count, Is.EqualTo(3));
    }
}
