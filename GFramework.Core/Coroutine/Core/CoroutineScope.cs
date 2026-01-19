using System.Collections;
using GFramework.Core.Abstractions.Coroutine;

namespace GFramework.Core.Coroutine.Core;

/// <summary>
///     协程作用域实现类，定义协程的生命周期边界
/// </summary>
/// <remarks>
///     协程作用域是协程的生命周期边界，所有在作用域内启动的协程都会随着作用域的销毁而自动取消
///     类似于Kotlin的CoroutineScope概念
/// </remarks>
public class CoroutineScope : ICoroutineScope
{
    private readonly CoroutinePriority _defaultPriority;
    private readonly HashSet<ICoroutineHandle> _pausedCoroutines;
    private readonly HashSet<ICoroutineHandle> _runningCoroutines;
    private readonly CoroutineScheduler _scheduler;
    private bool _isActive;

    /// <summary>
    ///     初始化协程作用域的新实例
    /// </summary>
    /// <param name="scheduler">协程调度器</param>
    /// <param name="name">作用域名称</param>
    /// <param name="priority">作用域内协程的默认优先级</param>
    public CoroutineScope(CoroutineScheduler scheduler, string name,
        CoroutinePriority priority = CoroutinePriority.Normal)
    {
        _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        Name = name ?? $"Scope_{Guid.NewGuid():N}";
        _defaultPriority = priority;
        _runningCoroutines = new HashSet<ICoroutineHandle>();
        _pausedCoroutines = new HashSet<ICoroutineHandle>();
        _isActive = true;
    }

    /// <summary>
    ///     获取作用域是否处于活跃状态
    /// </summary>
    public bool IsActive => _isActive;

    /// <summary>
    ///     获取作用域的名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     获取作用域内正在运行的协程数量
    /// </summary>
    public int RunningCount => _runningCoroutines.Count + _pausedCoroutines.Count;

    /// <summary>
    ///     在此作用域中启动一个新的协程
    /// </summary>
    /// <param name="routine">协程的迭代器</param>
    /// <param name="priority">协程优先级，使用Inherit继承作用域默认优先级</param>
    /// <returns>协程句柄，用于控制协程</returns>
    /// <exception cref="System.InvalidOperationException">当作用域不活跃时抛出</exception>
    public ICoroutineHandle Launch(IEnumerator routine, CoroutinePriority priority = CoroutinePriority.Inherit)
    {
        if (!_isActive)
            throw new InvalidOperationException($"Scope '{Name}' is not active");

        // 解析优先级
        var actualPriority = priority == CoroutinePriority.Inherit ? _defaultPriority : priority;

        // 创建协程上下文
        var context = new CoroutineContext(this, _scheduler, this, actualPriority, Name);

        // 启动协程
        var handle = _scheduler.StartCoroutine(routine, context);

        // 注册到作用域
        _runningCoroutines.Add(handle);

        // 注册完成事件，自动清理
        handle.OnComplete += OnCoroutineComplete;
        handle.OnError += OnCoroutineError;

        return handle;
    }

    /// <summary>
    ///     取消作用域及其所有协程
    /// </summary>
    public void Cancel()
    {
        if (!_isActive) return;

        _isActive = false;

        // 取消所有运行中的协程
        var allCoroutines = new List<ICoroutineHandle>();
        allCoroutines.AddRange(_runningCoroutines);
        allCoroutines.AddRange(_pausedCoroutines);

        foreach (var coroutine in allCoroutines)
        {
            coroutine.Cancel();
        }

        _runningCoroutines.Clear();
        _pausedCoroutines.Clear();
    }

    /// <summary>
    ///     暂停作用域内的所有协程
    /// </summary>
    public void Pause()
    {
        if (!_isActive) return;

        // 将所有运行中的协程移动到暂停列表
        var toPause = _runningCoroutines.ToList();
        foreach (var coroutine in toPause)
        {
            if (coroutine.Status == CoroutineStatus.Running)
            {
                coroutine.Pause();
                _pausedCoroutines.Add(coroutine);
                _runningCoroutines.Remove(coroutine);
            }
        }
    }

    /// <summary>
    ///     恢复作用域内的所有协程
    /// </summary>
    public void Resume()
    {
        if (!_isActive) return;

        // 将所有暂停的协程移动到运行列表
        var toResume = _pausedCoroutines.ToList();
        foreach (var coroutine in toResume)
        {
            if (coroutine.IsPaused)
            {
                coroutine.Resume();
                _runningCoroutines.Add(coroutine);
                _pausedCoroutines.Remove(coroutine);
            }
        }
    }

    /// <summary>
    ///     释放资源
    /// </summary>
    public void Dispose()
    {
        Cancel();
    }

    /// <summary>
    ///     协程完成事件处理
    /// </summary>
    /// <param name="handle">完成的协程句柄</param>
    private void OnCoroutineComplete(ICoroutineHandle handle)
    {
        _runningCoroutines.Remove(handle);
        _pausedCoroutines.Remove(handle);
    }

    /// <summary>
    ///     协程错误事件处理
    /// </summary>
    /// <param name="handle">发生错误的协程句柄</param>
    /// <param name="exception">发生的异常</param>
    private void OnCoroutineError(ICoroutineHandle handle, Exception exception)
    {
        _runningCoroutines.Remove(handle);
        _pausedCoroutines.Remove(handle);
    }
}