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

using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Logging;

namespace GFramework.Cqrs.Tests.Logging;

/// <summary>
///     供 CQRS 测试项目复用的最小日志记录器实现。
/// </summary>
public sealed class TestLogger : AbstractLogger
{
    private readonly List<LogEntry> _logs = [];

    /// <summary>
    ///     初始化测试日志记录器。
    /// </summary>
    /// <param name="name">日志名称。</param>
    /// <param name="minLevel">最小日志级别。</param>
    public TestLogger(string? name = null, LogLevel minLevel = LogLevel.Info) : base(name, minLevel)
    {
    }

    /// <summary>
    ///     获取当前测试期间捕获到的日志条目。
    /// </summary>
    public IReadOnlyList<LogEntry> Logs => _logs;

    /// <summary>
    ///     将日志写入内存，供断言使用。
    /// </summary>
    /// <param name="level">日志级别。</param>
    /// <param name="message">日志消息。</param>
    /// <param name="exception">关联异常。</param>
    protected override void Write(LogLevel level, string message, Exception? exception)
    {
        _logs.Add(new LogEntry(level, message, exception));
    }

    /// <summary>
    ///     表示单条测试日志记录。
    /// </summary>
    /// <param name="Level">日志级别。</param>
    /// <param name="Message">日志消息。</param>
    /// <param name="Exception">关联异常。</param>
    public sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);
}
