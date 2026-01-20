using System.Collections;
using GFramework.Game.Abstractions.coroutine;

namespace GFramework.Game.coroutine;

public class CoroutineScope : ICoroutineScope, IDisposable
{
    private readonly List<CoroutineScope> _children = new();
    private readonly CoroutineScope _parent;
    private readonly HashSet<CoroutineHandle> _runningCoroutines = new();
    private readonly CoroutineScheduler _scheduler;

    private bool _isActive = true;

    public CoroutineScope(CoroutineScheduler scheduler, string name = null, CoroutineScope parent = null)
    {
        _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        _parent = parent;
        _parent?._children.Add(this);
        Name = name ?? $"Scope_{GetHashCode()}";
    }

    public string Name { get; }
    public bool IsActive => _isActive;

    public void Cancel()
    {
        if (!_isActive) return;

        _isActive = false;

        foreach (var child in _children.ToList())
            child.Cancel();

        foreach (var handle in _runningCoroutines.ToList())
            handle.Cancel();

        _runningCoroutines.Clear();
    }

    public void Dispose() => Cancel();

    public CoroutineHandle Launch(IEnumerator routine)
    {
        if (!_isActive)
            throw new InvalidOperationException($"Scope '{Name}' is not active");

        var context = new CoroutineContext(this, _scheduler, this);
        var handle = _scheduler.StartCoroutine(routine, context);

        _runningCoroutines.Add(handle);
        handle.OnComplete += () => _runningCoroutines.Remove(handle);
        handle.OnError += (ex) => _runningCoroutines.Remove(handle);

        return handle;
    }
}