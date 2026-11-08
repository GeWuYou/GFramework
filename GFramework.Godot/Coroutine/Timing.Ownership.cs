// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Coroutine;

namespace GFramework.Godot.Coroutine;

public partial class Timing
{
    private Dictionary<ulong, OwnedCoroutineScope> _ownerScopes = [];

    /// <summary>Tracks native exit callbacks and all independently selectable scopes belonging to one node.</summary>
    private sealed class OwnedCoroutineScope
    {
        /// <summary>Creates an owner registration before any user iterator is prewarmed.</summary>
        /// <param name="owner">The live native owner.</param>
        /// <param name="ownerId">Its engine instance identity.</param>
        /// <param name="defaultScope">The Supervisor scope for ordinary node-run tasks.</param>
        /// <param name="onExiting">Closes every scope before the node leaves.</param>
        /// <param name="onExited">Releases callbacks after other TreeExiting subscribers have run.</param>
        internal OwnedCoroutineScope(Node owner, ulong ownerId, CoroutineScope defaultScope,
            Action<ulong> onExiting, Action<ulong> onExited)
        {
            Owner = new WeakReference<Node>(owner);
            DefaultScope = defaultScope;
            Scopes = [defaultScope];
            OnExiting = () => onExiting(ownerId);
            OnExited = () => onExited(ownerId);
        }

        /// <summary>Gets the weak native owner, avoiding an ownership cycle with a persistent Timing host.</summary>
        internal WeakReference<Node> Owner
        {
            get;
        }

        /// <summary>Gets the default scope used by ordinary node task entrypoints.</summary>
        internal CoroutineScope DefaultScope
        {
            get; set;
        }

        /// <summary>Gets every explicitly selected scope that must be closed on tree exit.</summary>
        internal List<CoroutineScope> Scopes
        {
            get;
        }

        /// <summary>Gets the pre-exit cancellation callback.</summary>
        internal Action OnExiting
        {
            get;
        }

        /// <summary>Gets the post-exit callback that releases this registration.</summary>
        internal Action OnExited
        {
            get;
        }

        /// <summary>Prevents synchronous cleanup callbacks from starting replacement work on an exiting node.</summary>
        internal bool IsExiting
        {
            get; set;
        }
    }

    /// <summary>Creates an independent node-owned scope with an explicit, immutable failure policy.</summary>
    /// <param name="owner">The live lifetime owner.</param>
    /// <param name="policy">Supervisor by default for independent node operations.</param>
    /// <returns>A scope whose disposal or owner exit cancels all of its tasks and descendants.</returns>
    public static CoroutineScope CreateScope(Node owner, CoroutineFailurePolicy policy = CoroutineFailurePolicy.Supervisor) =>
        Instance.CreateScopeOnInstance(owner, policy);

    /// <summary>Creates an explicitly configured scope owned by a node on this host.</summary>
    /// <param name="owner">The live lifetime owner.</param>
    /// <param name="policy">The immutable failure policy.</param>
    /// <returns>The newly created independent scope.</returns>
    public CoroutineScope CreateScopeOnInstance(Node owner, CoroutineFailurePolicy policy = CoroutineFailurePolicy.Supervisor)
    {
        ArgumentNullException.ThrowIfNull(owner);
        if (policy is not (CoroutineFailurePolicy.FailFast or CoroutineFailurePolicy.Supervisor))
            throw new ArgumentOutOfRangeException(nameof(policy));
        var registration = GetOrCreateOwnedScope(owner);
        registration.Scopes.RemoveAll(scope => scope.IsDisposed);
        var scope = Scheduler.CreateScope(policy);
        registration.Scopes.Add(scope);
        return scope;
    }

    /// <summary>Runs an iterator in the owner's default Supervisor scope.</summary>
    /// <param name="owner">The live owner, or null to reject the request.</param>
    /// <param name="coroutine">The iterator transferred to the scheduler.</param>
    /// <param name="segment">Its initial stage and pause policy.</param>
    /// <param name="tag">An optional management tag.</param>
    /// <param name="cancellationToken">External cancellation.</param>
    /// <returns>The task handle, or default for an unavailable owner.</returns>
    public CoroutineHandle RunOwnedCoroutineOnInstance(Node? owner, IEnumerator<IYieldInstruction>? coroutine,
        Segment segment = Segment.Process, string? tag = null, CancellationToken cancellationToken = default)
    {
        if (coroutine == null)
            return default;
        if (!IsNodeAlive(owner))
        {
            coroutine.Dispose();
            return default;
        }

        var scope = GetOrCreateOwnedScope(owner!).DefaultScope;
        return scope.Run(coroutine, tag, cancellationToken: cancellationToken,
            stage: GetStage(segment), ignorePause: segment == Segment.ProcessIgnorePause);
    }

