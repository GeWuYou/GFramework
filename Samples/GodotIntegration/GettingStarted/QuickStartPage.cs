// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using Godot;

namespace GodotIntegration.GettingStarted;

/// <summary>Ordinary Control scene configured by its owning route factory before mounting.</summary>
public partial class QuickStartPage : CenterContainer
{
    private QuickStartItem _item = null!;
    private Action _confirm = null!;

    /// <summary>Receives the registered resource and the one action needed by this view.</summary>
    /// <param name="item">Borrowed display data.</param>
    /// <param name="confirm">Observed host action.</param>
    public void Configure(QuickStartItem item, Action confirm)
    {
        _item = item;
        _confirm = confirm;
    }

    /// <inheritdoc />
    public override void _Ready()
    {
        GetNode<Label>("Panel/Content/Title").Text = _item.Title;
        GetNode<Button>("Panel/Content/OpenConfirm").Pressed += _confirm;
    }
}
