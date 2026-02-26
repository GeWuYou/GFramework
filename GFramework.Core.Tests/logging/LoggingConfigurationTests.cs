using System.IO;
using System.Text.Json;
using GFramework.Core.Abstractions.logging;
using GFramework.Core.logging;
using NUnit.Framework;

namespace GFramework.Core.Tests.logging;

/// <summary>
///     测试 LoggingConfiguration 和 LoggingConfigurationLoader 的功能和行为
/// </summary>
[TestFixture]
public class LoggingConfigurationTests
{
    [Test]
    public void LoadFromJsonString_WithValidJson_ShouldDeserialize()
    {
        var json = @"{
            ""minLevel"": ""Debug"",
            ""appenders"": [
                {
                    ""type"": ""Console"",
                    ""formatter"": ""Default"",
                    ""useColors"": true
                }
            ],
            ""loggerLevels"": {
                ""GFramework.Core"": ""Trace"",
                ""MyApp"": ""Info""
            }
        }";

        var config = LoggingConfigurationLoader.LoadFromJsonString(json);

        Assert.That(config.MinLevel, Is.EqualTo(LogLevel.Debug));
        Assert.That(config.Appenders.Count, Is.EqualTo(1));
        Assert.That(config.Appenders[0].Type, Is.EqualTo("Console"));
        Assert.That(config.LoggerLevels.Count, Is.EqualTo(2));
        Assert.That(config.LoggerLevels["GFramework.Core"], Is.EqualTo(LogLevel.Trace));
    }

    [Test]
    public void LoadFromJsonString_WithInvalidJson_ShouldThrow()
    {
        var invalidJson = "{ invalid json }";

        Assert.Throws<JsonException>(() => LoggingConfigurationLoader.LoadFromJsonString(invalidJson));
    }

    [Test]
    public void CreateFactory_WithConsoleAppender_ShouldCreateFactory()
    {
        var json = @"{
            ""minLevel"": ""Info"",
            ""appenders"": [
                {
                    ""type"": ""Console"",
                    ""formatter"": ""Default"",
                    ""useColors"": false
                }
            ]
        }";

        var config = LoggingConfigurationLoader.LoadFromJsonString(json);
        var factory = LoggingConfigurationLoader.CreateFactory(config);

        Assert.That(factory, Is.Not.Null);

        var logger = factory.GetLogger("TestLogger");
        Assert.That(logger, Is.Not.Null);
        Assert.That(logger.Name(), Is.EqualTo("TestLogger"));
    }

    [Test]
    public void CreateFactory_WithFileAppender_ShouldCreateFactory()
    {
        var testFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.log");

        try
        {
            var json = $@"{{
                ""minLevel"": ""Info"",
                ""appenders"": [
                    {{
                        ""type"": ""File"",
                        ""filePath"": ""{testFile.Replace("\\", "\\\\")}"",
                        ""formatter"": ""Json""
                    }}
                ]
            }}";

            var config = LoggingConfigurationLoader.LoadFromJsonString(json);
            var factory = LoggingConfigurationLoader.CreateFactory(config);

            var logger = factory.GetLogger("TestLogger");
            logger.Info("Test message");

            // 验证文件是否创建
            Assert.That(File.Exists(testFile), Is.True);
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
    public void CreateFactory_WithLoggerLevels_ShouldApplyCorrectLevels()
    {
        var json = @"{
            ""minLevel"": ""Info"",
            ""appenders"": [
                {
                    ""type"": ""Console"",
                    ""formatter"": ""Default""
                }
            ],
            ""loggerLevels"": {
                ""GFramework.Core"": ""Trace"",
                ""MyApp"": ""Warning""
            }
        }";

        var config = LoggingConfigurationLoader.LoadFromJsonString(json);
        var factory = LoggingConfigurationLoader.CreateFactory(config);

        var logger1 = factory.GetLogger("GFramework.Core.Test");
        var logger2 = factory.GetLogger("MyApp.Controllers");
        var logger3 = factory.GetLogger("OtherNamespace");

        Assert.That(logger1.IsTraceEnabled(), Is.True);
        Assert.That(logger2.IsTraceEnabled(), Is.False);
        Assert.That(logger2.IsWarnEnabled(), Is.True);
        Assert.That(logger3.IsInfoEnabled(), Is.True);
    }

    [Test]
    public void CreateFactory_WithInvalidAppenderType_ShouldThrowException()
    {
        var json = @"{
            ""minLevel"": ""Info"",
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
                ""minLevel"": ""Info"",
                ""appenders"": [
                    {{
                        ""type"": ""File"",
                        ""filePath"": ""{testFile.Replace("\\", "\\\\")}"",
                        ""formatter"": ""Default"",
                        ""filter"": {{
                            ""type"": ""LogLevel"",
                            ""minLevel"": ""Warning""
                        }}
                    }}
                ]
            }}";

            var config = LoggingConfigurationLoader.LoadFromJsonString(json);
            var factory = LoggingConfigurationLoader.CreateFactory(config);

            var logger = factory.GetLogger("TestLogger");
            logger.Info("Info message");
            logger.Warn("Warning message");

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
        var testFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.log");

        try
        {
            var json = $@"{{
                ""minLevel"": ""Info"",
                ""appenders"": [
                    {{
                        ""type"": ""File"",
                        ""filePath"": ""{testFile.Replace("\\", "\\\\")}"",
                        ""formatter"": ""Default"",
                        ""filter"": {{
                            ""type"": ""Namespace"",
                            ""namespaces"": [""GFramework""]
                        }}
                    }}
                ]
            }}";

            var config = LoggingConfigurationLoader.LoadFromJsonString(json);
            Assert.That(config.Appenders[0].Filter, Is.Not.Null);
            Assert.That(config.Appenders[0].Filter.Type, Is.EqualTo("Namespace"));
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
    public void LoadFromJsonString_WithComplexConfiguration_ShouldWork()
    {
        var json = @"{
            ""minLevel"": ""Info"",
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
                        ""minLevel"": ""Warning""
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
                ""GFramework.Core"": ""Debug"",
                ""MyApp.Controllers"": ""Info"",
                ""MyApp.Services"": ""Warning""
            }
        }";

        var config = LoggingConfigurationLoader.LoadFromJsonString(json);

        Assert.That(config.MinLevel, Is.EqualTo(LogLevel.Info));
        Assert.That(config.Appenders.Count, Is.EqualTo(3));
        Assert.That(config.Appenders[0].Type, Is.EqualTo("Console"));
        Assert.That(config.Appenders[1].Type, Is.EqualTo("File"));
        Assert.That(config.Appenders[1].Filter, Is.Not.Null);
        Assert.That(config.Appenders[2].Type, Is.EqualTo("RollingFile"));
        Assert.That(config.Appenders[2].MaxFileSize, Is.EqualTo(10485760));
        Assert.That(config.LoggerLevels.Count, Is.EqualTo(3));
    }
}