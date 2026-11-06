// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;

namespace GFramework.Core.Abstractions.Logging;

/// <summary>
///     日志条目，包含完整的日志信息。
///     作为输出引擎（appender/formatter/filter 管线）的统一事件记录；
///     级别枚举直接采用 <see cref="Microsoft.Extensions.Logging.LogLevel"/>，
///     与标准日志契约保持同一语义，不再维护自有级别映射。
/// </summary>
/// <param name="Timestamp">条目产生时间（本地时间）。</param>
/// <param name="Level">标准日志级别。</param>
/// <param name="LoggerName">产生条目的记录器类别名。</param>
/// <param name="Message">已格式化的消息文本。</param>
/// <param name="Exception">关联异常；无异常时为 null。</param>
/// <param name="Properties">调用方提供的结构化属性；可为 null。</param>
public sealed record LogEntry(
    DateTime Timestamp,
    LogLevel Level,
    string LoggerName,
    string Message,
    Exception? Exception,
    IReadOnlyDictionary<string, object?>? Properties)
{
    /// <summary>
    ///     获取用于格式化输出的属性集合，无属性时返回空字典。
    /// </summary>
    /// <returns>调用方提供的属性集合；不会为 null。</returns>
    public IReadOnlyDictionary<string, object?> GetAllProperties()
    {
        if (Properties == null || Properties.Count == 0)
            return EmptyProperties;

        return Properties;
    }

    /// <summary>共享的空属性字典，避免每次格式化分配。</summary>
    private static readonly IReadOnlyDictionary<string, object?> EmptyProperties =
        new Dictionary<string, object?>(StringComparer.Ordinal);
}
