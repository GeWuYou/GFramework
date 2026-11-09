// Copyright (c) 2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Arch.Core;
using GFramework.Core.Abstractions.Bases;
using GFramework.Core.Abstractions.Lifecycle;
using ArchSystem = Arch.System.ISystem<float>;

namespace GFramework.Ecs.Arch;

/// <summary>Host-driven native Arch execution, independent of Architecture and DI.</summary>
/// <remarks>
/// This object borrows its World and systems. It never disposes them. DI owns factory/type registrations;
/// direct callers stop this runtime before disposing systems and then the World. A Group is one root owner
/// of its children and must not share owning registrations for those children.
/// </remarks>
public sealed class ArchRuntime : IArchRuntime
{
    private readonly World _world;
    private readonly ArchSystem[] _systems;
    private readonly Measurement[]? _measurements;
    private readonly List<ArchSystem> _attempted = [];
    private RuntimeState _state;
    private bool _executing;
    private int _ownerThread;
    private int _entityCount;
    private long _completedFrames;
    private long _failedFrames;
    private TimeSpan _lastFrameDuration;
    private AggregateException? _stopError;

    /// <summary>Captures ordered native systems and the statistics policy without starting business work.</summary>
    /// <param name="world">The caller-owned World used by the systems.</param>
    /// <param name="systems">Borrowed roots; priority ties preserve enumeration order.</param>
    /// <param name="options">Statistics settings copied at construction; other fields configure the DI adapter.</param>
    public ArchRuntime(World world, IEnumerable<ArchSystem> systems, ArchOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(systems);
        _world = world;
        _systems = [.. systems.Select(system => system ?? throw new ArgumentException("Null system.", nameof(systems))).OrderBy(system => system is IPrioritized priority ? priority.Priority : 0)];
        if (_systems.Distinct(ReferenceEqualityComparer.Instance).Take(_systems.Length + 1).Count() != _systems.Length)
            throw new ArgumentException("Each top-level system instance must be registered once.", nameof(systems));
        _measurements = options?.EnableStatistics == true ? new Measurement[_systems.Length] : null;
    }

    /// <inheritdoc />
    public void Initialize()
    {
        CheckThreadAndReentrancy();
        if (_state == RuntimeState.Ready)
            return;
        if (_state != RuntimeState.Created)
            throw new InvalidOperationException("A stopped or failed Arch runtime cannot restart.");
        _ownerThread = Environment.CurrentManagedThreadId;
        _executing = true;
        _state = RuntimeState.Failed;
        try
        {
            foreach (var system in _systems)
            {
                // Even a partially initialized system owes its optional business-stop callback.
                _attempted.Add(system);
                system.Initialize();
            }
            _entityCount = _world.Size;
            _state = RuntimeState.Ready;
        }
        finally
        {
            _executing = false;
        }
    }

    /// <inheritdoc />
    public void Update(float deltaTime)
    {
        CheckThreadAndReentrancy();
        if (_state == RuntimeState.Failed)
            throw new InvalidOperationException("The Arch runtime failed; only Stop is permitted.");
        if (_state != RuntimeState.Ready)
            return;
        _executing = true;
        // Disabled statistics take the direct native path: no timer calls or measurement allocations.
        var started = _measurements is null ? 0 : Stopwatch.GetTimestamp();
        if (_measurements is not null)
            Array.Clear(_measurements);
        try
        {
            RunPhase(ArchUpdatePhase.BeforeUpdate, deltaTime);
            RunPhase(ArchUpdatePhase.Update, deltaTime);
            RunPhase(ArchUpdatePhase.AfterUpdate, deltaTime);
            if (_measurements is not null)
                _completedFrames++;
        }
        catch
        {
            _state = RuntimeState.Failed;
            if (_measurements is not null)
                _failedFrames++;
            throw;
        }
        finally
        {
            if (_measurements is not null)
            {
                _lastFrameDuration = Stopwatch.GetElapsedTime(started);
                _entityCount = _world.Size;
            }
            _executing = false;
        }
    }

