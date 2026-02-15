using GFramework.Core.Abstractions.logging;
using GFramework.Core.logging;
using Godot;

namespace GFramework.Godot.logging;

/// <summary>
///     Godot平台的日志记录器实现。
///     该类继承自 <see cref="AbstractLogger"/>，用于在 Godot 引擎中输出日志信息。
///     支持不同日志级别的输出，并根据级别调用 Godot 的相应方法。
/// </summary>
/// <param name="name">日志记录器的名称，默认为根日志记录器名称。</param>
/// <param name="minLevel">最低日志级别，默认为 <see cref="LogLevel.Info"/>。</param>
public sealed class GodotLogger(
    string? name = null,
    LogLevel minLevel = LogLevel.Info) : AbstractLogger(name ?? RootLoggerName, minLevel)
{
    /// <summary>
    ///     写入日志的核心方法。
    ///     格式化日志消息并根据日志级别调用 Godot 的输出方法。
    /// </summary>
    /// <param name="level">日志级别。</param>
    /// <param name="message">日志消息内容。</param>
    /// <param name="exception">可选的异常信息。</param>
    protected override void Write(LogLevel level, string message, Exception? exception)
    {
        // 构造时间戳和日志前缀
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var levelStr = level.ToString().ToUpper().PadRight(7);
        var logPrefix = $"[{timestamp}] {levelStr} [{Name()}]";

        // 添加异常信息到日志消息中
        if (exception != null) message += "\n" + exception;

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