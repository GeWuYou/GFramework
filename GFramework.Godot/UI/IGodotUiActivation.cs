// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Godot.UI;

/// <summary>Optional interaction notifications, independent of opening, visibility, and gameplay pause.</summary>
public interface IGodotUiActivation
{
    /// <summary>Called when this view becomes eligible for interaction.</summary>
    void OnActivated();
    /// <summary>Called when a modal or navigation removes interaction eligibility.</summary>
    void OnDeactivated();
}
