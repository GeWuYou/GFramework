// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Scene;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GFramework.Godot.Scene;

/// <summary>Creates native scenes from an explicitly injected registry, reclaiming failed instances.</summary>
public sealed class GodotSceneFactory : ISceneFactory
{
    private readonly IGodotSceneRegistry _registry;
    private readonly ILogger<GodotSceneFactory> _logger;

    /// <summary>Creates a factory usable with standard DI or direct composition.</summary>
    /// <param name="registry">Packed scene registry owned by the host.</param>
    /// <param name="logger">Optional standard logger.</param>
    public GodotSceneFactory(IGodotSceneRegistry registry, ILogger<GodotSceneFactory>? logger = null)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _logger = logger ?? NullLogger<GodotSceneFactory>.Instance;
    }

    /// <inheritdoc />
    public ISceneBehavior Create(string sceneKey)
    {
        var node = _registry.Get(sceneKey).Instantiate();
        try
        {
            var behavior = node is ISceneBehaviorProvider provider ? provider.GetScene() : SceneBehaviorFactory.Create(node, sceneKey);
            _logger.LogDebug("Created scene {SceneKey}", sceneKey);
            return behavior;
        }
        catch { node.Free(); throw; }
    }
}
