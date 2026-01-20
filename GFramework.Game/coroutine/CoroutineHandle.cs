using System.Collections;
using GFramework.Game.Abstractions.coroutine;

namespace GFramework.Game.coroutine;

public class CoroutineHandle : IYieldInstruction
{
    private readonly Stack<IEnumerator> _stack = new();
    private IYieldInstruction? _waitingInstruction;

    internal CoroutineHandle(IEnumerator routine, CoroutineContext context, IYieldInstruction? waitingInstruction)
    {
        _stack.Push(routine);
        Context = context;
        _waitingInstruction = waitingInstruction;
    }

    public CoroutineContext Context { get; }
    public bool IsCancelled { get; private set; }
    public bool IsDone { get; private set; }

    void IYieldInstruction.Update(float deltaTime)
    {
        InternalUpdate(deltaTime);
    }

    public event Action? OnComplete;
    public event Action<Exception>? OnError;

    private bool InternalUpdate(float deltaTime)
    {
        if (IsDone) return false;

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

    private void ProcessYieldValue(object yielded)
    {
        switch (yielded)
        {
            case null:
                break;
            case IEnumerator nested:
                _stack.Push(nested);
                break;
            // ✅ 将更具体的类型放在前面
            case CoroutineHandle otherHandle:
                _waitingInstruction = otherHandle;
                break;
            case IYieldInstruction instruction:
                _waitingInstruction = instruction;
                break;
            default:
                throw new InvalidOperationException($"Unsupported yield type: {yielded.GetType()}");
        }
    }

    private bool CompleteCheck()
    {
        if (_stack.Count == 0) Complete();
        return IsDone;
    }

    private void Complete()
    {
        if (IsDone) return;
        IsDone = true;
        _stack.Clear();
        _waitingInstruction = null;
        OnComplete?.Invoke();
    }

    private void HandleError(Exception ex)
    {
        IsDone = true;
        _stack.Clear();
        _waitingInstruction = null;
        OnError?.Invoke(ex);
    }

    public void Cancel()
    {
        if (IsDone) return;
        IsDone = true;
        IsCancelled = true;
        _stack.Clear();
        _waitingInstruction = null;
    }
}