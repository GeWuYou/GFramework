// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Logging;

/// <summary>
///     Appender 配置。
/// </summary>
public sealed class AppenderConfiguration
{
    /// <summary>
    ///     Appender 类型（Console, File, RollingFile, Async）。
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    ///     格式化器类型（Default, Json）。
    /// </summary>
    public string Formatter { get; set; } = "Default";

    /// <summary>
    ///     文件路径（仅用于 File 和 RollingFile）。
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    ///     是否使用颜色（仅用于 Console）。
    /// </summary>
    public bool UseColors { get; set; } = true;

    /// <summary>
    ///     缓冲区大小（仅用于 Async）。
    /// </summary>
    public int BufferSize { get; set; } = 10000;

    /// <summary>
    ///     最大文件大小（仅用于 RollingFile，字节）。
    /// </summary>
    public long MaxFileSize { get; set; } = 10 * 1024 * 1024;

    /// <summary>
    ///     最大文件数量（仅用于 RollingFile）。
    /// </summary>
    public int MaxFileCount { get; set; } = 5;

    /// <summary>
    ///     过滤器配置。
    /// </summary>
    public FilterConfiguration? Filter { get; set; }

    /// <summary>
    ///     内部 Appender 配置（仅用于 Async）。
    /// </summary>
    public AppenderConfiguration? InnerAppender { get; set; }
}
