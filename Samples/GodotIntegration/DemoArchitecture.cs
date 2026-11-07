// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Pause;
using GFramework.Core.Pause;
using GFramework.Game.Abstractions.Scene;
using GFramework.Game.Abstractions.UI;
using GFramework.Godot.Architectures;

namespace GodotIntegration;

/// <summary>
/// Installs the sample's Scene/UI composition. The Godot host owns the scene tree,
/// while Architecture owns component startup and reverse-order shutdown.
/// </summary>
internal sealed class DemoArchitecture(DemoRoot root) : AbstractArchitecture
{
    /// <summary>Registers interfaces before the provider is built; no nodes are attached during configuration.</summary>
    protected override void InstallModules()
    {
        RegisterUtility<IPauseStackManager>(new PauseStackManager());
        RegisterUtility<ISceneFactory>(new DemoSceneFactory(root));
        RegisterUtility<IUiFactory>(new DemoUiFactory(root));
        RegisterSystem(new DemoSceneRouter(root));
        RegisterSystem(new DemoUiRouter(root));
    }
}
