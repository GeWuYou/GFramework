// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using System.Text;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Logging.Formatters;

namespace GFramework.Core.Logging.Appenders;

/// <summary>
///     文件日志输出器（线程安全）
/// </summary>
public sealed class FileAppender : ILogAppender, IDisposable
{
    private readonly string _filePath;
    private readonly ILogFilter? _filter;
    private readonly ILogFormatter _formatter;
#if NET9_0_OR_GREATER
    // net9.0 及以上目标使用专用 Lock，以满足分析器对专用同步原语的建议。
    private readonly System.Threading.Lock _lock = new();
#else
    // net8.0 目标仍回退到 object 锁，以保持多目标编译兼容性。
    private readonly object _lock = new();
#endif
    private bool _disposed;
    private StreamWriter? _writer;

    /// <summary>
    ///     创建文件日志输出器
    /// </summary>
    /// <param name="filePath">日志文件路径</param>
    /// <param name="formatter">日志格式化器</param>
    /// <param name="filter">日志过滤器（可选）</param>
    /// <exception cref="ArgumentException">当文件路径为空或无效时抛出</exception>
    /// <exception cref="IOException">当无法创建或打开日志文件时抛出</exception>
    public FileAppender(
        string filePath,
        ILogFormatter? formatter = null,
        ILogFilter? filter = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or whitespace.", nameof(filePath));

        _filePath = filePath;
        _formatter = formatter ?? new DefaultLogFormatter();
        _filter = filter;

        try
        {
            EnsureDirectoryExists();
            InitializeWriter();
        }
        catch
        {
            // 确保在初始化失败时清理资源
            _writer?.Dispose();
            _writer = null;
            throw;
        }
    }

    /// <summary>
    ///     释放资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        lock (_lock)
        {
            _writer?.Flush();
            _writer?.Dispose();
            _writer = null;
            _disposed = true;
        }
    }

    /// <summary>
    ///     追加日志条目到文件
    /// </summary>
    /// <param name="entry">日志条目</param>
    public void Append(LogEntry entry)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(FileAppender));

        if (_filter != null && !_filter.ShouldLog(entry))
            return;

        var message = _formatter.Format(entry);

        lock (_lock)
        {
            _writer?.WriteLine(message);
        }
    }

    /// <summary>
    ///     刷新文件缓冲区
    /// </summary>
    public void Flush()
    {
        lock (_lock)
        {
            _writer?.Flush();
        }
    }

    private void EnsureDirectoryExists()
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private void InitializeWriter()
    {
        _writer = new StreamWriter(_filePath, append: true, Encoding.UTF8)
        {
            AutoFlush = true
        };
    }
}
