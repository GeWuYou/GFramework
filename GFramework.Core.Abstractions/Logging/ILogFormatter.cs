// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.Logging;

/// <summary>
///     日志格式化器接口，用于将日志条目格式化为字符串
/// </summary>
public interface ILogFormatter
{
    /// <summary>
    ///     将日志条目格式化为字符串
    /// </summary>
    /// <param name="entry">日志条目</param>
    /// <returns>格式化后的日志字符串</returns>
    string Format(LogEntry entry);
}