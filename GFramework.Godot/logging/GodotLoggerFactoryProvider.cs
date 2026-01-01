using GFramework.Core.Abstractions.logging;

namespace GFramework.Godot.logging;

/// <summary>
/// Godot日志工厂提供程序，用于创建Godot日志记录器实例
/// </summary>
public sealed class GodotLoggerFactoryProvider : ILoggerFactoryProvider
{
    /// <summary>
    /// 创建指定名称和最小日志级别的日志记录器
    /// </summary>
    /// <param name="name">日志记录器的名称</param>
    /// <param name="minLevel">日志记录器的最小日志级别</param>
    /// <returns>返回配置好的Godot日志记录器实例</returns>
    public ILogger CreateLogger(string name, LogLevel minLevel)
        => new GodotLoggerFactory().GetLogger(name, minLevel);
}