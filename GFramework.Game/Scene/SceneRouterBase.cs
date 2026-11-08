// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Enums;
using GFramework.Game.Abstractions.Routing;
using GFramework.Game.Abstractions.Runtime;
using GFramework.Game.Abstractions.Scene;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace GFramework.Game.Scene;

/// <summary>
/// Owns a scene stack with explicit factory, root and dispatcher dependencies.
/// Preparation preserves old scenes; destructive cleanup completes even after caller cancellation.
/// Use SceneRouter directly unless a project needs to register custom handlers in a derived type.
/// </summary>
public abstract class SceneRouterBase : ISceneRouter
{
    private readonly SceneTransitionPipeline _pipeline;
    private readonly ISceneFactory _factory;
    private readonly ISceneRoot _root;
    private readonly SceneOperationQueue _operations;
    private readonly List<IRouteGuard<ISceneBehavior>> _guards = [];
    private readonly Stack<ISceneBehavior> _stack = new();

    /// <summary>Creates an idle router; construction does not attach or load scenes.</summary>
    /// <param name="factory">Creates host-owned scene adapters.</param>
    /// <param name="root">Attaches and removes scene instances.</param>
    /// <param name="dispatcher">Executes all native callbacks on the host.</param>
    protected SceneRouterBase(ISceneFactory factory, ISceneRoot root, IGameDispatcher dispatcher)
        : this(factory, root, dispatcher, logger: null)
    {
    }

