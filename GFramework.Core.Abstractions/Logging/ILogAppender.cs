// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.Logging;

/// <summary>
///     日志输出器接口，负责将日志条目写入特定目标
/// </summary>
public interface ILogAppender : IDisposable
{
    /// <summary>
    ///     追加日志条目
    /// </summary>
    /// <param name="entry">日志条目</param>
    void Append(LogEntry entry);

    /// <summary>
    ///     刷新缓冲区，确保所有日志已写入
    /// </summary>
    void Flush();
}