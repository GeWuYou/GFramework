using System;
using System.Collections.Generic;
using System.Threading;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Logging;

namespace GFramework.Core.Tests.Logging;

/// <summary>
///     表示供日志相关测试复用的内存日志记录器。
/// </summary>
/// <remarks>
///     并发写入会通过内部锁串行化；<see cref="Logs" /> 每次返回快照，避免断言观察到正在被修改的可变集合。
/// </remarks>
public sealed class TestLogger : AbstractLogger
{
    private readonly List<LogEntry> _logs = new();
    private readonly Lock _sync = new();

    /// <summary>
    ///     初始化 <see cref="TestLogger" /> 的新实例。
    /// </summary>
    /// <param name="name">日志记录器的名称；未指定时沿用基类默认行为。</param>
    /// <param name="minLevel">允许写入的最小日志级别。</param>
    public TestLogger(string? name = null, LogLevel minLevel = LogLevel.Info) : base(name, minLevel)
    {
    }

    /// <summary>
    ///     获取按写入顺序保存的日志条目快照。
    /// </summary>
    public IReadOnlyList<LogEntry> Logs
    {
        get
        {
            lock (_sync)
            {
                return _logs.ToArray();
            }
        }
    }

    /// <summary>
    ///     将日志信息追加到内存列表，供断言读取。
    /// </summary>
    /// <param name="level">日志级别。</param>
    /// <param name="message">日志消息。</param>
    /// <param name="exception">相关异常；没有异常时为 <see langword="null" />。</param>
    protected override void Write(LogLevel level, string message, Exception? exception)
    {
        lock (_sync)
        {
            _logs.Add(new LogEntry(level, message, exception));
        }
    }

    /// <summary>
    ///     表示单个日志条目的不可变快照。
    /// </summary>
    /// <param name="Level">日志级别。</param>
    /// <param name="Message">日志消息。</param>
    /// <param name="Exception">相关异常；没有异常时为 <see langword="null" />。</param>
    public sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);
}
