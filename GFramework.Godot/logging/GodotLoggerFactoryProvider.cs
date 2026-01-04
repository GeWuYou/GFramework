using GFramework.Core.Abstractions.logging;

namespace GFramework.Godot.logging;

/// <summary>
///     Godot日志工厂提供程序，用于创建Godot日志记录器实例
/// </summary>
public sealed class GodotLoggerFactoryProvider : ILoggerFactoryProvider
{
    /// <summary>
    ///     获取或设置最小日志级别
    /// </summary>
    public LogLevel MinLevel { get; set; }

    /// <summary>
    ///     创建指定名称的日志记录器实例
    /// </summary>
    /// <param name="name">日志记录器的名称</param>
    /// <returns>返回配置了最小日志级别的Godot日志记录器实例</returns>
    public ILogger CreateLogger(string name)
    {
        return new GodotLoggerFactory().GetLogger(name, MinLevel);
    }
}