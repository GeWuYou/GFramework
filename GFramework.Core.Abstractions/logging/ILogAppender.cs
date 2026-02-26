namespace GFramework.Core.Abstractions.logging;

/// <summary>
///     日志输出器接口，负责将日志条目写入特定目标
/// </summary>
public interface ILogAppender
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