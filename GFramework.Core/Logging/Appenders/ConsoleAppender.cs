// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using GFramework.Core.Abstractions.Logging;

namespace GFramework.Core.Logging.Appenders;

/// <summary>
///     控制台日志输出器
/// </summary>
public sealed class ConsoleAppender : ILogAppender, IDisposable
{
    private readonly ILogFilter? _filter;
    private readonly ILogFormatter _formatter;
    private readonly bool _useColors;
    private readonly TextWriter _writer;

    /// <summary>
    ///     创建控制台日志输出器
    /// </summary>
    /// <param name="formatter">日志格式化器</param>
    /// <param name="writer">文本写入器（默认为 Console.Out）</param>
    /// <param name="useColors">是否使用颜色（默认为 true）</param>
    /// <param name="filter">日志过滤器（可选）</param>
    public ConsoleAppender(
        ILogFormatter formatter,
        TextWriter? writer = null,
        bool useColors = true,
        ILogFilter? filter = null)
    {
        _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
        _writer = writer ?? Console.Out;
        _useColors = useColors && _writer == Console.Out;
        _filter = filter;
    }

    /// <summary>
    ///     释放资源
    /// </summary>
    public void Dispose()
    {
        _writer.Flush();
    }

    /// <summary>
    ///     追加日志条目到控制台
    /// </summary>
    /// <param name="entry">日志条目</param>
    public void Append(LogEntry entry)
    {
        if (_filter != null && !_filter.ShouldLog(entry))
            return;

        var message = _formatter.Format(entry);

        if (_useColors)
        {
            WriteColored(entry.Level, message);
        }
        else
        {
            _writer.WriteLine(message);
        }
    }

    /// <summary>
    ///     刷新控制台输出
    /// </summary>
    public void Flush()
    {
        _writer.Flush();
    }

    private void WriteColored(LogLevel level, string message)
    {
        var colorCode = GetAnsiColorCode(level);
        _writer.WriteLine($"\x1b[{colorCode}m{message}\x1b[0m");
    }

    private static string GetAnsiColorCode(LogLevel level)
    {
        return level switch
        {
            LogLevel.Trace => "90", // 暗灰色
            LogLevel.Debug => "36", // 青色
            LogLevel.Info => "37", // 白色
            LogLevel.Warning => "33", // 黄色
            LogLevel.Error => "31", // 红色
            LogLevel.Fatal => "35", // 洋红色
            _ => "37"
        };
    }
}