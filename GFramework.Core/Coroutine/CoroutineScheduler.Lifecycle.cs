// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Coroutine.Instructions;
using Microsoft.Extensions.Logging;

namespace GFramework.Core.Coroutine;

public sealed partial class CoroutineScheduler
{
    private static CoroutineResult FailureResult(Exception exception, CoroutineSlot? slot = null)
    {
        var known = slot?.Waiting switch
        {
            JoinCoroutine { IsDone: true, PropagateFailure: true } join => join.Result.Status,
            WaitForCoroutine { IsDone: true } owned => owned.Result.Status,
            ICoroutineCompletionStatusSource source => source.CompletionStatus,
            _ => (slot?.Enumerator as ICoroutineCompletionStatusSource)?.CompletionStatus ?? CoroutineCompletionStatus.Unknown,
        };
        // A faulted Task may hold an OCE, and cleanup may throw OCE. Their explicit fault outcome wins over exception type.
        var cancelled = exception is OperationCanceledException && known != CoroutineCompletionStatus.Faulted;
        return new CoroutineResult(cancelled ? CoroutineCompletionStatus.Cancelled : CoroutineCompletionStatus.Faulted,
            cancelled ? null : exception);
    }

    private static CoroutineResult CombineResults(CoroutineResult? first, CoroutineResult second)
    {
        if (first == null)
        {
            return second;
        }
        var status = first.Value.Status == CoroutineCompletionStatus.Faulted || second.Status == CoroutineCompletionStatus.Faulted
            ? CoroutineCompletionStatus.Faulted
            : first.Value.Status == CoroutineCompletionStatus.Cancelled || second.Status == CoroutineCompletionStatus.Cancelled
                ? CoroutineCompletionStatus.Cancelled : second.Status;
        var exception = first.Value.Exception == null ? second.Exception : second.Exception == null
            ? first.Value.Exception : new AggregateException(first.Value.Exception, second.Exception);
        return new CoroutineResult(status, exception);
    }

    private void RequestTermination(CoroutineSlot slot, CoroutineResult result)
    {
        if (!TryGetSlot(slot.Handle, out _))
        {
            return;
        }
        slot.PendingResult = slot.PendingResult == result ? result : CombineResults(slot.PendingResult, result);
        if (slot.Executing)
        {
            return;
        }
        _terminations.Enqueue(slot.Handle);
        DrainTerminations();
    }

    private void DrainTerminations()
    {
        if (_processingTerminations)
        {
            return;
        }
        _processingTerminations = true;
        try
        {
            // A work queue avoids recursive parent/child teardown and joins executing children before parent publication.
            while (_terminations.TryDequeue(out var handle))
            {
                if (!TryGetSlot(handle, out var slot) || slot.Executing || slot.PendingResult == null)
                {
                    continue;
                }
                if (slot.PendingResult.Value.Status != CoroutineCompletionStatus.Completed)
                {
                    CancelOwnedWork(slot);
                }
                if (slot.Children.Count == 0)
                {
                    FinalizeSlot(slot, slot.PendingResult.Value);
                }
            }
        }
        finally
        {
            _processingTerminations = false;
        }
    }

    private void CancelOwnedWork(CoroutineSlot slot)
    {
        if (!slot.CancellationRequested)
        {
            slot.CancellationRequested = true;
            slot.PendingResult = CleanupResource(slot.PendingResult!.Value, () => slot.CancellationSource?.Cancel());
        }
        if (slot.ChildrenCancellationRequested)
        {
            return;
        }
        slot.ChildrenCancellationRequested = true;
        foreach (var child in slot.Children)
        {
            if (TryGetSlot(child, out var childSlot))
            {
                RequestTermination(childSlot, new CoroutineResult(CoroutineCompletionStatus.Cancelled));
            }
        }
    }

    private void FinalizeSlot(CoroutineSlot slot, CoroutineResult result)
    {
        var handle = slot.Handle;
        // Detach first: user Dispose/cancellation/notification callbacks may recursively control this runtime.
        _slots[handle.SlotIndex] = null;
        _active.Remove(handle);
        _queues[(int)slot.Stage, (int)slot.Priority].Remove(slot.QueueNode!);
        slot.Scope.Unregister(handle);
        RemoveDependency(slot);

        result = CleanupResource(result, () => ReleaseWait(slot));
        result = CleanupResource(result, slot.CancellationRegistration.Dispose);
        if (slot.AdditionalCancellationRegistrations != null)
        {
            foreach (var registration in slot.AdditionalCancellationRegistrations)
            {
                result = CleanupResource(result, registration.Dispose);
            }
            slot.AdditionalCancellationRegistrations.Clear();
        }
        slot.AdditionalCancellationTokens?.Clear();
        var enumerator = slot.Enumerator;
        slot.Enumerator = null;
        if (enumerator != null)
        {
            result = CleanupResource(result, enumerator.Dispose);
        }
        if (slot.CancellationSource != null)
        {
            result = CleanupResource(result, slot.CancellationSource.Dispose);
            slot.CancellationSource = null;
        }
        // Freed only after disposal: cleanup code starting a new task cannot occupy the slot being disposed.
        _freeSlots.Push(handle.SlotIndex);
        RecordOutcome(slot, result);
        DeliverWaiters(slot, result);
        CompleteParent(slot, result);
        if (result.Status == CoroutineCompletionStatus.Faulted && !slot.Parent.IsValid)
        {
            slot.Scope.OnFailure();
        }
        RefreshStatistics();
        NotifyOutcome(slot.Handle, result);
    }

