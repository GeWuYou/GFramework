using GFramework.Core.Abstractions.logging;
using GFramework.Core.logging;
using Godot;

namespace GFramework.Godot.logging;

/// <summary>
///     Godot平台的日志记录器实现
/// </summary>
public sealed class GodotLogger(
    string? name = null,
    LogLevel minLevel = LogLevel.Info) : AbstractLogger(name ?? RootLoggerName, minLevel)
{
    protected override void Write(LogLevel level, string message, Exception? exception)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var levelStr = level.ToString().ToUpper().PadRight(7);
        var logPrefix = $"[{timestamp}] {levelStr} [{Name()}]";

        // 添加异常信息
        if (exception != null)
        {
            message += "\n" + exception;
        }

        var logMessage = $"{logPrefix} {message}";

        // 根据日志级别选择 Godot 输出方法
        switch (level)
        {
            case LogLevel.Fatal:
                GD.PushError(logMessage);
                break;
            case LogLevel.Error:
                GD.PrintErr(logMessage);
                break;
            case LogLevel.Warning:
                GD.PushWarning(logMessage);
                break;
            default: // Trace / Debug / Info
                GD.Print(logMessage);
                break;
        }
    }
}