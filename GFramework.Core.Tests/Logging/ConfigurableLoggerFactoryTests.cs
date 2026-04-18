using System.Reflection;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Logging;
using GFramework.Core.Logging.Appenders;

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
    ///     验证调用方传入的默认最小级别会作为配置级别的下限参与最终 logger 级别计算。
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
    ///     验证工厂释放时会兼容释放未实现 <see cref="IDisposable" /> 的 <see cref="AsyncLogAppender" />。
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
        var asyncAppender = GetSingleAsyncAppender(factory);

        logger.Info("dispose-path");

        ((IDisposable)factory).Dispose();

        Assert.That(asyncAppender.IsCompleted, Is.True);
    }

    private static AsyncLogAppender GetSingleAsyncAppender(ILoggerFactory factory)
    {
        var appendersField = factory.GetType().GetField("_appenders", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(appendersField, Is.Not.Null);

        var appenders = appendersField!.GetValue(factory) as ILogAppender[];
        Assert.That(appenders, Is.Not.Null);
        Assert.That(appenders, Has.Length.EqualTo(1));
        Assert.That(appenders![0], Is.TypeOf<AsyncLogAppender>());

        return (AsyncLogAppender)appenders[0];
    }
}
