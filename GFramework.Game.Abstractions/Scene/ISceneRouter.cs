// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Abstractions.Scene;

/// <summary>
/// Owns guarded scene navigation independently of an architecture or engine.
/// The host supplies scene construction, root attachment and dispatch; dispose before releasing the host.
/// </summary>
public interface ISceneRouter : IAsyncDisposable
{
    /// <summary>Gets the current scene, or null for an empty stack.</summary>
    ISceneBehavior? Current
    {
        get;
    }
    /// <summary>Gets the current scene identifier.</summary>
    string? CurrentKey
    {
        get;
    }
    /// <summary>Gets a bottom-to-top snapshot of the scene stack.</summary>
    IEnumerable<ISceneBehavior> Stack
    {
        get;
    }
    /// <summary>Gets whether a transition is currently executing.</summary>
    bool IsTransitioning
    {
        get;
    }
    /// <summary>Prepares a scene before clearing the previous stack.</summary>
    /// <param name="sceneKey">Registered scene identifier.</param>
    /// <param name="param">Optional preparation parameters.</param>
    /// <param name="cancellationToken">Cancels waiting or preparation before commit.</param>
    /// <returns>The completed transition outcome.</returns>
    Task<SceneOperationResult> ReplaceAsync(string sceneKey, ISceneEnterParam? param = null, CancellationToken cancellationToken = default);
    /// <summary>Pushes a scene while retaining the previous stack.</summary>
    /// <param name="sceneKey">Registered scene identifier.</param>
    /// <param name="param">Optional preparation parameters.</param>
    /// <param name="cancellationToken">Cancels waiting or preparation before commit.</param>
    /// <returns>The completed transition outcome.</returns>
    Task<SceneOperationResult> PushAsync(string sceneKey, ISceneEnterParam? param = null, CancellationToken cancellationToken = default);
    /// <summary>Closes the top scene and resumes the previous one.</summary>
    /// <param name="cancellationToken">Cancels admission before destructive cleanup begins.</param>
    /// <returns>The completed transition outcome.</returns>
    Task<SceneOperationResult> PopAsync(CancellationToken cancellationToken = default);
    /// <summary>Clears the stack after all leave guards accept.</summary>
    /// <param name="cancellationToken">Cancels admission before destructive cleanup begins.</param>
    /// <returns>The completed transition outcome.</returns>
    Task<SceneOperationResult> ClearAsync(CancellationToken cancellationToken = default);
    /// <summary>Closes admission, joins navigation and cleans every scene without user guards.</summary>
    /// <returns>The shared stop task, including any cleanup errors.</returns>
    Task StopAsync();
    /// <summary>Checks whether an identifier exists in the current stack.</summary>
    /// <param name="sceneKey">Scene identifier.</param>
    /// <returns>Whether the scene is present.</returns>
    bool Contains(string sceneKey);
}
