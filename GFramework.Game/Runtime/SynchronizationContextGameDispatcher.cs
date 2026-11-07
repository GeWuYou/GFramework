// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Runtime;

namespace GFramework.Game.Runtime;

/// <summary>Dispatches game operations to an explicitly supplied host synchronization context.</summary>
/// <remarks>Create this dispatcher on the host thread. Pending callbacks are cancellable; started callbacks always complete before ownership can be released.</remarks>
public sealed class SynchronizationContextGameDispatcher : IGameDispatcher
{
    private readonly SynchronizationContext _context;

    /// <summary>Creates a dispatcher for a host-owned context.</summary>
    /// <param name="context">The context that serializes host callbacks.</param>
    public SynchronizationContextGameDispatcher(SynchronizationContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public Task InvokeAsync(Func<Task> callback, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(callback);
        cancellationToken.ThrowIfCancellationRequested();
        if (ReferenceEquals(SynchronizationContext.Current, _context))
            return callback();

        var operation = new PendingOperation(callback, cancellationToken, ExecutionContext.Capture());
        try
        {
            _context.Post(static state => ((PendingOperation)state!).Start(), operation);
        }
        catch (Exception exception)
        {
            operation.Fail(exception);
        }
        return operation.Completion;
    }

    /// <summary>Arbitrates cancellation and start without completing ahead of an active callback.</summary>
    private sealed class PendingOperation
    {
        private readonly Func<Task> _callback;
        private readonly CancellationToken _token;
        private readonly ExecutionContext? _executionContext;
        private readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly CancellationTokenRegistration _registration;
        private int _state;

        /// <summary>Creates a pending callback and registers cancellation before posting.</summary>
        public PendingOperation(Func<Task> callback, CancellationToken token, ExecutionContext? executionContext)
        {
            _callback = callback;
            _token = token;
            _executionContext = executionContext;
            _registration = token.Register(() =>
            {
                if (Interlocked.CompareExchange(ref _state, 2, 0) == 0)
                    _completion.TrySetCanceled(token);
            });
        }

        /// <summary>Gets the observed operation task.</summary>
        public Task Completion => CompleteAsync();

        /// <summary>Disposes the cancellation registration only after the operation has settled.</summary>
        private async Task CompleteAsync()
        {
            try { await _completion.Task.ConfigureAwait(false); }
            finally { _registration.Dispose(); }
        }

        /// <summary>Enters the caller's execution context so queue reentrancy markers propagate.</summary>
        public void Start()
        {
            if (Interlocked.CompareExchange(ref _state, 1, 0) != 0)
                return;
            if (_executionContext is null)
                Execute();
            else
                ExecutionContext.Run(_executionContext, static state => ((PendingOperation)state!).Execute(), this);
        }

        /// <summary>Observes asynchronous callbacks at the synchronization-context boundary.</summary>
        private async void Execute()
        {
            try
            {
                await _callback().ConfigureAwait(true);
                _completion.TrySetResult();
            }
            catch (OperationCanceledException exception) { _completion.TrySetCanceled(exception.CancellationToken); }
            catch (Exception exception) { _completion.TrySetException(exception); }
            finally { Interlocked.Exchange(ref _state, 2); }
        }

        /// <summary>Reports posting failures without interfering with an already running callback.</summary>
        public void Fail(Exception exception)
        {
            if (Interlocked.CompareExchange(ref _state, 2, 0) == 0)
                _completion.TrySetException(exception);
        }
    }
}
