// Copyright (c) 2026 GeWuYou
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Logging;

namespace GFramework.Cqrs.Tests.Logging;

/// <summary>
///     供 CQRS 测试项目复用的最小标准日志记录器实现。
///     基于标准 <see cref="ILogger" />，把日志调用捕获到内存中供断言使用。
/// </summary>
/// <remarks>
///     初始化测试日志记录器。
/// </remarks>
/// <param name="name">日志类别名称。</param>
/// <param name="minLevel">最小日志级别；低于该级别的日志调用会被忽略。</param>
public sealed class TestLogger(string? name = null, LogLevel minLevel = LogLevel.Information) : ILogger
{
    private readonly string _name = name ?? nameof(TestLogger);
    private readonly List<LogEntry> _logs = [];
    private readonly Lock _sync = new();
    private LogLevel _minLevel = minLevel;

    /// <summary>
    ///     获取当前日志器的类别名称。
    /// </summary>
    public string Name => _name;

    /// <summary>
    ///     获取或设置最小日志级别。
    /// </summary>
    public LogLevel MinLevel
    {
        get
        {
            lock (_sync)
            {
                return _minLevel;
            }
        }

        set
        {
            lock (_sync)
            {
                _minLevel = value;
            }
        }
    }

    /// <summary>
    ///     获取当前测试期间捕获到的日志条目快照。
    /// </summary>
    public IReadOnlyList<LogEntry> Logs
    {
        get
        {
            lock (_sync)
            {
                return [.. _logs];
            }
        }
    }

    /// <summary>
    ///     判断指定日志级别是否会被当前测试日志器记录。
    /// </summary>
    /// <param name="logLevel">目标日志级别。</param>
    /// <returns>级别不低于 <see cref="MinLevel" /> 且不是 <see cref="LogLevel.None" /> 时返回 <see langword="true" />。</returns>
    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel != LogLevel.None && logLevel >= MinLevel;
    }

    /// <summary>
    ///     创建日志作用域。测试日志器不支持作用域，始终返回 <see langword="null" />。
    /// </summary>
    /// <typeparam name="TState">状态类型。</typeparam>
    /// <param name="state">日志作用域状态。</param>
    /// <returns>始终返回 <see langword="null" />。</returns>
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        return null;
    }

    /// <summary>
    ///     将日志写入内存，供断言使用。
    /// </summary>
    /// <typeparam name="TState">状态类型。</typeparam>
    /// <param name="logLevel">日志级别。</param>
    /// <param name="eventId">事件标识。</param>
    /// <param name="state">日志状态。</param>
    /// <param name="exception">关联异常。</param>
    /// <param name="formatter">把状态格式化为消息的委托。</param>
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        if (!IsEnabled(logLevel))
        {
            return;
        }

        var message = formatter(state, exception);
        lock (_sync)
        {
            _logs.Add(new LogEntry(logLevel, message, exception));
        }
    }

    /// <summary>
    ///     表示单条测试日志记录。
    /// </summary>
    /// <param name="Level">日志级别。</param>
    /// <param name="Message">格式化后的日志消息。</param>
    /// <param name="Exception">关联异常。</param>
    public sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);
}
