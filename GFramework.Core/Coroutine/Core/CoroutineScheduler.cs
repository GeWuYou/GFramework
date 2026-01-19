using System.Collections;
using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Abstractions.Coroutine.Config;
using GFramework.Core.Abstractions.logging;
using GFramework.Core.Coroutine.Errors;

namespace GFramework.Core.Coroutine.Core;

/// <summary>
///     协程调度器实现类，负责协程的调度和执行
/// </summary>
/// <remarks>
///     协程调度器作为ISystem注册到架构中，在Update方法中驱动所有协程
///     支持优先级调度、分组管理和错误处理
/// </remarks>
public class CoroutineScheduler : ICoroutineScheduler
{
    private readonly List<ICoroutineHandle> _activeCoroutines;
    private readonly CoroutineProperties _config;
    private readonly ICoroutineErrorHandler _defaultErrorHandler;
    private readonly float _leakCheckInterval;
    private readonly ILogger _logger;
    private readonly List<ICoroutineHandle> _toAdd;
    private readonly HashSet<ICoroutineHandle> _toRemove;
    private float _leakCheckTimer;
    private int _totalCoroutineCount;

    /// <summary>
    ///     初始化协程调度器的新实例
    /// </summary>
    /// <param name="config">协程配置</param>
    /// <param name="logger">日志记录器</param>
    public CoroutineScheduler(CoroutineProperties config, ILogger logger)
    {
        _config = config ?? new CoroutineProperties();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _defaultErrorHandler = config?.DefaultErrorHandler ?? new DefaultErrorHandler(logger);
        _activeCoroutines = new List<ICoroutineHandle>();
        _toAdd = new List<ICoroutineHandle>();
        _toRemove = new HashSet<ICoroutineHandle>();
        _leakCheckInterval = 60f; // 默认60秒检查一次
        _leakCheckTimer = 0f;
        _totalCoroutineCount = 0;
    }

    /// <summary>
    ///     获取当前活跃的协程数量
    /// </summary>
    public int ActiveCoroutineCount => _activeCoroutines.Count;

    /// <summary>
    ///     获取自启动以来的总协程数量
    /// </summary>
    public int TotalCoroutineCount => _totalCoroutineCount;

    /// <summary>
    ///     创建一个新的协程作用域
    /// </summary>
    /// <param name="name">作用域名称</param>
    /// <param name="priority">作用域内协程的默认优先级</param>
    /// <returns>新创建的协程作用域</returns>
    public ICoroutineScope CreateScope(string name, CoroutinePriority priority = CoroutinePriority.Normal)
    {
        return new CoroutineScope(this, name, priority);
    }

    /// <summary>
    ///     创建一个新的协程分组
    /// </summary>
    /// <param name="name">分组名称</param>
    /// <param name="priority">分组内协程的默认优先级</param>
    /// <returns>新创建的协程分组</returns>
    public ICoroutineGroup CreateGroup(string name, CoroutinePriority priority = CoroutinePriority.Normal)
    {
        return new CoroutineGroup(this, name, priority);
    }

    /// <summary>
    ///     更新所有协程，执行一帧的协程逻辑
    /// </summary>
    /// <param name="deltaTime">距离上一帧的时间间隔（秒）</param>
    public void UpdateCoroutines(float deltaTime)
    {
        // 添加新协程
        if (_toAdd.Count > 0)
        {
            _activeCoroutines.AddRange(_toAdd);
            _toAdd.Clear();
        }

        // 按优先级排序协程
        SortCoroutinesByPriority();

        // 执行所有协程
        for (int i = _activeCoroutines.Count - 1; i >= 0; i--)
        {
            var coroutine = _activeCoroutines[i];

            // 检查作用域是否还活跃
            if (!coroutine.Context.Scope.IsActive)
            {
                coroutine.Cancel();
                _toRemove.Add(coroutine);
                continue;
            }

            // 检查协程是否超时
            if (CheckCoroutineTimeout(coroutine))
            {
                _logger.Warn($"Coroutine '{coroutine.Name}' exceeded timeout, cancelling");
                coroutine.Cancel();
                _toRemove.Add(coroutine);
                continue;
            }

            // 更新协程
            if (!(coroutine is CoroutineHandle handle) || !handle.Update(deltaTime))
            {
                _toRemove.Add(coroutine);
            }
        }

        // 移除已完成的协程
        if (_toRemove.Count > 0)
        {
            _activeCoroutines.RemoveAll(c => _toRemove.Contains(c));
            _toRemove.Clear();
        }

        // 检查协程泄漏
        if (_config.EnableLeakDetection)
        {
            CheckForLeaks(deltaTime);
        }
    }

