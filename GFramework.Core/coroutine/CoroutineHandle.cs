using System.Collections;
using GFramework.Core.Abstractions.coroutine;

namespace GFramework.Core.coroutine;

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
    /// 是否由父协程控制（被yield）
    /// </summary>
    private bool _isManagedByParent;

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
    /// 标记协程由父协程管理
    /// </summary>
    internal void MarkAsManagedByParent()
    {
        _isManagedByParent = true;
    }

    /// <summary>
    /// 检查协程是否由父协程管理
    /// </summary>
    internal bool IsManagedByParent => _isManagedByParent;

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

        // 如果有等待指令，先更新等待指令
        if (_waitingInstruction != null)
        {
            _waitingInstruction.Update(deltaTime);
            
            // 如果等待指令还未完成，继续等待
            if (!_waitingInstruction.IsDone)
            {
                return true;
            }
            
            // 等待指令已完成，清除它
            _waitingInstruction = null;
        }

        // 每帧只推进一步（执行一个 MoveNext）
        if (_stack.Count > 0)
        {
            try
            {
                var current = _stack.Peek();
                bool hasNext = current.MoveNext();
                
                if (!hasNext)
                {
                    // 当前枚举器已完成，弹出栈
                    _stack.Pop();
                    
                    // 如果栈为空，协程完成
                    if (_stack.Count == 0)
                    {
                        Complete();
                        return false;
                    }
                    
                    // 否则继续执行下一个枚举器（在下一帧）
                    return true;
                }
                
                // MoveNext() 返回 true，有下一个值
                var yielded = current.Current;
                var needsWait = ProcessYieldValue(yielded);

                // 如果需要等待，则暂停执行
                if (needsWait)
                {
                    return true;
                }

                // 如果不需要等待（yield null 或嵌套协程），继续处理
                // 处理 yield null 的情况：yield null 后需要再调用一次 MoveNext 才能知道是否完成
                if (yielded == null)
                {
                    // yield null 意味着等待一帧，但协程可能还没有完成
                    // 需要再次检查是否还有更多步骤
                    bool stillHasNext = current.MoveNext();
                    if (!stillHasNext)
                    {
                        // 协程确实完成了
                        _stack.Pop();
                        if (_stack.Count == 0)
                        {
                            Complete();
                            return false;
                        }
                    }
                    else
                    {
                        // 还有更多内容，yield 出来的值需要处理
                        yielded = current.Current;
                        needsWait = ProcessYieldValue(yielded);
                        if (needsWait)
                        {
                            return true;
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                HandleError(ex);
                return false;
            }
        }

        // 栈为空，协程完成
        Complete();
        return false;
    }

    /// <summary>
    /// 处理协程中yield返回的值，根据类型决定如何处理
    /// </summary>
    /// <param name="yielded">协程yield返回的对象</param>
    /// <returns>如果需要等待返回true，否则返回false</returns>
    private bool ProcessYieldValue(object yielded)
    {
        switch (yielded)
        {
            case CoroutineHandle otherHandle:
                // 处理 yield return CoroutineHandle
                if (otherHandle.IsDone)
                {
                    // 子协程已完成，不需要等待
                    return false;
                }
                
                // 标记子协程由父协程管理
                otherHandle.MarkAsManagedByParent();
                _waitingInstruction = otherHandle;
                return true; // 需要等待子协程完成

            case IEnumerator nested:
                // 处理 yield return IEnumerator（嵌套协程）
                _stack.Push(nested);
                return false; // 压入嵌套协程，在下一帧继续执行

            case IYieldInstruction instruction:
                // 处理 yield return IYieldInstruction
                _waitingInstruction = instruction;
                return true; // 需要等待指令完成

            case null:
                // 处理 yield return null（等待一帧）
                return false; // null 立即继续，但会返回 true 让协程继续执行

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