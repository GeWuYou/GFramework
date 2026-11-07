// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using GFramework.Game.Abstractions.Scene;
using GFramework.Godot.Scene;

namespace GodotIntegration;

/// <summary>Constructs a native gameplay scene using explicit project dependencies.</summary>
internal sealed class DemoSceneFactory(DemoRoot root) : ISceneFactory
{
    /// <inheritdoc />
    public ISceneBehavior Create(string sceneKey)
    {
        if (!string.Equals(sceneKey, "play", StringComparison.Ordinal))
            throw new ArgumentException("Unknown sample scene.", nameof(sceneKey));
        return new Node2DSceneBehavior(new DemoWorld { Host = root }, sceneKey);
    }
}