    /// <summary>Runs a structured flow in the owner's default Supervisor scope.</summary>
    /// <param name="owner">The live lifetime owner.</param>
    /// <param name="factory">Creates the flow and its explicitly owned children.</param>
    /// <param name="segment">Its initial stage and pause policy.</param>
    /// <param name="tag">An optional management tag.</param>
    /// <param name="cancellationToken">External cancellation.</param>
    /// <returns>The flow handle, or default for an unavailable owner.</returns>
    public CoroutineHandle RunOwnedCoroutineOnInstance(Node? owner,
        Func<CoroutineContext, IEnumerator<IYieldInstruction>> factory,
        Segment segment = Segment.Process, string? tag = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(factory);
        if (!IsNodeAlive(owner))
            return default;
        return GetOrCreateOwnedScope(owner!).DefaultScope.Run(factory, tag,
            cancellationToken: cancellationToken, stage: GetStage(segment),
            ignorePause: segment == Segment.ProcessIgnorePause);
    }

    /// <summary>Cancels all scopes owned by a node across active hosts without detaching lifetime ownership.</summary>
    /// <param name="owner">The native owner.</param>
    /// <returns>The number of active owned tasks cancelled.</returns>
    public static int KillCoroutines(Node owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        if (!IsInstanceValid(owner))
            return 0;
        var ownerId = owner.GetInstanceId();
        return EnumerateActiveInstances().Sum(timing => timing.CancelOwnedScopes(ownerId));
    }

    /// <summary>Counts every active owned task, including descendants and explicitly configured scopes.</summary>
    /// <param name="owner">The native owner.</param>
    /// <returns>The active owned task count across current hosts.</returns>
    public static int GetOwnedCoroutineCount(Node owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        if (!IsInstanceValid(owner))
            return 0;
        var ownerId = owner.GetInstanceId();
        return EnumerateActiveInstances().Sum(timing => timing._ownerScopes.TryGetValue(ownerId, out var registration)
            ? registration.Scopes.Sum(scope => scope.ActiveCount)
            : 0);
    }

    /// <summary>Subscribes before synchronous prewarming, so an iterator cannot escape ownership by exiting its node.</summary>
    /// <param name="owner">The live native owner.</param>
    /// <returns>The registered owner scopes.</returns>
    private OwnedCoroutineScope GetOrCreateOwnedScope(Node owner)
    {
        if (!IsNodeAlive(owner))
            throw new InvalidOperationException("Coroutine scopes require a live node inside the scene tree.");
        var ownerId = owner.GetInstanceId();
        if (_ownerScopes.TryGetValue(ownerId, out var existing))
        {
            if (existing.IsExiting)
                throw new InvalidOperationException("Cannot start work while its owner is leaving the scene tree.");
            if (existing.DefaultScope.IsDisposed)
            {
                existing.DefaultScope = Scheduler.CreateScope(CoroutineFailurePolicy.Supervisor);
                existing.Scopes.Add(existing.DefaultScope);
            }
            return existing;
        }

        var registration = new OwnedCoroutineScope(owner, ownerId,
            Scheduler.CreateScope(CoroutineFailurePolicy.Supervisor), HandleOwnerExiting, HandleOwnerExited);
        _ownerScopes.Add(ownerId, registration);
        owner.TreeExiting += registration.OnExiting;
        owner.TreeExited += registration.OnExited;
        return registration;
    }

    /// <summary>Cancels a snapshot because task cleanup can dispose another scope in the same owner.</summary>
    /// <param name="ownerId">The owner's native identity.</param>
    /// <returns>The count of accepted cancellations.</returns>
    private int CancelOwnedScopes(ulong ownerId) => _ownerScopes.TryGetValue(ownerId, out var registration)
        ? registration.Scopes.ToArray().Sum(scope => scope.Cancel())
        : 0;

    /// <summary>Closes every scope while the node is still valid, preventing replacement work from cleanup callbacks.</summary>
    /// <param name="ownerId">The native identity captured before exit.</param>
    private void HandleOwnerExiting(ulong ownerId)
    {
        if (!_ownerScopes.TryGetValue(ownerId, out var registration))
            return;
        registration.IsExiting = true;
        foreach (var scope in registration.Scopes.ToArray())
            scope.Dispose();
    }

    /// <summary>Releases native event subscriptions after all pre-exit callbacks have run.</summary>
    /// <param name="ownerId">The native identity captured before exit.</param>
    private void HandleOwnerExited(ulong ownerId)
    {
        if (_ownerScopes.Remove(ownerId, out var registration))
            DetachOwnerCallbacks(registration);
    }

    /// <summary>Detaches all native registrations before host shutdown completes.</summary>
    private void DisposeOwnedScopes()
    {
        var registrations = _ownerScopes.Values.ToArray();
        _ownerScopes.Clear();
        List<Exception>? failures = null;
        foreach (var registration in registrations)
        {
            TryCleanup(() => DetachOwnerCallbacks(registration), ref failures);
            foreach (var scope in registration.Scopes)
                TryCleanup(scope.Dispose, ref failures);
        }
        if (failures != null)
            throw new AggregateException("Owned coroutine scope cleanup failed.", failures);
    }

    /// <summary>Detaches only when the weak native owner still exists.</summary>
    /// <param name="registration">The owner registration being released.</param>
    private static void DetachOwnerCallbacks(OwnedCoroutineScope registration)
    {
        if (registration.Owner.TryGetTarget(out var owner) && IsInstanceValid(owner))
        {
            owner.TreeExiting -= registration.OnExiting;
            owner.TreeExited -= registration.OnExited;
        }
    }
}
