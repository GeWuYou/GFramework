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

/// <summary>Connects framework page and modal routing to the project's UI root.</summary>
internal sealed class DemoUiRouter(DemoRoot root) : UiRouterBase
{
    /// <summary>Binds the host after the default router resolves its utilities.</summary>
    protected override void OnInit()
    {
        base.OnInit();
        BindRoot(root);
    }

    /// <summary>The sample does not require transition effects.</summary>
    protected override void RegisterHandlers()
    {
    }
}
