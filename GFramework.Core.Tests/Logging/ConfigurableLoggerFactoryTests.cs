// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Logging;

namespace GFramework.Core.Tests.Logging;

/// <summary>
///     验证可配置 Logger 工厂在配置归一化、级别合并与释放路径上的行为契约。
/// </summary>
[TestFixture]
public sealed class ConfigurableLoggerFactoryTests
{
    /// <summary>
    ///     验证当反序列化结果把集合字段写成 <see langword="null" /> 时，工厂会将其归一化为空集合而不是抛出空引用异常。
    /// </summary>
    [Test]
    public void CreateFactory_ShouldNormalizeNullCollectionsFromConfiguration()
    {
        var config = LoggingConfigurationLoader.LoadFromJsonString(
            """
            {
              "minLevel": "Warning",
              "appenders": null,
              "loggerLevels": null
            }
            """);

        var factory = LoggingConfigurationLoader.CreateFactory(config);
        var logger = factory.GetLogger("TestLogger");

        Assert.Multiple(() =>
        {
            Assert.That(config.Appenders, Is.Not.Null);
            Assert.That(config.LoggerLevels, Is.Not.Null);
            Assert.That(logger.IsInfoEnabled(), Is.False);
            Assert.That(logger.IsWarnEnabled(), Is.True);
        });
    }

    /// <summary>
    ///     验证当配置输入把 appenders 集合中的某个元素反序列化为 <see langword="null" /> 时，工厂会抛出可诊断异常。
    /// </summary>
    [Test]
    public void CreateFactory_ShouldThrowInvalidOperationException_WhenAppenderEntryIsNull()
    {
        var config = LoggingConfigurationLoader.LoadFromJsonString(
            """
            {
              "appenders": [ null ]
            }
            """);

        var exception = Assert.Throws<InvalidOperationException>(() => LoggingConfigurationLoader.CreateFactory(config));

        Assert.That(exception!.Message, Is.EqualTo("Appender configuration cannot be null."));
    }

    /// <summary>
    ///     验证在未命中命名空间覆盖时，调用方传入的默认最小级别会作为最终 logger 级别的下限参与计算。
    /// </summary>
    [Test]
    public void GetLogger_ShouldHonorStricterCallerMinLevelWhenNoOverrideMatches()
    {
        var config = LoggingConfigurationLoader.LoadFromJsonString(
            """
            {
              "minLevel": "Info",
              "appenders": [
                {
                  "type": "Console",
                  "formatter": "Default",
                  "useColors": false
                }
              ]
            }
            """);

        var factory = LoggingConfigurationLoader.CreateFactory(config);
        var logger = factory.GetLogger("TestLogger", LogLevel.Warning);

        Assert.Multiple(() =>
        {
            Assert.That(logger.IsInfoEnabled(), Is.False);
            Assert.That(logger.IsWarnEnabled(), Is.True);
        });
    }

    /// <summary>
    ///     验证命名空间覆盖级别会优先于调用方传入的默认最小级别，确保覆盖配置保持最高优先级。
    /// </summary>
    [Test]
    public void GetLogger_ShouldPreferNamespaceOverrideOverCallerMinLevel()
    {
        var config = LoggingConfigurationLoader.LoadFromJsonString(
            """
            {
              "minLevel": "Info",
              "appenders": [
                {
                  "type": "Console",
                  "formatter": "Default",
                  "useColors": false
                }
              ],
              "loggerLevels": {
                "MyApp.Services": "Debug"
              }
            }
            """);

        var factory = LoggingConfigurationLoader.CreateFactory(config);
        var logger = factory.GetLogger("MyApp.Services.OrderService", LogLevel.Fatal);

        Assert.Multiple(() =>
        {
            Assert.That(logger.IsDebugEnabled(), Is.True);
            Assert.That(logger.IsTraceEnabled(), Is.False);
        });
    }

    /// <summary>
    ///     验证调用方传入空 logger 名称时，会得到显式的参数异常而不是后续字符串操作的空引用异常。
    /// </summary>
    [Test]
    public void GetLogger_WithNullName_ShouldThrowArgumentNullException()
    {
        var factory = LoggingConfigurationLoader.CreateFactory(new LoggingConfiguration());

        Assert.Throws<ArgumentNullException>(() => factory.GetLogger(null!));
    }

    /// <summary>
    ///     验证工厂释放时会兼容释放未实现 <see cref="IDisposable" /> 的异步 appender，并让既有 logger 观察到已释放状态。
    /// </summary>
    [Test]
    public void Dispose_ShouldDisposeAsyncLogAppenderCreatedFromConfiguration()
    {
        var config = LoggingConfigurationLoader.LoadFromJsonString(
            """
            {
              "appenders": [
                {
                  "type": "Async",
                  "bufferSize": 8,
                  "innerAppender": {
                    "type": "Console",
                    "formatter": "Default",
                    "useColors": false
                  }
                }
              ]
            }
            """);

        var factory = LoggingConfigurationLoader.CreateFactory(config);
        var logger = factory.GetLogger("AsyncLogger");

        logger.Info("dispose-path");

        ((IDisposable)factory).Dispose();

        Assert.Throws<ObjectDisposedException>(() => logger.Info("after-dispose"));
    }
}
