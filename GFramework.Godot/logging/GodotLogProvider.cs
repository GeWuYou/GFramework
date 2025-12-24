using GFramework.Core.logging;

namespace GFramework.Godot.logging;

/// <summary>
/// Godot日志提供程序，实现ILogProvider接口，用于创建GodotLogger实例
/// </summary>
public sealed class GodotLogProvider : ILogProvider
{
    /// <summary>
    /// 创建指定类别的日志记录器
    /// </summary>
    /// <param name="category">日志类别名称</param>
    /// <returns>返回GodotLogger实例</returns>
    public ILogger CreateLogger(string category)
        => new GodotLogger(category);
}