    /// <summary>Creates a navigator whose transition diagnostics use the host-configured logger.</summary>
    /// <param name="factory">Creates host-owned scene adapters.</param>
    /// <param name="root">Attaches and removes scene instances.</param>
    /// <param name="dispatcher">Executes native callbacks on the host.</param>
    /// <param name="logger">Receives errors retained by ContinueOnError transition policy.</param>
    protected SceneRouterBase(ISceneFactory factory, ISceneRoot root, IGameDispatcher dispatcher, ILogger? logger)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(dispatcher);
        _factory = factory;
        _root = root;
        _pipeline = new SceneTransitionPipeline(logger);
        _operations = new SceneOperationQueue(dispatcher);
    }

    /// <summary>Gets the current scene or null when empty.</summary>
    public ISceneBehavior? Current => _stack.TryPeek(out var scene) ? scene : null;
    /// <summary>Gets the current scene identifier.</summary>
    public string? CurrentKey => Current?.Key;
    /// <summary>Gets a bottom-to-top snapshot.</summary>
    public IEnumerable<ISceneBehavior> Stack => [.. _stack.Reverse()];
    /// <summary>Gets the number of retained scenes.</summary>
    public int Count => _stack.Count;
    /// <summary>Gets whether a transition is currently executing.</summary>
    public bool IsTransitioning
    {
        get; private set;
    }

    /// <inheritdoc />
    public Task<SceneOperationResult> ReplaceAsync(string sceneKey, ISceneEnterParam? param = null,
        CancellationToken cancellationToken = default) => TransitionAsync(sceneKey, SceneTransitionType.Replace,
        param, () => ReplaceCoreAsync(sceneKey, param, cancellationToken), cancellationToken);

    /// <inheritdoc />
    public Task<SceneOperationResult> PushAsync(string sceneKey, ISceneEnterParam? param = null,
        CancellationToken cancellationToken = default) => TransitionAsync(sceneKey, SceneTransitionType.Push,
        param, () => PushCoreAsync(sceneKey, param, cancellationToken), cancellationToken);

    /// <inheritdoc />
    public Task<SceneOperationResult> PopAsync(CancellationToken cancellationToken = default) =>
        TransitionAsync(key: null, SceneTransitionType.Pop, param: null, () => PopCoreAsync(cancellationToken), cancellationToken);

    /// <inheritdoc />
    public Task<SceneOperationResult> ClearAsync(CancellationToken cancellationToken = default) =>
        TransitionAsync(key: null, SceneTransitionType.Clear, param: null, () => ClearCoreAsync(cancellationToken), cancellationToken);

    /// <inheritdoc />
    public Task StopAsync() => _operations.StopAsync(CompleteStopAsync);

    /// <inheritdoc />
    [SuppressMessage("Usage", "CA1816", Justification = "DisposeAsync preserves the canonical StopAsync task identity; CompleteStopAsync suppresses finalization only after the shared cleanup succeeds.")]
    public ValueTask DisposeAsync() => new(StopAsync());

    /// <summary>Completes the one admitted shutdown before suppressing finalization; failed or reentrant aliases do not suppress it.</summary>
    [SuppressMessage("Usage", "CA1816", Justification = "This is the one successful canonical shutdown path shared by StopAsync and DisposeAsync; suppression here preserves alias task identity and never runs on failure or a reentrant no-op.")]
    private async Task CompleteStopAsync()
    {
        await ClearWithoutGuardsAsync().ConfigureAwait(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public bool Contains(string sceneKey) => _stack.Any(scene => string.Equals(scene.Key, sceneKey, StringComparison.Ordinal));

    /// <summary>Adds a guard before navigation starts. Lower priority numbers run first.</summary>
    /// <param name="guard">Explicit policy dependency.</param>
    public void AddGuard(IRouteGuard<ISceneBehavior> guard)
    {
        ArgumentNullException.ThrowIfNull(guard);
        if (_guards.Contains(guard))
            return;
        _guards.Add(guard);
        _guards.Sort(static (left, right) => left.Priority.CompareTo(right.Priority));
    }

    /// <summary>Removes a registered guard.</summary>
    /// <param name="guard">Guard to remove.</param>
    public void RemoveGuard(IRouteGuard<ISceneBehavior> guard) => _guards.Remove(guard);

    /// <summary>Registers a transition notification/effect before navigation starts.</summary>
    /// <param name="handler">Stage handler.</param>
    /// <param name="options">Timeout and error policy.</param>
    public void RegisterHandler(ISceneTransitionHandler handler, SceneTransitionHandlerOptions? options = null) =>
        _pipeline.RegisterHandler(handler, options);

    /// <summary>Removes a transition stage handler.</summary>
    /// <param name="handler">Handler to remove.</param>
    public void UnregisterHandler(ISceneTransitionHandler handler) => _pipeline.UnregisterHandler(handler);

    /// <summary>Registers middleware around navigation.</summary>
    /// <param name="handler">Middleware.</param>
    /// <param name="options">Timeout and error policy.</param>
    public void RegisterAroundHandler(ISceneAroundTransitionHandler handler, SceneTransitionHandlerOptions? options = null) =>
        _pipeline.RegisterAroundHandler(handler, options);

    /// <summary>Removes navigation middleware.</summary>
    /// <param name="handler">Middleware to remove.</param>
    public void UnregisterAroundHandler(ISceneAroundTransitionHandler handler) => _pipeline.UnregisterAroundHandler(handler);

    /// <summary>Serializes transition policy and publishes after-change only for committed changes.</summary>
    private Task<SceneOperationResult> TransitionAsync(string? key, SceneTransitionType type, ISceneEnterParam? param,
        Func<Task<SceneOperationResult>> action, CancellationToken cancellationToken)
    {
        if (key is not null)
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var result = SceneOperationResult.NoOp;
        var operation = _operations.RunAsync(async () =>
        {
            IsTransitioning = true;
            try
            {
                var transition = new SceneTransitionEvent
                {
                    FromSceneKey = CurrentKey,
                    ToSceneKey = key,
                    TransitionType = type,
                    EnterParam = param,
                };
                await _pipeline.ExecuteAroundAsync(transition, async () =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await _pipeline.ExecuteAsync(transition, SceneTransitionPhases.BeforeChange, cancellationToken).ConfigureAwait(true);
                    result = await action().ConfigureAwait(true);
                    if (result == SceneOperationResult.Applied)
                        await _pipeline.ExecuteAsync(transition, SceneTransitionPhases.AfterChange, CancellationToken.None).ConfigureAwait(true);
                }, cancellationToken).ConfigureAwait(true);
            }
            finally
            {
                IsTransitioning = false;
            }
        }, cancellationToken);
        return CompleteAsync(operation, () => result);
    }

    /// <summary>Returns the outcome after the host operation has actually completed.</summary>
    private static async Task<SceneOperationResult> CompleteAsync(Task operation, Func<SceneOperationResult> result)
    {
        await operation.ConfigureAwait(false);
        return result();
    }

    /// <summary>Checks policies before preparing any target.</summary>
    private async Task<bool> CanEnterAsync(string key, ISceneEnterParam? param, CancellationToken cancellationToken)
    {
        foreach (var guard in _guards.ToArray())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!await guard.CanEnterAsync(key, param, cancellationToken).ConfigureAwait(true))
                return false;
            if (guard.CanInterrupt)
                break;
        }
        cancellationToken.ThrowIfCancellationRequested();
        return true;
    }

    /// <summary>Checks leave policies without changing stack ownership.</summary>
    private async Task<bool> CanLeaveAsync(IEnumerable<ISceneBehavior> scenes, CancellationToken cancellationToken)
    {
        foreach (var scene in scenes)
        {
            foreach (var guard in _guards.ToArray())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!await guard.CanLeaveAsync(scene.Key, cancellationToken).ConfigureAwait(true))
                    return false;
                if (guard.CanInterrupt)
                    break;
            }
        }
        cancellationToken.ThrowIfCancellationRequested();
        return true;
    }

    /// <summary>Prepares the target before destructive commit of replacement.</summary>
    private async Task<SceneOperationResult> ReplaceCoreAsync(string key, ISceneEnterParam? param, CancellationToken cancellationToken)
    {
        if (!await CanEnterAsync(key, param, cancellationToken).ConfigureAwait(true) ||
            !await CanLeaveAsync(_stack, cancellationToken).ConfigureAwait(true))
            return SceneOperationResult.Rejected;
        var scene = await PrepareSceneAsync(key, param, cancellationToken).ConfigureAwait(true);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ClearWithoutGuardsAsync().ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            var errors = new List<Exception> { exception };
            await CleanupSceneAsync(scene, entered: false, errors).ConfigureAwait(true);
            if (errors.Count == 1 && exception is OperationCanceledException)
                throw;
            throw new AggregateException("Scene replacement failed after prepared-target cleanup.", errors);
        }
        await EnterSceneAsync(scene, CancellationToken.None).ConfigureAwait(true);
        return SceneOperationResult.Applied;
    }

    /// <summary>Retains the old scene until the prepared target enters successfully.</summary>
    private async Task<SceneOperationResult> PushCoreAsync(string key, ISceneEnterParam? param, CancellationToken cancellationToken)
    {
        if (Contains(key))
            return SceneOperationResult.NoOp;
        if (!await CanEnterAsync(key, param, cancellationToken).ConfigureAwait(true))
            return SceneOperationResult.Rejected;
        var scene = await PrepareSceneAsync(key, param, cancellationToken).ConfigureAwait(true);
        await EnterSceneAsync(scene, cancellationToken).ConfigureAwait(true);
        return SceneOperationResult.Applied;
    }

    /// <summary>Loads a target while retaining the previous stack, reclaiming partial targets on failure.</summary>
    private async Task<ISceneBehavior> PrepareSceneAsync(string key, ISceneEnterParam? param, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var scene = _factory.Create(key);
        try
        {
            _root.AddScene(scene);
            await scene.OnLoadAsync(param, cancellationToken).ConfigureAwait(true);
            cancellationToken.ThrowIfCancellationRequested();
            return scene;
        }
        catch (Exception exception)
        {
            var errors = new List<Exception> { exception };
            await CleanupSceneAsync(scene, entered: false, errors).ConfigureAwait(true);
            if (errors.Count == 1 && exception is OperationCanceledException)
                throw;
            throw new AggregateException("Scene preparation failed; the previous stack was retained.", errors);
        }
    }

    /// <summary>Compensates a failed pause or enter and restores the previous active scene.</summary>
    private async Task EnterSceneAsync(ISceneBehavior scene, CancellationToken cancellationToken)
    {
        var previous = Current;
        var enterAttempted = false;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (previous is not null)
                await previous.OnPauseAsync(cancellationToken).ConfigureAwait(true);
            cancellationToken.ThrowIfCancellationRequested();
            _stack.Push(scene);
            enterAttempted = true;
            await scene.OnEnterAsync(cancellationToken).ConfigureAwait(true);
            cancellationToken.ThrowIfCancellationRequested();
        }
        catch (Exception exception)
        {
            if (ReferenceEquals(Current, scene))
                _stack.Pop();
            var errors = new List<Exception> { exception };
            await CleanupSceneAsync(scene, enterAttempted, errors).ConfigureAwait(true);
            if (previous is not null)
                await AttemptAsync(() => previous.OnResumeAsync(CancellationToken.None), errors).ConfigureAwait(true);
            if (errors.Count == 1 && exception is OperationCanceledException)
                throw;
            throw new AggregateException("Scene enter failed; the previous scene was restored where possible.", errors);
        }
    }

    /// <summary>Commits a pop only after admission; cleanup and resume then run to completion.</summary>
    private async Task<SceneOperationResult> PopCoreAsync(CancellationToken cancellationToken)
    {
        if (Current is not { } scene)
            return SceneOperationResult.NoOp;
        if (!await CanLeaveAsync([scene], cancellationToken).ConfigureAwait(true))
            return SceneOperationResult.Rejected;
        _stack.Pop();
        var errors = new List<Exception>();
        await CleanupSceneAsync(scene, entered: true, errors).ConfigureAwait(true);
        if (Current is { } next)
            await AttemptAsync(() => next.OnResumeAsync(CancellationToken.None), errors).ConfigureAwait(true);
        ThrowCleanupErrors(errors);
        return SceneOperationResult.Applied;
    }

    /// <summary>Checks every leave guard before draining any scene.</summary>
    private async Task<SceneOperationResult> ClearCoreAsync(CancellationToken cancellationToken)
    {
        if (_stack.Count == 0)
            return SceneOperationResult.NoOp;
        if (!await CanLeaveAsync(_stack, cancellationToken).ConfigureAwait(true))
            return SceneOperationResult.Rejected;
        await ClearWithoutGuardsAsync().ConfigureAwait(true);
        return SceneOperationResult.Applied;
    }

    /// <summary>Drains all scenes without resuming scenes which are about to be removed.</summary>
    private async Task ClearWithoutGuardsAsync()
    {
        var errors = new List<Exception>();
        while (_stack.TryPop(out var scene))
            await CleanupSceneAsync(scene, entered: true, errors).ConfigureAwait(true);
        ThrowCleanupErrors(errors);
    }

    /// <summary>Attempts exit, unload and removal independently using non-cancelable cleanup.</summary>
    private async Task CleanupSceneAsync(ISceneBehavior scene, bool entered, List<Exception> errors)
    {
        if (entered)
            await AttemptAsync(() => scene.OnExitAsync(CancellationToken.None), errors).ConfigureAwait(true);
        await AttemptAsync(() => scene.OnUnloadAsync(CancellationToken.None), errors).ConfigureAwait(true);
        try
        {
            _root.RemoveScene(scene);
        }
        catch (Exception exception) { errors.Add(exception); }
    }

    /// <summary>Collects a callback error without skipping later cleanup.</summary>
    private static async Task AttemptAsync(Func<ValueTask> callback, List<Exception> errors)
    {
        try
        {
            await callback().ConfigureAwait(true);
        }
        catch (Exception exception) { errors.Add(exception); }
    }

    /// <summary>Reports all errors after ownership has been released.</summary>
    private static void ThrowCleanupErrors(List<Exception> errors)
    {
        if (errors.Count != 0)
            throw new AggregateException("One or more scene cleanup operations failed.", errors);
    }
}
