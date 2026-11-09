// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Lifecycle;
using GFramework.Core.Abstractions.State;
using GFramework.Core.Events;
using Microsoft.Extensions.Logging;

namespace GFramework.Core.State;

/// <summary>A standalone state machine with serialized transitions, explicit failures and bounded successful history.</summary>
/// <remarks>Construct states with their dependencies before registration. Callbacks run outside metadata locks and retain
/// the calling synchronization context. Hosts own thread dispatch and states' IDisposable resources.
/// A callback may not await another operation on this machine: reentry fails instead of deadlocking.</remarks>
public class StateMachine : IStateMachine, IAsyncDestroyable
{
#if NET9_0_OR_GREATER
    private readonly System.Threading.Lock _gate = new();
#else
    private readonly object _gate = new();
#endif
    private readonly SemaphoreSlim _operation = new(1, 1);
    private readonly AsyncLocal<CallbackScope?> _callbackScope = new();
    private readonly Dictionary<Type, IState> _states = [];
    private readonly List<IState> _history = [];
    private readonly List<IState> _started = [];
    private readonly int _maxHistorySize;
    private readonly ILogger? _observerLogger;
    private IState? _current;
    private Exception? _fault;
    private bool _frozen;
    private Task? _stopTask;

    /// <summary>Create a machine retaining at most the requested number of successful previous states.</summary>
    /// <param name="maxHistorySize">Nonnegative history capacity; zero disables history.</param>
    /// <param name="observerLogger">Optional diagnostics for isolated state observers; defaults to Trace.</param>
    public StateMachine(int maxHistorySize = 10, ILogger? observerLogger = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxHistorySize);
        _maxHistorySize = maxHistorySize;
        _observerLogger = observerLogger;
    }

    /// <inheritdoc />
    public IState? Current
    {
        get
        {
            lock (_gate)
                return _current;
        }
    }

    /// <inheritdoc />
    public Exception? Fault
    {
        get
        {
            lock (_gate)
                return _fault;
        }
    }

    /// <summary>Raised after a successful transition; observer failures never fault the machine.</summary>
    public event EventHandler<StateChangedEvent>? StateChanged;

    /// <inheritdoc />
    public IStateMachine Register(IState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        lock (_gate)
        {
            EnsureConfigurable();
            if (!_states.TryAdd(state.GetType(), state))
                throw new InvalidOperationException("A concrete state type can only be registered once.");
        }
        return this;
    }

    /// <inheritdoc />
    public bool Unregister<T>() where T : IState
    {
        lock (_gate)
        {
            EnsureConfigurable();
            return _states.Remove(typeof(T));
        }
    }

    /// <inheritdoc />
    public bool IsRegistered<T>() where T : IState
    {
        lock (_gate)
            return _states.ContainsKey(typeof(T));
    }

    /// <inheritdoc />
    public T? GetState<T>() where T : class, IState
    {
        lock (_gate)
            return _states.TryGetValue(typeof(T), out var state) ? state as T : null;
    }

    /// <inheritdoc />
    public IEnumerable<Type> GetRegisteredStateTypes()
    {
        lock (_gate)
            return [.. _states.Keys];
    }

    /// <inheritdoc />
    public IState? GetPreviousState()
    {
        lock (_gate)
            return _history.Count == 0 ? null : _history[^1];
    }

    /// <inheritdoc />
    public IReadOnlyList<IState> GetStateHistory()
    {
        lock (_gate)
            return [.. _history.AsEnumerable().Reverse()];
    }

    /// <inheritdoc />
    public void ClearHistory()
    {
        RejectReentry();
        lock (_gate)
        {
            EnsureRunning();
            if (_operation.CurrentCount == 0)
                throw new InvalidOperationException("Cannot clear history during a state operation.");
            _history.Clear();
        }
    }

    /// <inheritdoc />
    public async Task<bool> CanChangeToAsync<T>(CancellationToken cancellationToken = default) where T : IState
    {
        RejectReentry();
        await _operation.WaitAsync(cancellationToken).ConfigureAwait(true);
        try
        {
            IState? target;
            lock (_gate)
            {
                EnsureRunning();
                _states.TryGetValue(typeof(T), out target);
            }
            if (target is null)
                return false;
            if (ReferenceEquals(Current, target))
                return false;
            return await GuardAsync(Current, target, cancellationToken).ConfigureAwait(true);
        }
        finally { _operation.Release(); }
    }

    /// <inheritdoc />
    public async Task<bool> ChangeToAsync<T>(CancellationToken cancellationToken = default) where T : IState
    {
        RejectReentry();
        await _operation.WaitAsync(cancellationToken).ConfigureAwait(true);
        try
        {
            IState target;
            lock (_gate)
            {
                EnsureRunning();
                _frozen = true;
                if (!_states.TryGetValue(typeof(T), out target!))
                    throw new InvalidOperationException($"State {typeof(T).Name} is not registered.");
            }
            return await TransitionAsync(target, returning: false, cancellationToken).ConfigureAwait(true);
        }
        finally { _operation.Release(); }
    }

    /// <inheritdoc />
    public async Task<bool> GoBackAsync(CancellationToken cancellationToken = default)
    {
        RejectReentry();
        await _operation.WaitAsync(cancellationToken).ConfigureAwait(true);
        try
        {
            IState? target;
            lock (_gate)
            {
                EnsureRunning();
                _frozen = true;
                target = _history.Count == 0 ? null : _history[^1];
            }
            return target is not null &&
                await TransitionAsync(target, returning: true, cancellationToken).ConfigureAwait(true);
        }
        finally { _operation.Release(); }
    }

    /// <summary>Perform guard, exit and entry, then commit history and notify observers.</summary>
    private async Task<bool> TransitionAsync(IState next, bool returning, CancellationToken token)
    {
        var old = Current;
        if (ReferenceEquals(old, next))
            return false;
        token.ThrowIfCancellationRequested();
        if (!await GuardAsync(old, next, token).ConfigureAwait(true))
            return false;
        token.ThrowIfCancellationRequested();
        var callbackScope = new CallbackScope();
        _callbackScope.Value = callbackScope;
        try
        {
            await ExitAsync(old, next, token).ConfigureAwait(true);
            lock (_gate)
            {
                _current = next;
                // Entry can partially acquire resources; terminal cleanup includes the attempted state.
                if (!_started.Exists(state => ReferenceEquals(state, next)))
                    _started.Add(next);
            }
            await EnterAsync(next, old, token).ConfigureAwait(true);
            lock (_gate)
            {
                if (returning)
                    _history.RemoveAt(_history.Count - 1);
                else if (old is not null && _maxHistorySize > 0)
                {
                    _history.Add(old);
                    if (_history.Count > _maxHistorySize)
                        _history.RemoveAt(0);
                }
            }
            ObserverNotification.Notify(StateChanged, this, new StateChangedEvent { OldState = old, NewState = next }, _observerLogger);
            return true;
        }
        catch (Exception error)
        {
            lock (_gate)
                _fault ??= error;
            throw;
        }
        finally
        {
            callbackScope.Deactivate();
            _callbackScope.Value = null;
        }
    }

    /// <summary>Invoke a guard with reentry protection and expose callback failures.</summary>
    private async Task<bool> GuardAsync(IState? current, IState target, CancellationToken token)
    {
        if (current is null)
            return true;
        var callbackScope = new CallbackScope();
        _callbackScope.Value = callbackScope;
        try
        {
            return current is IAsyncState asyncState
                ? await asyncState.CanTransitionToAsync(target, token).ConfigureAwait(true)
                : current.CanTransitionTo(target);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // A canceled guard has not changed state; cancellation must not poison the machine.
            throw;
        }
        catch (Exception error)
        {
            lock (_gate)
                _fault ??= error;
            throw;
        }
        finally
        {
            callbackScope.Deactivate();
            _callbackScope.Value = null;
        }
    }

    /// <summary>Select the asynchronous callback without blocking a host thread.</summary>
    private static Task EnterAsync(IState state, IState? from, CancellationToken token)
    {
        if (state is IAsyncState asyncState)
            return asyncState.OnEnterAsync(from, token);
        state.OnEnter(from);
        return Task.CompletedTask;
    }

    /// <summary>Select the asynchronous exit callback, including terminal exit.</summary>
    private static Task ExitAsync(IState? state, IState? to, CancellationToken token)
    {
        if (state is IAsyncState asyncState)
            return asyncState.OnExitAsync(to, token);
        state?.OnExit(to);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask StopAsync()
    {
        RejectReentry();
        TaskCompletionSource completion;
        lock (_gate)
        {
            if (_stopTask is not null)
                return new ValueTask(_stopTask);
            completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _stopTask = completion.Task;
            _frozen = true;
        }
        _ = CompleteStopAsync(completion);
        return new ValueTask(completion.Task);
    }

    /// <summary>Wait for the active operation, then attempt every started state's business cleanup.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1816",
        Justification = "Business stop and DisposeAsync share one terminal task; finalization is suppressed only after the shared cleanup succeeds.")]
    private async Task CompleteStopAsync(TaskCompletionSource completion)
    {
        await _operation.WaitAsync().ConfigureAwait(true);
        var callbackScope = new CallbackScope();
        _callbackScope.Value = callbackScope;
        var errors = new List<Exception>();
        try
        {
            try
            {
                await ExitAsync(Current, to: null, CancellationToken.None).ConfigureAwait(true);
            }
            catch (Exception error) { errors.Add(error); }
            for (var index = _started.Count - 1; index >= 0; index--)
            {
                try
                {
                    if (_started[index] is IAsyncDestroyable asyncState)
                        await asyncState.DestroyAsync().ConfigureAwait(true);
                    else if (_started[index] is IDestroyable state)
                        state.Destroy();
                }
                catch (Exception error) { errors.Add(error); }
            }
            lock (_gate)
            {
                _current = null;
                _history.Clear();
                _states.Clear();
                _started.Clear();
                StateChanged = null;
            }
        }
        finally
        {
            callbackScope.Deactivate();
            _callbackScope.Value = null;
            _operation.Release();
        }
        if (errors.Count == 0)
        {
            GC.SuppressFinalize(this);
            completion.TrySetResult();
        }
        else
            completion.TrySetException(new AggregateException("State cleanup failed.", errors));
    }

    /// <inheritdoc />
    public ValueTask DestroyAsync() => StopAsync();

    /// <inheritdoc />
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1816",
        Justification = "Successful shared stop completion suppresses finalization while this entrypoint preserves cached terminal task identity.")]
    public ValueTask DisposeAsync() => StopAsync();

    /// <summary>Reject registration changes once an operation has fixed the machine's topology.</summary>
    private void EnsureConfigurable()
    {
        EnsureRunning();
        if (_frozen)
            throw new InvalidOperationException("State registration is frozen.");
    }

    /// <summary>Reject operations after stop starts or a callback has failed.</summary>
    private void EnsureRunning()
    {
        ObjectDisposedException.ThrowIf(_stopTask is not null, nameof(StateMachine));
        if (_fault is not null)
            throw new InvalidOperationException("The state machine has faulted; stop it before creating another.", _fault);
    }

    /// <summary>Waiting for this machine from its own callback would deadlock.</summary>
    private void RejectReentry()
    {
        if (_callbackScope.Value is { IsActive: true })
            throw new InvalidOperationException("State callbacks cannot reenter their own state machine.");
    }
    /// <summary>Inherited execution contexts share this lifetime marker rather than copying a permanent boolean.</summary>
    private sealed class CallbackScope
    {
        private int _active = 1;
        /// <summary>Whether the originating callback still owns the operation boundary.</summary>
        public bool IsActive => Volatile.Read(ref _active) != 0;
        /// <summary>Allow captured child contexts to issue later operations after the callback returns.</summary>
        public void Deactivate() => Interlocked.Exchange(ref _active, 0);
    }

}
