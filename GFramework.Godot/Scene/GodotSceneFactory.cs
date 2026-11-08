// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Scene;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GFramework.Godot.Scene;

/// <summary>Creates native scenes from an explicitly injected registry, reclaiming failed instances.</summary>
/// <remarks>Creates a factory usable with standard DI or direct composition.</remarks>
/// <param name="registry">Packed scene registry owned by the host.</param>
/// <param name="logger">Optional standard logger.</param>
public sealed class GodotSceneFactory(IGodotSceneRegistry registry, ILogger<GodotSceneFactory>? logger = null) : ISceneFactory
{
    private readonly IGodotSceneRegistry _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    private readonly ILogger<GodotSceneFactory> _logger = logger ?? NullLogger<GodotSceneFactory>.Instance;

    /// <inheritdoc />
    public ISceneBehavior Create(string sceneKey)
    {
        var node = _registry.Get(sceneKey).Instantiate();
        try
        {
            var behavior = node is ISceneBehaviorProvider provider ? provider.GetScene() : SceneBehaviorFactory.Create(node, sceneKey);
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Created scene {SceneKey}", sceneKey);
            }
            return behavior;
        }
        catch { node.Free(); throw; }
    }
}
