// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Collections;
using System.Runtime.ExceptionServices;
using GFramework.Core.Abstractions.Coroutine;

namespace GFramework.Godot.Coroutine;

/// <summary>An owned iterator whose intrinsic node lifetime is consumed by every Core coroutine start boundary.</summary>
/// <remarks>
/// Native subscriptions are attached and released on the Godot thread. External cancellation only touches
/// the managed cancellation source. The scheduler consumes its intrinsic token for direct, scoped, factory
/// and child starts, so long waits and scene pause do not hide node exit.
/// </remarks>
internal sealed class NodeBoundCoroutine : IEnumerator<IYieldInstruction>, ICoroutineCancellationSource
{
    private readonly IEnumerator<IYieldInstruction> _inner;
    private readonly List<WeakReference<Node>> _nodes = new();
    private readonly CancellationTokenSource _cancellation = new();
    private readonly Action _onNodeExiting;
    private CancellationTokenRegistration _innerCancellationRegistration;
    private bool _disposed;

    /// <summary>Transfers iterator ownership and subscribes each valid node exactly once.</summary>
    /// <param name="inner">The underlying iterator.</param>
    /// <param name="nodes">Any node's exit cancels this iterator.</param>
    internal NodeBoundCoroutine(IEnumerator<IYieldInstruction> inner, IReadOnlyCollection<Node> nodes)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _onNodeExiting = _cancellation.Cancel;
        CancellationToken = _cancellation.Token;
        try
        {
            foreach (var node in nodes.Distinct())
            {
                if (!Timing.IsNodeAlive(node))
                {
                    _cancellation.Cancel();
                    continue;
                }
                _nodes.Add(new WeakReference<Node>(node));
                node.TreeExiting += _onNodeExiting;
            }
            if (inner is ICoroutineCancellationSource source)
            {
                // Every wrapper exposes the union of its own nodes and any intrinsic lifetime beneath it.
                _innerCancellationRegistration = source.CancellationToken.Register(_cancellation.Cancel);
            }
        }
        catch (Exception setupFailure)
        {
            try
            {
                Dispose();
            }
            catch (Exception cleanupFailure)
            {
                throw new AggregateException("Node lifetime setup and cleanup failed.", setupFailure, cleanupFailure);
            }
            throw;
        }
    }

    /// <summary>Gets the underlying instruction without hiding scheduler-recognized instruction types.</summary>
    public IYieldInstruction Current => _inner.Current;

    /// <inheritdoc />
    object IEnumerator.Current => Current;

    /// <summary>Gets the intrinsic lifetime independently from whichever facade, scope or context starts this iterator.</summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>Stops before the next user step when a bound node has exited.</summary>
    /// <returns>Whether the inner iterator produced another instruction.</returns>
    public bool MoveNext() => !_disposed && !_cancellation.IsCancellationRequested && _inner.MoveNext();

    /// <summary>Rejects reset because cancellation subscriptions and iterators have single ownership.</summary>
    public void Reset() => throw new NotSupportedException("Node-bound coroutines cannot be reset.");

    /// <summary>Detaches native callbacks and disposes the actual user iterator even if cleanup throws.</summary>
    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        List<Exception>? failures = null;
        foreach (var reference in _nodes)
        {
            try
            {
                if (reference.TryGetTarget(out var node) && GodotObject.IsInstanceValid(node))
                    node.TreeExiting -= _onNodeExiting;
            }
            catch (Exception exception)
            {
                (failures ??= new List<Exception>()).Add(exception);
            }
        }

        // Each resource has independent ownership. A failed native unsubscription must not skip user finally blocks.
        TryDispose(_innerCancellationRegistration.Dispose, ref failures);
        TryDispose(_cancellation.Dispose, ref failures);
        TryDispose(_inner.Dispose, ref failures);
        if (failures is { Count: 1 })
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures != null)
            throw new AggregateException("Node-bound coroutine cleanup failed.", failures);
    }

    /// <summary>Attempts independent cleanup and retains every failure for the scheduler's terminal result.</summary>
    /// <param name="dispose">The cleanup operation.</param>
    /// <param name="failures">Failures accumulated by this disposal.</param>
    private static void TryDispose(Action dispose, ref List<Exception>? failures)
    {
        try
        {
            dispose();
        }
        catch (Exception exception)
        {
            (failures ??= new List<Exception>()).Add(exception);
        }
    }
}
