using GFramework.Core.Abstractions.logging;
using GFramework.Core.logging;

namespace GFramework.Godot.logging;

/// <summary>
///     Godot日志工厂提供程序，用于创建Godot日志记录器实例
/// </summary>
public sealed class GodotLoggerFactoryProvider : ILoggerFactoryProvider
{
    private readonly ILoggerFactory _cachedFactory;

    /// <summary>
    ///     初始化Godot日志记录器工厂提供程序
    /// </summary>
    public GodotLoggerFactoryProvider()
    {
        _cachedFactory = new CachedLoggerFactory(new GodotLoggerFactory());
    }

    /// <summary>
    ///     获取或设置最小日志级别
    /// </summary>
    public LogLevel MinLevel { get; set; }

    /// <summary>
    ///     创建指定名称的日志记录器实例（带缓存）
    /// </summary>
    /// <param name="name">日志记录器的名称</param>
    /// <returns>返回配置了最小日志级别的Godot日志记录器实例</returns>
    public ILogger CreateLogger(string name)
    {
        return _cachedFactory.GetLogger(name, MinLevel);
    }
}