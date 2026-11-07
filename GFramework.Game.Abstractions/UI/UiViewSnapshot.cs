// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Abstractions.UI;

/// <summary>An immutable, engine-independent description of one tracked view.</summary>
/// <param name="Handle">Owning service and instance identity.</param>
/// <param name="RouteKey">Registered route key.</param>
/// <param name="Options">Immutable route policies.</param>
/// <param name="Visible">Current committed visibility.</param>
/// <param name="Active">Current committed interaction eligibility.</param>
public sealed record UiViewSnapshot(UiInstanceHandle Handle, string RouteKey, UiRouteOptions Options, bool Visible, bool Active);
