using System.Collections;
using GFramework.Game.Abstractions.coroutine;

namespace GFramework.Game.coroutine;

public class CoroutineScheduler : ICoroutineScheduler
{
    private readonly List<CoroutineHandle> _active = new();
    private readonly List<CoroutineHandle> _toAdd = new();
    private readonly HashSet<CoroutineHandle> _toRemove = new();

    public int ActiveCount => _active.Count;

    public void Update(float deltaTime)
    {
        if (_toAdd.Count > 0)
        {
            _active.AddRange(_toAdd);
            _toAdd.Clear();
        }

        for (var i = _active.Count - 1; i >= 0; i--)
        {
            var c = _active[i];

            if (!c.Context.Scope.IsActive)
            {
                c.Cancel();
                _toRemove.Add(c);
                continue;
            }

            if (!c.Update(deltaTime))
                _toRemove.Add(c);
        }

        if (_toRemove.Count <= 0) return;
        {
            _active.RemoveAll(c => _toRemove.Contains(c));
            _toRemove.Clear();
        }
    }

    internal CoroutineHandle StartCoroutine(IEnumerator routine, CoroutineContext context)
    {
        var handle = new CoroutineHandle(routine, context);
        _toAdd.Add(handle);
        return handle;
    }

    internal void RemoveCoroutine(CoroutineHandle handle) => _toRemove.Add(handle);
}