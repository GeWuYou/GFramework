// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.UI;
using Godot;

namespace GodotIntegration;

/// <summary>
/// Ordinary project Control. Routing, Cancel, modal input and pause policy come
/// from its registered route; the node has no framework behavior-provider contract.
/// </summary>
public partial class DemoPanel : PanelContainer
{
    /// <summary>Gets the owning instance identity used by the native integration checks.</summary>
    internal UiInstanceHandle InstanceHandle { get; init; }
}