    /// <inheritdoc />
    public void Stop()
    {
        CheckThreadAndReentrancy();
        if (_state == RuntimeState.Stopped)
        {
            if (_stopError is not null)
                throw _stopError;
            return;
        }
        _executing = true;
        _state = RuntimeState.Stopped;
        var errors = new List<Exception>();
        try
        {
            // Native systems need no business-stop contract. Optional IDestroyable participants
            // stop here; final IDisposable remains with the single external/provider owner.
            for (var index = _attempted.Count - 1; index >= 0; index--)
            {
                try
                {
                    if (_attempted[index] is IDestroyable participant)
                        participant.Destroy();
                }
                catch (Exception error)
                {
                    errors.Add(error);
                }
            }
            _attempted.Clear();
            _entityCount = _world.Size;
        }
        finally
        {
            _executing = false;
        }
        if (errors.Count > 0)
        {
            _stopError = new AggregateException("Arch system business stop failed.", errors);
            throw _stopError;
        }
    }

    /// <inheritdoc />
    public ArchStatisticsSnapshot? GetStatistics()
    {
        CheckThreadAndReentrancy();
        if (_measurements is null)
            return null;
        if (_state == RuntimeState.Ready)
            _entityCount = _world.Size;
        var systems = new ArchSystemStatistics[_systems.Length];
        for (var index = 0; index < _systems.Length; index++)
        {
            var measurement = _measurements[index];
            var type = _systems[index].GetType();
            systems[index] = new ArchSystemStatistics(index, type.FullName ?? type.Name,
                measurement.Before, measurement.Update, measurement.After, measurement.FailedPhase);
        }
        return new ArchStatisticsSnapshot(_entityCount, _completedFrames, _failedFrames, _lastFrameDuration, systems);
    }

    /// <summary>Runs one native phase over every root, retaining partial measurements on failure.</summary>
    private void RunPhase(ArchUpdatePhase phase, float deltaTime)
    {
        for (var index = 0; index < _systems.Length; index++)
        {
            var started = _measurements is null ? 0 : Stopwatch.GetTimestamp();
            try
            {
                switch (phase)
                {
                    case ArchUpdatePhase.BeforeUpdate:
                        _systems[index].BeforeUpdate(in deltaTime);
                        break;
                    case ArchUpdatePhase.Update:
                        _systems[index].Update(in deltaTime);
                        break;
                    case ArchUpdatePhase.AfterUpdate:
                        _systems[index].AfterUpdate(in deltaTime);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(phase));
                }
            }
            catch
            {
                if (_measurements is { } measurements)
                    measurements[index].FailedPhase = phase;
                throw;
            }
            finally
            {
                if (_measurements is not null)
                {
                    var elapsed = Stopwatch.GetElapsedTime(started);
                    switch (phase)
                    {
                        case ArchUpdatePhase.BeforeUpdate:
                            _measurements[index].Before = elapsed;
                            break;
                        case ArchUpdatePhase.Update:
                            _measurements[index].Update = elapsed;
                            break;
                        case ArchUpdatePhase.AfterUpdate:
                            _measurements[index].After = elapsed;
                            break;
                    }
                }
            }
        }
    }

    /// <summary>Rejects cross-thread or callback reentry that would invalidate the native lifecycle and snapshot.</summary>
    private void CheckThreadAndReentrancy()
    {
        if (_executing || (_ownerThread != 0 && _ownerThread != Environment.CurrentManagedThreadId))
            throw new InvalidOperationException("Arch runtime access requires its idle owning host thread.");
    }

    /// <summary>The one-way business lifetime; final resource ownership is external.</summary>
    private enum RuntimeState
    {
        Created, Ready, Failed, Stopped,
    }

    /// <summary>Reusable mutable storage; immutable public objects are allocated only when a snapshot is requested.</summary>
    [StructLayout(LayoutKind.Auto)]
    private struct Measurement
    {
        /// <summary>Pre-update elapsed time; null means not invoked.</summary>
        public TimeSpan? Before;
        /// <summary>Update elapsed time; null means not invoked.</summary>
        public TimeSpan? Update;
        /// <summary>Post-update elapsed time; null means not invoked.</summary>
        public TimeSpan? After;
        /// <summary>The failed phase, if any.</summary>
        public ArchUpdatePhase? FailedPhase;
    }
}
