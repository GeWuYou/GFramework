using GFramework.Core.Abstractions.logging;

namespace GFramework.Core.Abstractions.properties;

/// <summary>
///     日志配置选项类，用于配置日志系统的相关参数
/// </summary>
public sealed class LoggerProperties
{
    /// <summary>
    ///     获取或设置日志工厂提供程序
    ///     可为空，用于提供自定义的日志工厂实现
    /// </summary>
    public ILoggerFactoryProvider LoggerFactoryProvider { get; set; } = null!;
}