    /// <summary>
    ///     启动一个协程
    /// </summary>
    /// <param name="routine">协程的迭代器</param>
    /// <param name="context">协程上下文</param>
    /// <returns>协程句柄</returns>
    internal ICoroutineHandle StartCoroutines(IEnumerator routine, CoroutineContext context)
    {
        if (_config.MaxCoroutines > 0 && _activeCoroutines.Count >= _config.MaxCoroutines)
        {
            throw new InvalidOperationException($"Maximum coroutine count ({_config.MaxCoroutines}) reached");
        }

        var handle = new CoroutineHandle(routine, context);
        _toAdd.Add(handle);
        _totalCoroutineCount++;

        if (_config.EnableDebugInfo)
        {
            _logger.Debug($"Started coroutine: {handle.Name} (Priority: {context.Priority})");
        }

        return handle;
    }

    /// <summary>
    ///     按优先级排序协程
    /// </summary>
    private void SortCoroutinesByPriority()
    {
        // 使用稳定的排序算法，保持相同优先级的协程顺序
        _activeCoroutines.Sort((a, b) =>
        {
            // 暂停的协程排在最后
            if (a.IsPaused && !b.IsPaused) return 1;
            if (!a.IsPaused && b.IsPaused) return -1;

            // 按优先级排序
            int priorityDiff = a.Priority.CompareTo(b.Priority);
            if (priorityDiff != 0) return priorityDiff;

            // 相同优先级保持启动顺序
            return 0;
        });
    }

    /// <summary>
    ///     检查协程是否超时
    /// </summary>
    /// <param name="handle">协程句柄</param>
    /// <returns>如果超时返回true，否则返回false</returns>
    private bool CheckCoroutineTimeout(ICoroutineHandle handle)
    {
        if (_config.CoroutineTimeout <= 0) return false;

        var elapsed = (DateTime.UtcNow - handle.Context.StartTime).TotalSeconds;
        return elapsed > _config.CoroutineTimeout;
    }

    /// <summary>
    ///     检查协程泄漏
    /// </summary>
    /// <param name="deltaTime">距离上一帧的时间间隔（秒）</param>
    private void CheckForLeaks(float deltaTime)
    {
        _leakCheckTimer += deltaTime;

        if (_leakCheckTimer < _leakCheckInterval) return;

        _leakCheckTimer = 0f;

        var warningDuration = TimeSpan.FromSeconds(_config.LeakWarningDuration);

        foreach (var coroutine in _activeCoroutines)
        {
            if (coroutine.IsPaused || coroutine.IsCancelled || coroutine.IsDone)
            {
                continue;
            }

            var elapsed = DateTime.UtcNow - coroutine.Context.StartTime;
            if (elapsed > warningDuration)
            {
                _logger.Warn(
                    $"Potential coroutine leak detected: '{coroutine.Name}' has been running for {elapsed.TotalMinutes:F1} minutes");
            }
        }
    }

    /// <summary>
    ///     启动协程（内部方法）
    /// </summary>
    /// <param name="routine">协程的迭代器</param>
    /// <param name="context">协程上下文</param>
    /// <returns>协程句柄</returns>
    internal ICoroutineHandle StartCoroutine(IEnumerator routine, CoroutineContext context)
    {
        return StartCoroutines(routine, context);
    }
}