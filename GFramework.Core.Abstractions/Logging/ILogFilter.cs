// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.Logging;

/// <summary>
///     日志过滤器接口，用于决定是否应该记录某条日志
/// </summary>
public interface ILogFilter
{
    /// <summary>
    ///     判断是否应该记录该日志条目
    /// </summary>
    /// <param name="entry">日志条目</param>
    /// <returns>如果应该记录返回 true，否则返回 false</returns>
    bool ShouldLog(LogEntry entry);
}