    private static CoroutineResult CleanupResource(CoroutineResult result, Action cleanup)
    {
        try
        {
            cleanup();
            return result;
        }
        catch (Exception exception)
        {
            // Cleanup failures must remain observable even when cancellation initiated termination.
            return CombineResults(result, new CoroutineResult(CoroutineCompletionStatus.Faulted, exception));
        }
    }

    private void ReleaseWait(CoroutineSlot slot)
    {
        RemoveDependency(slot);
        var waiting = slot.Waiting;
        slot.Waiting = null;
        if (waiting is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private void RemoveDependency(CoroutineSlot slot)
    {
        if (TryGetSlot(slot.WaitTarget, out var target))
        {
            target.Waiters.Remove(slot.Handle);
        }
        slot.WaitTarget = default;
    }

    private void RecordOutcome(CoroutineSlot slot, CoroutineResult result)
    {
        _results[slot.Handle] = result;
        _resultOrder.Enqueue(slot.Handle);
        while (_resultOrder.Count > CompletionHistoryLimit)
        {
            _results.Remove(_resultOrder.Dequeue());
        }
        if (_resultSources.Remove(slot.Handle, out var resultSource))
        {
            resultSource.TrySetResult(result);
        }
        if (_statusSources.Remove(slot.Handle, out var statusSource))
        {
            statusSource.TrySetResult(result.Status);
        }
        _statistics?.RecordTermination(result.Status, slot.ExecutionTimeMs,
            Stopwatch.GetElapsedTime(slot.StartedTimestamp).TotalMilliseconds, slot.Priority, slot.Tag);
    }

    private void DeliverWaiters(CoroutineSlot slot, CoroutineResult result)
    {
        foreach (var handle in slot.Waiters)
        {
            if (!TryGetSlot(handle, out var waiter) || waiter.WaitTarget != slot.Handle)
            {
                continue;
            }
            waiter.WaitTarget = default;
            switch (waiter.Waiting)
            {
                case WaitForCoroutine owned:
                    owned.Complete(result);
                    break;
                case JoinCoroutine join:
                    join.Complete(result);
                    break;
            }
        }
        slot.Waiters.Clear();
    }

    private void CompleteParent(CoroutineSlot child, CoroutineResult result)
    {
        if (!TryGetSlot(child.Parent, out var parent))
        {
            return;
        }
        parent.Children.Remove(child.Handle);
        if (parent.PendingResult is { } terminating)
        {
            RequestTermination(parent, result.Status == CoroutineCompletionStatus.Faulted ? result : terminating);
        }
        else if (result.Status == CoroutineCompletionStatus.Faulted && parent.ChildFailurePolicy == CoroutineFailurePolicy.FailFast)
        {
            RequestTermination(parent, result);
        }
        else if (parent.BodyCompleted && parent.Children.Count == 0)
        {
            RequestTermination(parent, new CoroutineResult(CoroutineCompletionStatus.Completed));
        }
    }

    private void NotifyOutcome(CoroutineHandle handle, CoroutineResult result)
    {
        if (result.Exception != null)
        {
            LogSafely(result.Exception, "Coroutine {Handle} failed.", handle);
            if (OnCoroutineException is { } errorHandlers)
            {
                var args = new CoroutineExceptionEventArgs(handle, result.Exception);
                foreach (EventHandler<CoroutineExceptionEventArgs> handler in errorHandlers.GetInvocationList().Cast<EventHandler<CoroutineExceptionEventArgs>>())
                {
                    try
                    {
                        handler(this, args);
                    }
                    catch (Exception exception)
                    {
                        LogSafely(exception, "Coroutine error observer failed.", handle);
                    }
                }
            }
        }
        if (OnCoroutineFinished is { } finishHandlers)
        {
            var args = new CoroutineFinishedEventArgs(handle, result.Status, result.Exception);
            foreach (EventHandler<CoroutineFinishedEventArgs> handler in finishHandlers.GetInvocationList().Cast<EventHandler<CoroutineFinishedEventArgs>>())
            {
                try
                {
                    handler(this, args);
                }
                catch (Exception exception)
                {
                    LogSafely(exception, "Coroutine completion observer failed.", handle);
                }
            }
        }
    }

    private void LogSafely(Exception exception, string message, CoroutineHandle handle)
    {
        try
        {
            if (string.Equals(message, "Coroutine {Handle} failed.", StringComparison.Ordinal))
            {
                _logger.LogError(exception, "Coroutine {Handle} failed.", handle);
            }
            else
            {
                _logger.LogError(exception, "{DiagnosticMessage}", message);
            }
        }
        catch (Exception)
        {
            // Diagnostics cannot interrupt termination when a host-provided logger fails.
        }
    }
}
