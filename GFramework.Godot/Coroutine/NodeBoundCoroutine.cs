// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Collections;
using System.Runtime.ExceptionServices;
using GFramework.Core.Abstractions.Coroutine;

namespace GFramework.Godot.Coroutine;

/// <summary>An owned iterator whose node-exit cancellation is consumed by Timing before its first step.</summary>
/// <remarks>
/// Native subscriptions are attached and released on the Godot thread. External cancellation only touches
/// the managed cancellation source; Timing forwards its token into the scheduler so long waits do not hide exit.
/// </remarks>
internal sealed class NodeBoundCoroutine : IEnumerator<IYieldInstruction>
{
    private readonly IEnumerator<IYieldInstruction> _inner;
    private readonly List<WeakReference<Node>> _nodes = new();
    private readonly CancellationTokenSource _cancellation = new();
    private readonly Action _onNodeExiting;
    private CancellationTokenRegistration _externalRegistration;
    private bool _disposed;
    private bool _bound;

    /// <summary>Transfers iterator ownership and subscribes each valid node exactly once.</summary>
    /// <param name="inner">The underlying iterator.</param>
    /// <param name="nodes">Any node's exit cancels this iterator.</param>
    internal NodeBoundCoroutine(IEnumerator<IYieldInstruction> inner, IReadOnlyCollection<Node> nodes)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _onNodeExiting = _cancellation.Cancel;
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
    }

    /// <summary>Gets the underlying instruction without hiding scheduler-recognized instruction types.</summary>
    public IYieldInstruction Current => _inner.Current;

    /// <inheritdoc />
    object IEnumerator.Current => Current;

    /// <summary>Combines one external token without adding any native work to its callback.</summary>
    /// <param name="token">The external lifetime token.</param>
    /// <returns>The node-bound lifetime token.</returns>
    internal CancellationToken BindCancellation(CancellationToken token)
    {
        if (_bound)
            throw new InvalidOperationException("A node-bound iterator can only be run once.");
        _bound = true;
        if (token.CanBeCanceled)
            _externalRegistration = token.Register(_cancellation.Cancel);
        // Chained CancelWith wrappers must expose every bound node, including while a nested wait is blocked.
        return _inner is NodeBoundCoroutine innerBound
            ? innerBound.BindCancellation(_cancellation.Token)
            : _cancellation.Token;
    }

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
        TryDispose(_externalRegistration.Dispose, ref failures);
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
