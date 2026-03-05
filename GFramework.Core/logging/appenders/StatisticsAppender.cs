using System.Collections.Concurrent;
using System.Text;
using GFramework.Core.Abstractions.logging;

namespace GFramework.Core.logging.appenders;

/// <summary>
///     日志统计 Appender，用于收集日志指标
///     线程安全：所有方法都是线程安全的
/// </summary>
public sealed class StatisticsAppender : ILogAppender
{
    private readonly ConcurrentDictionary<LogLevel, long> _levelCounts = new();
    private readonly ConcurrentDictionary<string, long> _loggerCounts = new();
    private long _errorCount;
    private DateTime _startTime = DateTime.UtcNow;
    private long _totalCount;

    /// <summary>
    ///     获取总日志数量
    /// </summary>
    public long TotalCount => Interlocked.Read(ref _totalCount);

    /// <summary>
    ///     获取错误日志数量（Error + Fatal）
    /// </summary>
    public long ErrorCount => Interlocked.Read(ref _errorCount);

    /// <summary>
    ///     获取统计开始时间
    /// </summary>
    public DateTime StartTime => _startTime;

    /// <summary>
    ///     获取运行时长
    /// </summary>
    public TimeSpan Uptime => DateTime.UtcNow - _startTime;

    /// <summary>
    ///     获取错误率（错误数 / 总数）
    /// </summary>
    public double ErrorRate
    {
        get
        {
            var total = TotalCount;
            return total == 0 ? 0 : (double)ErrorCount / total;
        }
    }

    /// <summary>
    ///     追加日志条目
    /// </summary>
    public void Append(LogEntry entry)
    {
        // 增加总计数
        Interlocked.Increment(ref _totalCount);

        // 增加级别计数
        _levelCounts.AddOrUpdate(entry.Level, 1, (_, count) => count + 1);

        // 增加日志记录器计数
        _loggerCounts.AddOrUpdate(entry.LoggerName, 1, (_, count) => count + 1);

        // 如果是错误级别，增加错误计数
        if (entry.Level >= LogLevel.Error)
        {
            Interlocked.Increment(ref _errorCount);
        }
    }

    /// <summary>
    ///     刷新缓冲区（此 Appender 无需刷新）
    /// </summary>
    public void Flush()
    {
        // 统计 Appender 不需要刷新
    }

    /// <summary>
    ///     获取指定级别的日志数量
    /// </summary>
    public long GetCountByLevel(LogLevel level)
    {
        return _levelCounts.TryGetValue(level, out var count) ? count : 0;
    }

    /// <summary>
    ///     获取指定日志记录器的日志数量
    /// </summary>
    public long GetCountByLogger(string loggerName)
    {
        return _loggerCounts.TryGetValue(loggerName, out var count) ? count : 0;
    }

    /// <summary>
    ///     获取所有级别的日志数量
    /// </summary>
    public IReadOnlyDictionary<LogLevel, long> GetLevelCounts()
    {
        return new Dictionary<LogLevel, long>(_levelCounts);
    }

    /// <summary>
    ///     获取所有日志记录器的日志数量
    /// </summary>
    public IReadOnlyDictionary<string, long> GetLoggerCounts()
    {
        return new Dictionary<string, long>(_loggerCounts);
    }

    /// <summary>
    ///     重置所有统计数据
    /// </summary>
    public void Reset()
    {
        Interlocked.Exchange(ref _totalCount, 0);
        Interlocked.Exchange(ref _errorCount, 0);
        _levelCounts.Clear();
        _loggerCounts.Clear();
        _startTime = DateTime.UtcNow;
    }

    /// <summary>
    ///     生成统计报告
    /// </summary>
    public string GenerateReport()
    {
        var report = new StringBuilder();
        report.AppendLine("=== 日志统计报告 ===");
        report.AppendLine($"统计时间: {_startTime:yyyy-MM-dd HH:mm:ss} - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine($"运行时长: {Uptime}");
        report.AppendLine($"总日志数: {TotalCount}");
        report.AppendLine($"错误日志数: {ErrorCount}");
        report.AppendLine($"错误率: {ErrorRate:P2}");
        report.AppendLine();

        report.AppendLine("按级别统计:");
        foreach (var level in Enum.GetValues<LogLevel>())
        {
            var count = GetCountByLevel(level);
            if (count > 0)
            {
                var percentage = (double)count / TotalCount;
                report.AppendLine($"  {level}: {count} ({percentage:P2})");
            }
        }

        report.AppendLine();
        report.AppendLine("按日志记录器统计 (Top 10):");
        var topLoggers = _loggerCounts
            .OrderByDescending(kvp => kvp.Value)
            .Take(10);

        foreach (var (logger, count) in topLoggers)
        {
            var percentage = (double)count / TotalCount;
            report.AppendLine($"  {logger}: {count} ({percentage:P2})");
        }

        return report.ToString();
    }
}