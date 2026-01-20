using System.Collections;
using GFramework.Game.Abstractions.coroutine;

namespace GFramework.Game.coroutine;

/// <summary>
/// 协程作用域管理器，用于管理和控制协程的生命周期
/// </summary>
public sealed class CoroutineScope : ICoroutineScope, IDisposable
{
    private readonly List<CoroutineScope> _children = new();
    private readonly HashSet<CoroutineHandle> _runningCoroutines = new();
    private readonly CoroutineScheduler _scheduler;

    private bool _isActive = true;

    /// <summary>
    /// 初始化新的协程作用域实例
    /// </summary>
    /// <param name="scheduler">协程调度器</param>
    /// <param name="name">作用域名称，如果为null则自动生成</param>
    /// <param name="parent">父级作用域，如果为null则表示顶级作用域</param>
    public CoroutineScope(CoroutineScheduler scheduler, string? name = null, CoroutineScope? parent = null)
    {
        _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        parent?._children.Add(this);
        Name = name ?? $"Scope_{GetHashCode()}";
    }

    /// <summary>
    /// 获取作用域名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 获取当前作用域是否处于活动状态
    /// </summary>
    public bool IsActive => _isActive;

    /// <summary>
    /// 取消当前作用域及其所有子作用域中的所有运行中协程
    /// </summary>
    public void Cancel()
    {
        if (!_isActive) return;

        _isActive = false;

        // 递归取消所有子作用域
        foreach (var child in _children)
            child.Cancel();

        // 取消当前作用域中所有运行中的协程
        foreach (var handle in _runningCoroutines)
            handle.Cancel();

        _runningCoroutines.Clear();
    }

    /// <summary>
    /// 启动一个新的协程（接口实现）
    /// </summary>
    /// <param name="routine">要执行的协程枚举器</param>
    /// <returns>协程句柄</returns>
    ICoroutineHandle ICoroutineScope.Launch(IEnumerator routine)
    {
        return Launch(routine);
    }

    /// <summary>
    /// 释放资源并取消所有协程
    /// </summary>
    public void Dispose() => Cancel();

    /// <summary>
    /// 启动一个新的协程
    /// </summary>
    /// <param name="routine">要执行的协程枚举器</param>
    /// <returns>协程句柄</returns>
    public CoroutineHandle Launch(IEnumerator routine)
    {
        if (!_isActive)
            throw new InvalidOperationException($"Scope '{Name}' is not active");

        var context = new CoroutineContext(this, _scheduler, this);
        var handle = _scheduler.StartCoroutine(routine, context);

        // 添加到运行中协程集合
        _runningCoroutines.Add(handle);

        // 注册完成事件以从集合中移除句柄
        handle.OnComplete += () => _runningCoroutines.Remove(handle);

        // 注册错误事件以从集合中移除句柄
        handle.OnError += (_) => _runningCoroutines.Remove(handle);

        return handle;
    }
}