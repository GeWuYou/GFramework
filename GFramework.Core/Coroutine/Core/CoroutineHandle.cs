using System.Collections;
using GFramework.Core.Abstractions.Coroutine;

namespace GFramework.Core.Coroutine.Core;

/// <summary>
///     协程句柄实现类，提供对单个协程的控制和状态管理
/// </summary>
/// <remarks>
///     协程句柄负责协程的执行状态、优先级和事件通知
///     支持嵌套协程，使用Stack来管理协程调用链
/// </remarks>
public class CoroutineHandle : ICoroutineHandle
{
    private readonly Stack<IEnumerator> _stack;
    private IYieldInstruction? _currentInstruction;
    private bool _isHandlingError;
    private CoroutinePriority _priority;
    private CoroutineStatus _status;

    /// <summary>
    ///     初始化协程句柄的新实例
    /// </summary>
    /// <param name="routine">协程的迭代器</param>
    /// <param name="context">协程上下文</param>
    public CoroutineHandle(IEnumerator routine, CoroutineContext context)
    {
        _stack = new Stack<IEnumerator>();
        _stack.Push(routine);
        Context = context;
        Name = context.Name;
        _priority = context.Priority;
        _status = CoroutineStatus.Pending;
    }

    /// <summary>
    ///     获取协程的上下文
    /// </summary>
    public CoroutineContext Context { get; }

    /// <summary>
    ///     获取协程是否已完成
    /// </summary>
    public bool IsDone => _status is CoroutineStatus.Completed or CoroutineStatus.Cancelled or CoroutineStatus.Error;

    /// <summary>
    ///     获取协程是否已被取消
    /// </summary>
    public bool IsCancelled => _status == CoroutineStatus.Cancelled;

    /// <summary>
    ///     获取协程是否处于暂停状态
    /// </summary>
    public bool IsPaused => _status == CoroutineStatus.Paused;

    /// <summary>
    ///     获取协程的状态
    /// </summary>
    public CoroutineStatus Status => _status;

    /// <summary>
    ///     获取协程的优先级
    /// </summary>
    public CoroutinePriority Priority => _priority;

    /// <summary>
    ///     获取协程的名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     协程完成事件
    /// </summary>
    public event Action<ICoroutineHandle>? OnComplete;

    /// <summary>
    ///     协程错误事件
    /// </summary>
    public event Action<ICoroutineHandle, Exception>? OnError;

    /// <summary>
    ///     取消协程的执行
    /// </summary>
    public void Cancel()
    {
        if (IsDone)
        {
            return;
        }

        _status = CoroutineStatus.Cancelled;
        _stack.Clear();
        _currentInstruction = null;

        // 触发完成事件
        try
        {
            OnComplete?.Invoke(this);
        }
        catch (Exception)
        {
            // 忽略事件处理器中的异常
        }
    }

    /// <summary>
    ///     暂停协程的执行
    /// </summary>
    public void Pause()
    {
        if (IsDone)
        {
            return;
        }

        _status = CoroutineStatus.Paused;
    }

    /// <summary>
    ///     恢复协程的执行
    /// </summary>
    public void Resume()
    {
        if (IsDone || !IsPaused)
        {
            return;
        }

        _status = CoroutineStatus.Running;
    }

    /// <summary>
    ///     设置协程的优先级
    /// </summary>
    /// <param name="priority">新的优先级</param>
    public void SetPriority(CoroutinePriority priority)
    {
        _priority = priority;
    }

    /// <summary>
    ///     更新协程状态
    /// </summary>
    /// <param name="deltaTime">距离上一帧的时间间隔（秒）</param>
    /// <returns>如果协程仍在运行返回true，否则返回false</returns>
    internal bool Update(float deltaTime)
    {
        if (_status == CoroutineStatus.Pending)
        {
            _status = CoroutineStatus.Running;
        }

        // 检查是否被暂停
        if (_status == CoroutineStatus.Paused)
        {
            return true;
        }

        // 检查是否已完成
        if (_status is CoroutineStatus.Completed or CoroutineStatus.Cancelled or CoroutineStatus.Error)
        {
            return false;
        }

        try
        {
            return ExecuteCoroutine(deltaTime);
        }
        catch (Exception ex)
        {
            HandleError(ex);
            return false;
        }
    }

    /// <summary>
    ///     执行协程逻辑
    /// </summary>
    /// <param name="deltaTime">距离上一帧的时间间隔（秒）</param>
    /// <returns>如果协程仍在运行返回true，否则返回false</returns>
    private bool ExecuteCoroutine(float deltaTime)
    {
        // 检查栈是否为空
        if (_stack.Count == 0)
        {
            Complete();
            return false;
        }

        // 获取当前协程
        var current = _stack.Peek();
        bool shouldContinue = current.MoveNext();

        if (shouldContinue)
        {
            // 处理yield值
            ProcessYieldValue(deltaTime);
            return true;
        }
        else
        {
            // 当前协程完成，出栈
            _stack.Pop();

            // 检查是否还有协程
            if (_stack.Count == 0)
            {
                Complete();
                return false;
            }

            // 继续执行上层协程
            return true;
        }
    }

    /// <summary>
    ///     处理协程的yield值
    /// </summary>
    /// <param name="deltaTime">距离上一帧的时间间隔（秒）</param>
    private void ProcessYieldValue(float deltaTime)
    {
        var current = _stack.Peek();
        var yielded = current.Current;

        switch (yielded)
        {
            case null:
                // null - 直接继续
                break;

            case IEnumerator nestedRoutine:
                // 嵌套协程 - 压栈
                _stack.Push(nestedRoutine);
                break;

            case IYieldInstruction instruction:
                // Yield指令 - 更新指令状态
                _currentInstruction = instruction;
                instruction.Update(deltaTime);
                if (instruction.IsDone)
                {
                    _currentInstruction = null;
                }

                break;

            case ICoroutineHandle otherHandle:
                // 等待另一个协程
                if (otherHandle.IsDone)
                {
                    // 已完成，继续
                }

                break;

            default:
                // 其他类型 - 忽略或转换为协程
                if (yielded is IEnumerable enumerable)
                {
                    _stack.Push(enumerable.GetEnumerator());
                }

                break;
        }
    }

    /// <summary>
    ///     完成协程
    /// </summary>
    private void Complete()
    {
        if (_status is CoroutineStatus.Completed or CoroutineStatus.Cancelled or CoroutineStatus.Error)
        {
            return;
        }

        _status = CoroutineStatus.Completed;
        _stack.Clear();
        _currentInstruction = null;

        // 触发完成事件
        try
        {
            OnComplete?.Invoke(this);
        }
        catch (Exception)
        {
            // 忽略事件处理器中的异常
        }
    }

    /// <summary>
    ///     处理协程错误
    /// </summary>
    /// <param name="exception">发生的异常</param>
    private void HandleError(Exception exception)
    {
        if (_isHandlingError)
        {
            // 防止递归错误处理
            return;
        }

        _isHandlingError = true;
        _status = CoroutineStatus.Error;
        _stack.Clear();
        _currentInstruction = null;

        try
        {
            // 触发错误事件
            OnError?.Invoke(this, exception);
        }
        catch (Exception)
        {
            // 忽略事件处理器中的异常
        }
        finally
        {
            _isHandlingError = false;
        }
    }
}