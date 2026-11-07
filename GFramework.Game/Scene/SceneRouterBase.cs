// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Extensions;
using GFramework.Game.Abstractions.Enums;
using GFramework.Game.Abstractions.Scene;
using GFramework.Game.Routing;

namespace GFramework.Game.Scene;

/// <summary>
///     Owns a scene stack and serializes guarded navigation on the root's host context.
///     Preparation failures preserve the previous scene; shutdown bypasses guards and attempts every cleanup.
/// </summary>
public abstract class SceneRouterBase : RouterBase<ISceneBehavior, ISceneEnterParam>, ISceneRouter
{
    private readonly SceneTransitionPipeline _pipeline = new();
    private ISceneFactory _factory = null!;

    /// <summary>The host root that attaches and removes scene instances.</summary>
    protected ISceneRoot? Root;

    /// <summary>The current stack scene, or null when empty.</summary>
    public new ISceneBehavior? Current => base.Current;

    /// <summary>The current scene key, or null when empty.</summary>
    public new string? CurrentKey => Current?.Key;

    /// <summary>A bottom-to-top snapshot of the scene stack.</summary>
    IEnumerable<ISceneBehavior> ISceneRouter.Stack => Stack.Reverse().ToArray();

    /// <summary>Whether the router is executing a navigation operation.</summary>
    public bool IsTransitioning { get; private set; }

    /// <summary>Binds a root and captures its host synchronization context.</summary>
    /// <param name="root">The root that owns scene attachment.</param>
    public void BindRoot(ISceneRoot root)
    {
        ArgumentNullException.ThrowIfNull(root);
        EnsureRouteAvailable();
        Root = root;
        BindRouteContext();
    }

    /// <summary>Prepares a target before removing the previous scene stack.</summary>
    /// <param name="sceneKey">Target scene key.</param>
    /// <param name="param">Optional scene loading parameters.</param>
    /// <returns>Navigation completion; a rejected guard leaves the stack unchanged.</returns>
    public ValueTask ReplaceAsync(string sceneKey, ISceneEnterParam? param = null) =>
        TransitionAsync(sceneKey, SceneTransitionType.Replace, param, () => ReplaceCoreAsync(sceneKey, param));

    /// <summary>Pushes a prepared scene, restoring the previous scene if entering fails.</summary>
    /// <param name="sceneKey">Target scene key.</param>
    /// <param name="param">Optional scene loading parameters.</param>
    /// <returns>Navigation completion.</returns>
    public ValueTask PushAsync(string sceneKey, ISceneEnterParam? param = null) =>
        TransitionAsync(sceneKey, SceneTransitionType.Push, param, () => PushCoreAsync(sceneKey, param));

    /// <summary>Exits the top scene and resumes the remaining scene even if cleanup reports an error.</summary>
    /// <returns>Navigation completion.</returns>
    public ValueTask PopAsync() => TransitionAsync(null, SceneTransitionType.Pop, null, PopCoreAsync);

    /// <summary>Clears the stack only when every leave guard accepts; refusal terminates immediately.</summary>
    /// <returns>Navigation completion.</returns>
    public ValueTask ClearAsync() => TransitionAsync(null, SceneTransitionType.Clear, null, ClearCoreAsync);

    /// <summary>Checks whether a scene key is present in the stack.</summary>
    /// <param name="sceneKey">Scene key.</param>
    /// <returns>Whether the scene is present.</returns>
    public new bool Contains(string sceneKey) => base.Contains(sceneKey);

    /// <summary>Registers a transition stage handler.</summary>
    /// <param name="handler">Handler instance.</param>
    /// <param name="options">Optional timeout and error policy.</param>
    public void RegisterHandler(ISceneTransitionHandler handler, SceneTransitionHandlerOptions? options = null) =>
        _pipeline.RegisterHandler(handler, options);

    /// <summary>Removes a transition stage handler.</summary>
    /// <param name="handler">Handler instance.</param>
    public void UnregisterHandler(ISceneTransitionHandler handler) => _pipeline.UnregisterHandler(handler);

