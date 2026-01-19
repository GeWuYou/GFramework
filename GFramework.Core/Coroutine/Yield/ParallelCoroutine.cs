using System.Collections;
using GFramework.Core.Abstractions.Coroutine;

namespace GFramework.Core.Coroutine.Yield;

/// <summary>
///     并行执行多个协程的yield指令
/// </summary>
/// <remarks>
///     该指令会同时启动所有指定的协程，并在所有协程都完成时标记为完成
///     常用于需要并行执行多个独立任务的情况
/// </remarks>
public class ParallelCoroutine : IYieldInstruction, IEnumerator
{
    private readonly List<ICoroutineHandle> _handles;
    private readonly List<IEnumerator> _routines;
    private readonly ICoroutineScope _scope;
    private bool _isStarted;

    /// <summary>
    ///     初始化ParallelCoroutine指令的新实例
    /// </summary>
    /// <param name="scope">协程作用域</param>
    /// <param name="routines">要并行执行的协程列表</param>
    /// <exception cref="System.ArgumentNullException">当scope或routines为null时抛出</exception>
    public ParallelCoroutine(ICoroutineScope scope, List<IEnumerator> routines)
    {
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _routines = routines ?? throw new ArgumentNullException(nameof(routines));
        _handles = new List<ICoroutineHandle>();
        _isStarted = false;
        IsDone = false;
    }

    /// <summary>
    ///     获取当前对象
    /// </summary>
    public object Current => this;

    /// <summary>
    ///     移动到下一个
    /// </summary>
    /// <returns>如果未完成返回true，否则返回false</returns>
    public bool MoveNext()
    {
        if (IsDone) return false;

        // 首次调用时启动所有协程
        if (!_isStarted)
        {
            StartAllRoutines();
            _isStarted = true;
        }

        // 检查是否所有协程都已完成
        return !CheckAllDone();
    }

    /// <summary>
    ///     重置
    /// </summary>
    public void Reset()
    {
        _isStarted = false;
        IsDone = false;
        _handles.Clear();
    }

    /// <summary>
    ///     获取指令是否已完成
    /// </summary>
    public bool IsDone { get; private set; }

    /// <summary>
    ///     更新指令状态
    /// </summary>
    /// <param name="deltaTime">距离上一帧的时间间隔（秒）</param>
    public void Update(float deltaTime)
    {
        // MoveNext内部处理更新
    }

    /// <summary>
    ///     启动所有协程
    /// </summary>
    private void StartAllRoutines()
    {
        foreach (var routine in _routines)
        {
            if (routine == null) continue;

            var handle = _scope.Launch(routine);

            // 注册完成事件
            handle.OnComplete += h =>
            {
                if (h.IsDone)
                {
                    CheckAllDone();
                }
            };

            _handles.Add(handle);
        }
    }

    /// <summary>
    ///     检查是否所有协程都已完成
    /// </summary>
    /// <returns>如果全部完成返回true，否则返回false</returns>
    private bool CheckAllDone()
    {
        bool allDone = true;
        foreach (var handle in _handles)
        {
            if (!handle.IsDone)
            {
                allDone = false;
                break;
            }
        }

        if (allDone && !IsDone)
        {
            IsDone = true;
        }

        return allDone;
    }
}