// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Collections;
using System.Runtime.ExceptionServices;
using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Coroutine.Instructions;

namespace GFramework.Core.Coroutine;

/// <summary>Owns parallel inputs until transferred to child tasks, including disposal before first advancement.</summary>
internal sealed class CoroutineChildrenEnumerator : IEnumerator<IYieldInstruction>, ICoroutineCompletionStatusSource
{
    private CoroutineContext? _context;
    private readonly IEnumerator<IYieldInstruction>?[] _pending;
    private readonly List<CoroutineHandle> _handles = new();
    private readonly List<JoinCoroutine> _joins = new();
    private IYieldInstruction? _current;
    private int _waitIndex;
    private bool _started;
    private bool _completed;
    private bool _disposed;
    private CoroutineCompletionStatus _terminalStatus;

    /// <summary>Validates all entries before accepting ownership of the collection.</summary>
    internal CoroutineChildrenEnumerator(CoroutineContext context, IReadOnlyList<IEnumerator<IYieldInstruction>> coroutines)
    {
        _context = context;
        if (coroutines.Any(coroutine => coroutine == null))
        {
            throw new ArgumentException("Child coroutine entries must not be null.", nameof(coroutines));
        }
        _pending = coroutines.Cast<IEnumerator<IYieldInstruction>?>().ToArray();
    }

    /// <inheritdoc />
    public IYieldInstruction Current => _current!;
    /// <inheritdoc />
    object IEnumerator.Current => Current;
    /// <inheritdoc />
    CoroutineCompletionStatus ICoroutineCompletionStatusSource.CompletionStatus => _terminalStatus;

    /// <inheritdoc />
    public bool MoveNext()
    {
        if (_disposed)
        {
            return false;
        }
        try
        {
            if (!_started)
            {
                _started = true;
                StartChildren();
            }
            if (_waitIndex < _joins.Count)
            {
                _current = _joins[_waitIndex++];
                return true;
            }
            ThrowChildFailures();
            foreach (var join in _joins)
            {
                _terminalStatus = join.Result.Status;
                join.Result.ThrowIfFailed();
            }
            _completed = true;
            _terminalStatus = CoroutineCompletionStatus.Completed;
            Dispose();
            return false;
        }
        catch (Exception exception)
        {
            ExceptionDispatchInfo.Capture(DisposeAll(exception)!).Throw();
            return false;
        }
    }

    /// <inheritdoc />
    public void Reset() => throw new NotSupportedException("Owned child operations cannot be reset.");

    /// <summary>Disposes unstarted inputs and stops the operation's own children on early termination.</summary>
    public void Dispose()
    {
        var exception = DisposeAll();
        if (exception != null)
        {
            ExceptionDispatchInfo.Capture(exception).Throw();
        }
    }

    private void StartChildren()
    {
        var context = _context!;
        for (var index = 0; index < _pending.Length; index++)
        {
            var handle = context.RunChild(_pending[index]);
            _pending[index] = null;
            _handles.Add(handle);
            _joins.Add(context.Scheduler.Join(handle));
        }
    }

    private void ThrowChildFailures()
    {
        List<Exception>? failures = null;
        foreach (var join in _joins)
        {
            if (join.Result.Status == CoroutineCompletionStatus.Faulted)
            {
                _terminalStatus = CoroutineCompletionStatus.Faulted;
                (failures ??= new List<Exception>()).Add(join.Result.Exception
                    ?? new InvalidOperationException("A child failed without retaining an exception."));
            }
        }
        if (failures is { Count: 1 })
        {
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        }
        if (failures is { Count: > 1 })
        {
            throw new AggregateException(failures);
        }
    }

    private Exception? DisposeAll(Exception? primaryException = null)
    {
        if (_disposed)
        {
            return primaryException;
        }
        _disposed = true;
        _current = null;
        List<Exception>? failures = primaryException == null ? null : new List<Exception> { primaryException };
        for (var index = 0; index < _pending.Length; index++)
        {
            var pending = _pending[index];
            _pending[index] = null;
            try
            {
                pending?.Dispose();
            }
            catch (Exception exception)
            {
                _terminalStatus = CoroutineCompletionStatus.Faulted;
                (failures ??= new List<Exception>()).Add(exception);
            }
        }
        CancelRemainingChildren(ref failures);
        _joins.Clear();
        _handles.Clear();
        _context = null;
        return failures switch
        {
            null => null,
            { Count: 1 } => failures[0],
            _ => new AggregateException(failures)
        };
    }

    private void CancelRemainingChildren(ref List<Exception>? failures)
    {
        var context = _context!;
        if (!_completed)
        {
            foreach (var handle in _handles)
            {
                // Only new cleanup failures produced by our own cancellation belong here; already observed faults
                // have propagated through the parent policy or ThrowChildFailures and must not be duplicated.
                if (context.Scheduler.IsCoroutineAlive(handle))
                {
                    context.Scheduler.Kill(handle);
                    if (context.Scheduler.TryGetResult(handle, out var result) && result.Exception != null)
                    {
                        _terminalStatus = CoroutineCompletionStatus.Faulted;
                        (failures ??= new List<Exception>()).Add(result.Exception);
                    }
                }
            }
        }
    }
}