    /// <summary>Registers middleware around a complete transition.</summary>
    /// <param name="handler">Middleware instance.</param>
    /// <param name="options">Optional timeout and error policy.</param>
    public void RegisterAroundHandler(ISceneAroundTransitionHandler handler, SceneTransitionHandlerOptions? options = null) =>
        _pipeline.RegisterAroundHandler(handler, options);

    /// <summary>Removes transition middleware.</summary>
    /// <param name="handler">Middleware instance.</param>
    public void UnregisterAroundHandler(ISceneAroundTransitionHandler handler) => _pipeline.UnregisterAroundHandler(handler);

    /// <summary>Registers the scene-specific transition handlers.</summary>
    protected override abstract void RegisterHandlers();

    /// <summary>Resolves the scene factory after the architecture provider is ready.</summary>
    protected override void OnInit()
    {
        _factory = this.GetUtility<ISceneFactory>()!;
        RegisterHandlers();
    }

    /// <summary>Runs every scene cleanup without navigation guards during asynchronous shutdown.</summary>
    /// <returns>The cleanup task.</returns>
    protected override Task OnStopAsync() => ClearWithoutGuardsAsync();

    /// <summary>Holds the navigation gate across guards, middleware, callbacks and state updates.</summary>
    private ValueTask TransitionAsync(string? key, SceneTransitionType type, ISceneEnterParam? param,
        Func<Task<bool>> action)
    {
        return new ValueTask(ExecuteRouteOperationAsync(async () =>
        {
            IsTransitioning = true;
            try
            {
                var transition = new SceneTransitionEvent
                {
                    FromSceneKey = CurrentKey,
                    ToSceneKey = key,
                    TransitionType = type,
                    EnterParam = param
                };
                await _pipeline.ExecuteAroundAsync(transition, async () =>
                {
                    await _pipeline.ExecuteAsync(transition, SceneTransitionPhases.BeforeChange).ConfigureAwait(true);
                    if (await action().ConfigureAwait(true))
                        await _pipeline.ExecuteAsync(transition, SceneTransitionPhases.AfterChange).ConfigureAwait(true);
                }).ConfigureAwait(true);
            }
            finally
            {
                IsTransitioning = false;
            }
        }));
    }

    /// <summary>Checks every leave guard before a destructive clear or replacement.</summary>
    private async Task<bool> CanLeaveStackAsync()
    {
        foreach (var scene in Stack)
        {
            if (!await ExecuteLeaveGuardsAsync(scene.Key).ConfigureAwait(true))
                return false;
        }
        return true;
    }

    /// <summary>Checks admission and prepares the target while old scenes are still intact.</summary>
    private async Task<bool> ReplaceCoreAsync(string key, ISceneEnterParam? param)
    {
        if (!await ExecuteEnterGuardsAsync(key, param).ConfigureAwait(true) ||
            !await CanLeaveStackAsync().ConfigureAwait(true))
            return false;

        var scene = await PrepareSceneAsync(key, param).ConfigureAwait(true);
        try
        {
            await ClearWithoutGuardsAsync().ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            var errors = new List<Exception> { exception };
            await CleanupSceneAsync(scene, entered: false, errors).ConfigureAwait(true);
            throw new AggregateException("Scene replacement cleanup failed.", errors);
        }
        await EnterSceneAsync(scene).ConfigureAwait(true);
        return true;
    }

    /// <summary>Prepares a unique target before pausing the previous stack top.</summary>
    private async Task<bool> PushCoreAsync(string key, ISceneEnterParam? param)
    {
        if (Contains(key) || !await ExecuteEnterGuardsAsync(key, param).ConfigureAwait(true))
            return false;

        var scene = await PrepareSceneAsync(key, param).ConfigureAwait(true);
        await EnterSceneAsync(scene).ConfigureAwait(true);
        return true;
    }

