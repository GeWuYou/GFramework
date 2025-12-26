using GFramework.Core.logging;

namespace GFramework.Godot.logging;

/// <summary>
///     Godot日志工厂类，用于创建Godot平台专用的日志记录器实例
/// </summary>
public class GodotLoggerFactory : ILoggerFactory
{
    /// <summary>
    ///     获取指定名称的日志记录器实例
    /// </summary>
    /// <param name="name">日志记录器的名称</param>
    /// <returns>返回GodotLogger类型的日志记录器实例</returns>
    public ILogger GetLogger(string name)
    {
        return new GodotLogger(name);
    }
}