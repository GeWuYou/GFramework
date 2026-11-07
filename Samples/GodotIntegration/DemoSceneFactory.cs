// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using GFramework.Core.Utility;
using GFramework.Game.Abstractions.Enums;
using GFramework.Game.Abstractions.Scene;
using GFramework.Game.Abstractions.UI;
using GFramework.Game.Scene;
using GFramework.Game.UI;
using GFramework.Godot.Scene;
using GFramework.Godot.UI;

namespace GodotIntegration;

/// <summary>Creates the sample gameplay scene without a second scene-loading abstraction.</summary>
internal sealed class DemoSceneFactory(DemoRoot root) : AbstractContextUtility, ISceneFactory
{
    /// <inheritdoc />
    public ISceneBehavior Create(string sceneKey)
    {
        if (!string.Equals(sceneKey, "play", StringComparison.Ordinal))
            throw new ArgumentException("Unknown sample scene.", nameof(sceneKey));
        return new Node2DSceneBehavior(new DemoWorld { Host = root }, sceneKey);
    }

    /// <summary>No further dependencies are needed.</summary>
    protected override void OnInit()
    {
    }
}