    /// <summary>Attaches and loads a target, reclaiming even partially loaded instances on failure.</summary>
    private async Task<ISceneBehavior> PrepareSceneAsync(string key, ISceneEnterParam? param)
    {
        var root = Root ?? throw new InvalidOperationException("Bind a scene root before navigation.");
        var scene = _factory.Create(key);
        try
        {
            root.AddScene(scene);
            await scene.OnLoadAsync(param).ConfigureAwait(true);
            return scene;
        }
        catch (Exception exception)
        {
            var errors = new List<Exception> { exception };
            await CleanupSceneAsync(scene, entered: false, errors).ConfigureAwait(true);
            throw new AggregateException("Scene preparation failed; the previous stack was retained.", errors);
        }
    }

    /// <summary>Commits a prepared scene and compensates a failed pause or enter operation.</summary>
    private async Task EnterSceneAsync(ISceneBehavior scene)
    {
        var previous = Current;
        var enterAttempted = false;
        try
        {
            if (previous is not null)
                await previous.OnPauseAsync().ConfigureAwait(true);
            Stack.Push(scene);
            enterAttempted = true;
            await scene.OnEnterAsync().ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            if (ReferenceEquals(Current, scene))
                Stack.Pop();

            var errors = new List<Exception> { exception };
            await CleanupSceneAsync(scene, enterAttempted, errors).ConfigureAwait(true);
            if (previous is not null)
                await AttemptAsync(() => previous.OnResumeAsync(), errors).ConfigureAwait(true);
            throw new AggregateException("Scene enter failed; the previous scene was restored where possible.", errors);
        }
    }

    /// <summary>Removes a guarded scene, then always attempts to restore the scene underneath.</summary>
    private async Task<bool> PopCoreAsync()
    {
        if (Current is not { } scene || !await ExecuteLeaveGuardsAsync(scene.Key).ConfigureAwait(true))
            return false;

        Stack.Pop();
        var errors = new List<Exception>();
        await CleanupSceneAsync(scene, entered: true, errors).ConfigureAwait(true);
        if (Current is { } next)
            await AttemptAsync(() => next.OnResumeAsync(), errors).ConfigureAwait(true);
        ThrowCleanupErrors(errors);
        return true;
    }

    /// <summary>Terminates a blocked clear without repeatedly invoking the same guard.</summary>
    private async Task<bool> ClearCoreAsync()
    {
        if (!await CanLeaveStackAsync().ConfigureAwait(true))
            return false;
        await ClearWithoutGuardsAsync().ConfigureAwait(true);
        return true;
    }

    /// <summary>Drains all tracked scenes without resuming scenes that are about to be removed.</summary>
    private async Task ClearWithoutGuardsAsync()
    {
        var errors = new List<Exception>();
        while (Stack.TryPop(out var scene))
            await CleanupSceneAsync(scene, entered: true, errors).ConfigureAwait(true);
        ThrowCleanupErrors(errors);
    }

    /// <summary>Attempts exit, unload and root removal independently so one error cannot orphan a node.</summary>
    private async Task CleanupSceneAsync(ISceneBehavior scene, bool entered, List<Exception> errors)
    {
        if (entered)
            await AttemptAsync(() => scene.OnExitAsync(), errors).ConfigureAwait(true);
        await AttemptAsync(() => scene.OnUnloadAsync(), errors).ConfigureAwait(true);
        try
        {
            Root?.RemoveScene(scene);
        }
        catch (Exception exception)
        {
            errors.Add(exception);
        }
    }

    /// <summary>Records a lifecycle error while allowing the remaining cleanup to continue.</summary>
    private static async Task AttemptAsync(Func<ValueTask> callback, List<Exception> errors)
    {
        try
        {
            await callback().ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            errors.Add(exception);
        }
    }

    /// <summary>Reports all cleanup errors only after every owned route has been processed.</summary>
    private static void ThrowCleanupErrors(List<Exception> errors)
    {
        if (errors.Count != 0)
            throw new AggregateException("One or more scene cleanup operations failed.", errors);
    }
}
