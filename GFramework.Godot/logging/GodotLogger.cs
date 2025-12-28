using GFramework.Core.Abstractions.logging;
using GFramework.Core.logging;
using Godot;

namespace GFramework.Godot.logging;

/// <summary>
///     Godot平台的日志记录器实现
/// </summary>
public sealed class GodotLogger(
    string? name = null,
    LogLevel minLevel = LogLevel.Info) : AbstractLogger(name ?? ILogger.RootLoggerName, minLevel)
{
    protected override void Write(LogLevel level, string message, Exception? exception)
    {
        var prefix = $"[{level.ToString().ToUpper()}][{Name()}]";

        // 将异常信息追加到日志消息中
        if (exception != null) message += "\n" + exception;

        // 根据日志级别选择不同的输出方法
        switch (level)
        {
            case LogLevel.Error:
            case LogLevel.Fatal:
                GD.PrintErr($"{prefix} {message}");
                break;

            case LogLevel.Warning:
                GD.PushWarning($"{prefix} {message}");
                break;
            default:
                GD.Print($"{prefix} {message}");
                break;
        }
    }
}