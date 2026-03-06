using System.Text;
using GFramework.Core.Abstractions.events;

namespace GFramework.Core.events;

/// <summary>
///     事件统计信息实现类
///     线程安全：使用 Interlocked 操作确保计数器的原子性
/// </summary>
public sealed class EventStatistics : IEventStatistics
{
    private readonly Dictionary<string, int> _listenerCountByType = new();
    private readonly object _lock = new();
    private readonly Dictionary<string, long> _publishCountByType = new();
    private long _totalFailed;
    private long _totalHandled;
    private long _totalPublished;

    /// <inheritdoc />
    public long TotalPublished => Interlocked.Read(ref _totalPublished);

    /// <inheritdoc />
    public long TotalHandled => Interlocked.Read(ref _totalHandled);

    /// <inheritdoc />
    public long TotalFailed => Interlocked.Read(ref _totalFailed);

    /// <inheritdoc />
    public int ActiveEventTypes
    {
        get
        {
            lock (_lock)
            {
                return _publishCountByType.Count;
            }
        }
    }

    /// <inheritdoc />
    public int ActiveListeners
    {
        get
        {
            lock (_lock)
            {
                return _listenerCountByType.Values.Sum();
            }
        }
    }

    /// <inheritdoc />
    public long GetPublishCount(string eventType)
    {
        lock (_lock)
        {
            return _publishCountByType.TryGetValue(eventType, out var count) ? count : 0;
        }
    }

    /// <inheritdoc />
    public int GetListenerCount(string eventType)
    {
        lock (_lock)
        {
            return _listenerCountByType.TryGetValue(eventType, out var count) ? count : 0;
        }
    }

    /// <inheritdoc />
    public void Reset()
    {
        Interlocked.Exchange(ref _totalPublished, 0);
        Interlocked.Exchange(ref _totalHandled, 0);
        Interlocked.Exchange(ref _totalFailed, 0);

        lock (_lock)
        {
            _publishCountByType.Clear();
            _listenerCountByType.Clear();
        }
    }

    /// <inheritdoc />
    public string GenerateReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== 事件统计报告 ===");
        sb.AppendLine($"总发布数: {TotalPublished}");
        sb.AppendLine($"总处理数: {TotalHandled}");
        sb.AppendLine($"总失败数: {TotalFailed}");
        sb.AppendLine($"活跃事件类型: {ActiveEventTypes}");
        sb.AppendLine($"活跃监听器: {ActiveListeners}");

        lock (_lock)
        {
            if (_publishCountByType.Count > 0)
            {
                sb.AppendLine("\n按事件类型统计（发布次数）:");
                foreach (var kvp in _publishCountByType.OrderByDescending(x => x.Value))
                    sb.AppendLine($"  {kvp.Key}: {kvp.Value}");
            }

            if (_listenerCountByType.Count > 0)
            {
                sb.AppendLine("\n按事件类型统计（监听器数量）:");
                foreach (var kvp in _listenerCountByType.OrderByDescending(x => x.Value))
                    sb.AppendLine($"  {kvp.Key}: {kvp.Value}");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    ///     记录事件发布
    /// </summary>
    /// <param name="eventType">事件类型名称</param>
    public void RecordPublish(string eventType)
    {
        Interlocked.Increment(ref _totalPublished);

        lock (_lock)
        {
            _publishCountByType.TryGetValue(eventType, out var count);
            _publishCountByType[eventType] = count + 1;
        }
    }

    /// <summary>
    ///     记录事件处理
    /// </summary>
    public void RecordHandle()
    {
        Interlocked.Increment(ref _totalHandled);
    }

    /// <summary>
    ///     记录事件处理失败
    /// </summary>
    public void RecordFailure()
    {
        Interlocked.Increment(ref _totalFailed);
    }

    /// <summary>
    ///     更新事件类型的监听器数量
    /// </summary>
    /// <param name="eventType">事件类型名称</param>
    /// <param name="count">监听器数量</param>
    public void UpdateListenerCount(string eventType, int count)
    {
        lock (_lock)
        {
            if (count > 0)
            {
                _listenerCountByType[eventType] = count;
            }
            else
            {
                _listenerCountByType.Remove(eventType);
            }
        }
    }
}