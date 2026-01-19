using System.Collections;
using GFramework.Core.Abstractions.Coroutine;

namespace GFramework.Core.Coroutine.Core;

/// <summary>
///     协程分组实现类，用于批量管理多个协程作用域
/// </summary>
/// <remarks>
///     协程分组提供了对多个作用域的批量操作能力，包括暂停、恢复和取消
///     适用于需要同时管理多个相关协程的场景，如场景加载、UI管理等
/// </remarks>
public class CoroutineGroup : ICoroutineGroup
{
    private readonly CoroutinePriority _defaultPriority;
    private readonly CoroutineScheduler _scheduler;
    private readonly HashSet<ICoroutineScope> _scopes;

    /// <summary>
    ///     初始化协程分组的新实例
    /// </summary>
    /// <param name="scheduler">协程调度器</param>
    /// <param name="name">分组名称</param>
    /// <param name="priority">分组内协程的默认优先级</param>
    public CoroutineGroup(CoroutineScheduler scheduler, string name,
        CoroutinePriority priority = CoroutinePriority.Normal)
    {
        _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        Name = name ?? $"Group_{Guid.NewGuid():N}";
        _defaultPriority = priority;
        _scopes = new HashSet<ICoroutineScope>();
        IsActive = true;
    }

    /// <summary>
    ///     获取分组中所有作用域的只读列表
    /// </summary>
    public IReadOnlyList<ICoroutineScope> Scopes => _scopes.ToList().AsReadOnly();

    /// <summary>
    ///     获取分组是否处于活跃状态
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    ///     获取分组的名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     获取分组内所有协程的总数量
    /// </summary>
    public int RunningCount
    {
        get
        {
            int total = 0;
            foreach (var scope in _scopes)
            {
                total += scope.RunningCount;
            }

            return total;
        }
    }

    /// <summary>
    ///     在此分组中启动一个新的协程
    /// </summary>
    /// <param name="routine">协程的迭代器</param>
    /// <param name="priority">协程优先级，使用Inherit继承分组默认优先级</param>
    /// <returns>协程句柄，用于控制协程</returns>
    /// <exception cref="System.InvalidOperationException">当分组不活跃时抛出</exception>
    public ICoroutineHandle Launch(IEnumerator routine, CoroutinePriority priority = CoroutinePriority.Inherit)
    {
        if (!IsActive)
            throw new InvalidOperationException($"Group '{Name}' is not active");

        // 创建一个临时的作用域来启动协程
        var actualPriority = priority == CoroutinePriority.Inherit ? _defaultPriority : priority;
        var scope = new CoroutineScope(_scheduler, $"{Name}_Temp", actualPriority);
        var handle = scope.Launch(routine, actualPriority);

        return handle;
    }

    /// <summary>
    ///     添加一个作用域到分组中
    /// </summary>
    /// <param name="scope">要添加的作用域</param>
    public void AddScope(ICoroutineScope scope)
    {
        if (scope == null)
            throw new ArgumentNullException(nameof(scope));

        _scopes.Add(scope);
    }

    /// <summary>
    ///     从分组中移除一个作用域
    /// </summary>
    /// <param name="scope">要移除的作用域</param>
    public void RemoveScope(ICoroutineScope scope)
    {
        if (scope == null)
            throw new ArgumentNullException(nameof(scope));

        _scopes.Remove(scope);
    }

    /// <summary>
    ///     暂停分组中的所有作用域
    /// </summary>
    public void PauseAll()
    {
        foreach (var scope in _scopes)
        {
            scope.Pause();
        }
    }

    /// <summary>
    ///     恢复分组中的所有作用域
    /// </summary>
    public void ResumeAll()
    {
        foreach (var scope in _scopes)
        {
            scope.Resume();
        }
    }

    /// <summary>
    ///     取消分组中的所有作用域
    /// </summary>
    public void CancelAll()
    {
        foreach (var scope in _scopes)
        {
            scope.Cancel();
        }

        _scopes.Clear();
    }

    /// <summary>
    ///     取消分组及其所有作用域
    /// </summary>
    public void Cancel()
    {
        if (!IsActive) return;

        IsActive = false;
        CancelAll();
    }

    /// <summary>
    ///     暂停分组
    /// </summary>
    public void Pause()
    {
        if (!IsActive) return;

        PauseAll();
    }

    /// <summary>
    ///     恢复分组
    /// </summary>
    public void Resume()
    {
        if (!IsActive) return;

        ResumeAll();
    }

    /// <summary>
    ///     释放资源
    /// </summary>
    public void Dispose()
    {
        Cancel();
    }
}