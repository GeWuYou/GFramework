using System.Collections;
using GFramework.Core.Abstractions.Coroutine;

namespace GFramework.Core.Coroutine.Yield;

/// <summary>
///     序列执行多个协程的yield指令
/// </summary>
/// <remarks>
///     该指令会按顺序执行所有指定的协程，当前一个协程完成后才启动下一个
///     常用于需要按特定顺序执行一系列任务的情况
/// </remarks>
public class SequenceCoroutine : IYieldInstruction, IEnumerator
{
    private readonly Queue<IEnumerator> _routineQueue;
    private readonly ICoroutineScope _scope;
    private ICoroutineHandle? _currentHandle;

    /// <summary>
    ///     初始化SequenceCoroutine指令的新实例
    /// </summary>
    /// <param name="scope">协程作用域</param>
    /// <param name="routines">要序列执行的协程列表</param>
    /// <exception cref="System.ArgumentNullException">当scope或routines为null时抛出</exception>
    public SequenceCoroutine(ICoroutineScope scope, List<IEnumerator> routines)
    {
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _routineQueue = new Queue<IEnumerator>(routines ?? throw new ArgumentNullException(nameof(routines)));
        _currentHandle = null;
        IsDone = _routineQueue.Count == 0;
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

        // 如果没有当前协程或当前协程已完成，启动下一个
        if (_currentHandle == null || _currentHandle.IsDone)
        {
            if (_routineQueue.Count > 0)
            {
                var nextRoutine = _routineQueue.Dequeue();
                _currentHandle = _scope.Launch(nextRoutine);

                // 注册完成事件
                _currentHandle.OnComplete += h =>
                {
                    if (_routineQueue.Count == 0 && h.IsDone)
                    {
                        IsDone = true;
                    }
                };

                return true;
            }
            else
            {
                // 没有更多协程，标记为完成
                IsDone = true;
                return false;
            }
        }

        return !IsDone;
    }

    /// <summary>
    ///     重置
    /// </summary>
    public void Reset()
    {
        _currentHandle = null;
        IsDone = _routineQueue.Count == 0;
        // 不重置队列，因为协程执行后不能重新使用
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
}