// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Pause;

namespace GFramework.Game.Abstractions.UI;

/// <summary>Determines whether a route participates in page navigation or is an independent window.</summary>
public enum UiRouteKind
{
    /// <summary>A page in navigation history; only the top page is visible.</summary>
    Page,
    /// <summary>An independent view, such as a HUD, popup, or notification.</summary>
    Window,
}
