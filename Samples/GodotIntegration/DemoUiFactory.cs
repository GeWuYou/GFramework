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

/// <summary>Creates menu, pause and settings views and uses the existing Godot behavior adapters.</summary>
internal sealed class DemoUiFactory(DemoRoot root) : AbstractContextUtility, IUiFactory
{
    /// <inheritdoc />
    public IUiPageBehavior Create(string uiKey)
    {
        var panel = root.CreatePanel(uiKey);
        return UiPageBehaviorFactory.Create(panel, uiKey,
            string.Equals(uiKey, "menu", StringComparison.Ordinal) ? UiLayer.Page : UiLayer.Modal);
    }

    /// <summary>No further dependencies are needed.</summary>
    protected override void OnInit()
    {
    }
}
