using System.Collections;
using GFramework.Game.Abstractions.coroutine;

namespace GFramework.Game.coroutine;

/// <summary>
/// 协程句柄类，用于管理和控制协程的执行状态
/// 实现了IYieldInstruction和ICoroutineHandle接口
/// </summary>
public class CoroutineHandle : IYieldInstruction, ICoroutineHandle
{
    /// <summary>
    /// 存储协程执行栈的堆栈结构
    /// </summary>
    private readonly Stack<IEnumerator> _stack = new();

    /// <summary>
    /// 当前等待执行的指令
    /// </summary>
    private IYieldInstruction? _waitingInstruction;

    /// <summary>
    /// 初始化一个新的协程句柄实例
    /// </summary>
    /// <param name="routine">要执行的枚举器协程</param>
    /// <param name="context">协程上下文环境</param>
    /// <param name="waitingInstruction">初始等待的指令</param>
    internal CoroutineHandle(IEnumerator routine, CoroutineContext context, IYieldInstruction? waitingInstruction)
    {
        _stack.Push(routine);
        Context = context;
        _waitingInstruction = waitingInstruction;
    }

    /// <summary>
    /// 获取协程的上下文环境
    /// </summary>
    public CoroutineContext Context { get; }

    /// <summary>
    /// 获取协程句柄的上下文环境（接口实现）
    /// </summary>
    ICoroutineContext ICoroutineHandle.Context => Context;

    /// <summary>
    /// 获取协程是否已被取消的标志
    /// </summary>
    public bool IsCancelled { get; private set; }

    /// <summary>
    /// 协程完成时触发的事件
    /// </summary>
    public event Action? OnComplete;

    /// <summary>
    /// 协程发生错误时触发的事件
    /// </summary>
    public event Action<Exception>? OnError;

    /// <summary>
    /// 取消协程的执行
    /// </summary>
    public void Cancel()
    {
        if (IsDone) return;
        IsDone = true;
        IsCancelled = true;
        _stack.Clear();
        _waitingInstruction = null;
        OnComplete?.Invoke();
    }

    /// <summary>
    /// 获取协程是否已完成的标志
    /// </summary>
    public bool IsDone { get; private set; }

    /// <summary>
    /// 更新协程执行状态（接口实现）
    /// </summary>
    /// <param name="deltaTime">时间增量</param>
    void IYieldInstruction.Update(float deltaTime)
    {
        InternalUpdate(deltaTime);
    }

    /// <summary>
    /// 内部更新协程执行逻辑
    /// </summary>
    /// <param name="deltaTime">时间增量</param>
    /// <returns>如果协程仍在运行返回true，否则返回false</returns>
    private bool InternalUpdate(float deltaTime)
    {
        if (IsDone) return false;

        // 检查并更新当前等待的指令
        if (_waitingInstruction != null)
        {
            _waitingInstruction.Update(deltaTime);
            if (!_waitingInstruction.IsDone) return true;
            _waitingInstruction = null;
        }

        if (_stack.Count == 0)
        {
            Complete();
            return false;
        }

        try
        {
            var current = _stack.Peek();
            if (current.MoveNext())
            {
                ProcessYieldValue(current.Current);
                return true;
            }

            _stack.Pop();
            return _stack.Count > 0 || !CompleteCheck();
        }
        catch (Exception ex)
        {
            HandleError(ex);
            return false;
        }
    }

    /// <summary>
    /// 处理协程中yield返回的值，根据类型决定如何处理
    /// </summary>
    /// <param name="yielded">协程yield返回的对象</param>
    private void ProcessYieldValue(object yielded)
    {
        switch (yielded)
        {
            case CoroutineHandle otherHandle:
                _waitingInstruction = otherHandle;
                break;
            case IEnumerator nested:
                _stack.Push(nested);
                break;
            case IYieldInstruction instruction:
                _waitingInstruction = instruction;
                break;
            case null:
                break;
            default:
                throw new InvalidOperationException($"Unsupported yield type: {yielded.GetType()}");
        }
    }

    /// <summary>
    /// 检查协程是否完成并进行相应处理
    /// </summary>
    /// <returns>如果协程已完成返回true，否则返回false</returns>
    private bool CompleteCheck()
    {
        if (_stack.Count == 0) Complete();
        return IsDone;
    }

    /// <summary>
    /// 标记协程完成并清理相关资源
    /// </summary>
    private void Complete()
    {
        if (IsDone) return;
        IsDone = true;
        _stack.Clear();
        _waitingInstruction = null;
        OnComplete?.Invoke();
    }

    /// <summary>
    /// 处理协程执行过程中发生的异常
    /// </summary>
    /// <param name="ex">发生的异常</param>
    private void HandleError(Exception ex)
    {
        IsDone = true;
        _stack.Clear();
        _waitingInstruction = null;
        OnError?.Invoke(ex);
    }
}