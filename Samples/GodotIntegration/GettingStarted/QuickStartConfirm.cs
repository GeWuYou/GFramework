// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading.Tasks;
using GFramework.Game.Abstractions.UI;
using Godot;

namespace GodotIntegration.GettingStarted;

/// <summary>Completes its typed result through a host-observed response that can run while the opener waits.</summary>
public partial class QuickStartConfirm : CenterContainer
{
    private string _question = string.Empty;
    private UiDialogContext<bool> _context = null!;
    private Action<Func<Task>> _run = null!;

    /// <summary>Receives the dialog's own completion context before entering the tree.</summary>
    /// <param name="question">Opening argument.</param>
    /// <param name="context">The owning UI instance.</param>
    /// <param name="run">Observed response admission.</param>
    public void Configure(string question, UiDialogContext<bool> context, Action<Func<Task>> run)
    {
        _question = question;
        _context = context;
        _run = run;
    }

    /// <inheritdoc />
    public override void _Ready()
    {
        GetNode<Label>("Panel/Content/Question").Text = _question;
        GetNode<Button>("Panel/Content/Accept").Pressed += () => _run(() => _context.CompleteAsync(result: true));
        GetNode<Button>("Panel/Content/Cancel").Pressed += () => _run(() => _context.CloseAsync());
    }
}
