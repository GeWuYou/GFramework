using GFramework.Core.logging;
using Godot;

namespace GFramework.Godot.logging;

/// <summary>
/// Godot平台的日志记录器实现
/// </summary>
public sealed class GodotLogger : ILogger
{
    private readonly string _category;

    public GodotLogger(string category) => _category = category;
    /// <summary>
    /// 记录日志消息到Godot控制台
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <param name="message">日志消息</param>
    /// <param name="exception">关联的异常对象</param>
    /// <param name="context">日志上下文</param>
    public void Log(LogLevel level, string message, Exception? exception, object? context)
    {
        var prefix = $"[{level}]";

        if (exception != null)
            message += $"\n{exception}";

        switch (level)
        {
            case LogLevel.Error:
            case LogLevel.Fatal:
                GD.PrintErr(prefix, message);
                break;
            case LogLevel.Warning:
                GD.PushWarning($"{prefix} {message}");
                break;
            case LogLevel.Trace:
            case LogLevel.Debug:
            case LogLevel.Info:
            default:
                GD.Print(prefix, message);
                break;
        }
    }

    /// <summary>
    /// 检查指定日志级别是否启用
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <returns>始终返回 true</returns>
    public bool IsEnabled(LogLevel level) => true;

    public void Info(string msg, object? ctx = null)
    {
        Log(LogLevel.Info, msg, null, ctx);
    }

    public void Error(string msg, Exception? ex = null, object? ctx = null)
    {
        Log(LogLevel.Error, msg, ex, ctx);
    }

    public void Debug(string msg, object? ctx = null)
    {
        Log(LogLevel.Debug, msg, null, ctx);
    }

    public void Trace(string msg, object? ctx = null)
    {
        Log(LogLevel.Trace, msg, null, ctx);
    }

    public void Warn(string msg, object? ctx = null)
    {
        Log(LogLevel.Warning, msg, null, ctx);
    }

    public void Fatal(string msg,Exception? ex = null, object? ctx = null)
    {
        Log(LogLevel.Fatal, msg, ex, ctx);
    }
}
