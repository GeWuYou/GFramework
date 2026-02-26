using System.IO;
using System.Text;
using GFramework.Core.Abstractions.logging;
using GFramework.Core.logging.formatters;

namespace GFramework.Core.logging.appenders;

/// <summary>
///     滚动文件日志输出器，支持按大小自动轮转日志文件
/// </summary>
public sealed class RollingFileAppender : ILogAppender, IDisposable
{
    private readonly string _baseFilePath;
    private readonly ILogFilter? _filter;
    private readonly ILogFormatter _formatter;
    private readonly object _lock = new();
    private readonly int _maxFileCount;
    private readonly long _maxFileSize;
    private long _currentSize;
    private bool _disposed;
    private StreamWriter? _writer;

    /// <summary>
    ///     创建滚动文件日志输出器
    /// </summary>
    /// <param name="baseFilePath">基础文件路径（例如: logs/app.log）</param>
    /// <param name="maxFileSize">单个文件最大大小（字节），默认 10MB</param>
    /// <param name="maxFileCount">保留的文件数量，默认 5</param>
    /// <param name="formatter">日志格式化器</param>
    /// <param name="filter">日志过滤器（可选）</param>
    public RollingFileAppender(
        string baseFilePath,
        long maxFileSize = 10 * 1024 * 1024,
        int maxFileCount = 5,
        ILogFormatter? formatter = null,
        ILogFilter? filter = null)
    {
        if (string.IsNullOrWhiteSpace(baseFilePath))
            throw new ArgumentException("Base file path cannot be null or whitespace.", nameof(baseFilePath));

        if (maxFileSize <= 0)
            throw new ArgumentException("Max file size must be greater than 0.", nameof(maxFileSize));

        if (maxFileCount <= 0)
            throw new ArgumentException("Max file count must be greater than 0.", nameof(maxFileCount));

        _baseFilePath = baseFilePath;
        _maxFileSize = maxFileSize;
        _maxFileCount = maxFileCount;
        _formatter = formatter ?? new DefaultLogFormatter();
        _filter = filter;

        EnsureDirectoryExists();
        InitializeWriter();
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
            throw new ObjectDisposedException(nameof(RollingFileAppender));

        if (_filter != null && !_filter.ShouldLog(entry))
            return;

        var message = _formatter.Format(entry);
        var messageBytes = Encoding.UTF8.GetByteCount(message) + Environment.NewLine.Length;

        lock (_lock)
        {
            // 检查是否需要轮转
            if (_currentSize + messageBytes > _maxFileSize)
            {
                RollFiles();
            }

            _writer?.WriteLine(message);
            _currentSize += messageBytes;
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

    /// <summary>
    ///     轮转日志文件
    /// </summary>
    private void RollFiles()
    {
        // 关闭当前文件
        _writer?.Flush();
        _writer?.Dispose();
        _writer = null;

        // 删除最旧的文件（如果存在）
        var oldestFile = GetRolledFileName(_maxFileCount - 1);
        if (File.Exists(oldestFile))
        {
            try
            {
                File.Delete(oldestFile);
            }
            catch
            {
                // 忽略删除错误
            }
        }

        // 重命名现有文件: app.log -> app.1.log -> app.2.log -> ...
        for (int i = _maxFileCount - 2; i >= 0; i--)
        {
            var sourceFile = i == 0 ? _baseFilePath : GetRolledFileName(i);
            var targetFile = GetRolledFileName(i + 1);

            if (File.Exists(sourceFile))
            {
                try
                {
                    if (File.Exists(targetFile))
                    {
                        File.Delete(targetFile);
                    }

                    File.Move(sourceFile, targetFile);
                }
                catch
                {
                    // 忽略移动错误
                }
            }
        }

        // 重新初始化写入器
        InitializeWriter();
    }

    /// <summary>
    ///     获取轮转后的文件名
    /// </summary>
    /// <param name="index">文件索引</param>
    /// <returns>轮转后的文件路径</returns>
    private string GetRolledFileName(int index)
    {
        var directory = Path.GetDirectoryName(_baseFilePath);
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(_baseFilePath);
        var extension = Path.GetExtension(_baseFilePath);

        var rolledFileName = $"{fileNameWithoutExt}.{index}{extension}";

        return string.IsNullOrEmpty(directory)
            ? rolledFileName
            : Path.Combine(directory, rolledFileName);
    }

    /// <summary>
    ///     确保目录存在
    /// </summary>
    private void EnsureDirectoryExists()
    {
        var directory = Path.GetDirectoryName(_baseFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    /// <summary>
    ///     初始化写入器
    /// </summary>
    private void InitializeWriter()
    {
        _writer = new StreamWriter(_baseFilePath, append: true, Encoding.UTF8)
        {
            AutoFlush = true
        };

        // 获取当前文件大小
        _currentSize = File.Exists(_baseFilePath) ? new FileInfo(_baseFilePath).Length : 0;
    }
}