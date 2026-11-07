// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Threading.Tasks;
using DodgeTheCreeps.Content;
using GFramework.Game.Abstractions.Content;
using GFramework.Game.Abstractions.UI;
using Godot;

namespace DodgeTheCreeps;

/// <summary>Builds conventional package selection and explicit save-repair views.</summary>
internal sealed partial class DodgeMenus
{
    private Control ContentManager(UiViewContext context)
    {
        var root = FullRect("Content");
        var panel = new PanelContainer { OffsetLeft = 12, OffsetRight = -12, OffsetTop = 12, OffsetBottom = -12 };
        panel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        panel.AddThemeStyleboxOverride("panel", Box(Ink, 12, 16));
        root.AddChild(panel);
        var scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        panel.AddChild(scroll);
        var column = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(390, 0) };
        column.AddThemeConstantOverride("separation", 9);
        scroll.AddChild(column);
        Title(column, "content", 28);
        Caption(column, "content_hint");
        AddContentPackages(column, context);
        Caption(column, () => _app.Text.Get("next_start", ("order", _app.Content.DescribeRequestedOrder())));
        Caption(column, DescribeWalker);
        if (_app.Content.IsReady)
        {
            var arena = _app.Content.Catalog.GetTable<DodgeArenaDefinition>("arenas").Get(new ContentId(_app.Content.ArenaId));
            Button(column, "ContentArena", () => _app.Text.Get("arena", ("name", _app.Text.ArenaName(arena.Id, arena.Name))),
                () => ReopenContentAsync(context, _app.CycleArenaAsync));
        }
        if (_app.Content.Error is not null || _app.Content.DiscoveryErrors.Count > 0) Caption(column, "diagnostics_heading");
        if (_app.Content.Error is not null) Caption(column, () => _app.Content.Error ?? string.Empty);
        foreach (var error in _app.Content.DiscoveryErrors) Caption(column, () => error);
        Button(column, "ResetContent", "reset_content", () => ReopenContentAsync(context, _app.ResetContentAsync));
        Button(column, "RestoreContentBackup", "restore_backup", _app.RestoreContentBackupAsync).Disabled = !_app.HasBackup;
        Button(column, "ContentBack", "back", () => context.CloseAsync());
        Status(column, _app.Content.PendingRestart ? "pending_restart" : "no_pending");
        return root;
    }

    private string DescribeWalker()
    {
        if (!_app.Content.IsReady) return _app.Text.Get("content_unavailable");
        var enemy = _app.Content.Catalog.GetTable<DodgeEnemyDefinition>("enemies").Get(new ContentId("base:walker"));
        var source = _app.Content.Catalog.GetFieldSource(new ContentReference("enemies", new ContentId("base:walker")), "speedMin");
        var package = source?.PackId ?? "base";
        return _app.Text.Get("walker_speed", ("min", enemy.SpeedMin.ToString("0", System.Globalization.CultureInfo.InvariantCulture)),
            ("max", enemy.SpeedMax.ToString("0", System.Globalization.CultureInfo.InvariantCulture)),
            ("source", _app.Text.PackageName(package, package)));
    }

    private void AddContentPackages(VBoxContainer column, UiViewContext context)
    {
        var packages = _app.Content.Packages.OrderBy(pack => PreferredIndex(pack.Manifest.Id))
            .ThenBy(static pack => pack.Manifest.Id, StringComparer.Ordinal).ToArray();
        foreach (var pack in packages)
        {
            var id = pack.Manifest.Id;
            var row = new HBoxContainer();
            column.AddChild(row);
            var enabled = string.Equals(id, "base", StringComparison.Ordinal) || _app.Content.RequestedProfile.EnabledPackages.Contains(id, StringComparer.Ordinal);
            var toggle = new Button
            {
                Name = "Toggle_" + id,
                CustomMinimumSize = new Vector2(285, 45), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                Disabled = string.Equals(id, "base", StringComparison.Ordinal), ClipText = true
            };
            _app.Text.Bind(toggle, () =>
            {
                toggle.Text = (enabled ? "[x] " : "[ ] ") + _app.Text.PackageName(id, pack.Manifest.Name);
                toggle.TooltipText = _app.Text.PackageName(id, pack.Manifest.Name);
            });
            toggle.AddThemeFontSizeOverride("font_size", 15);
            row.AddChild(toggle);
            ConnectContentAction(toggle, context, () => _app.ToggleContentAsync(id));
            if (!string.Equals(id, "base", StringComparison.Ordinal))
            {
                ContentOrderButton(row, "Up_" + id, "↑", context, () => _app.MoveContentAsync(id, -1));
                ContentOrderButton(row, "Down_" + id, "↓", context, () => _app.MoveContentAsync(id, 1));
            }
        }
        var knownIds = packages.Select(static pack => pack.Manifest.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var missing in _app.Content.RequestedProfile.EnabledPackages.Where(id => !knownIds.Contains(id)))
            Button(column, "DisableMissing_" + missing, () => _app.Text.Get("disable_missing", ("id", missing)),
                () => ReopenContentAsync(context, () => _app.ToggleContentAsync(missing)));

    }

    private int PreferredIndex(string id)
    {
        if (string.Equals(id, "base", StringComparison.Ordinal)) return -1;
        var order = _app.Content.RequestedProfile.LoadOrder;
        for (var index = 0; index < order.Count; index++)
            if (string.Equals(order[index], id, StringComparison.Ordinal)) return index;
        return int.MaxValue;
    }

    private void ContentOrderButton(HBoxContainer row, string name, string text, UiViewContext context, Func<Task> action)
    {
        var button = new Button { Name = name, Text = text, CustomMinimumSize = new Vector2(38, 45) };
        row.AddChild(button);
        ConnectContentAction(button, context, action);
    }

    private void ConnectContentAction(Button button, UiViewContext context, Func<Task> action)
    {
        button.Pressed += () => _app.RunAction(async () =>
        {
            button.Disabled = true;
            try { await ReopenContentAsync(context, action).ConfigureAwait(true); }
            finally { if (GodotObject.IsInstanceValid(button) && !button.IsQueuedForDeletion()) button.Disabled = false; }
        });
    }

    private async Task ReopenContentAsync(UiViewContext context, Func<Task> action)
    {
        await action().ConfigureAwait(true);
        await context.CloseAsync().ConfigureAwait(true);
        await _app.OpenContentAsync().ConfigureAwait(true);
    }

    private Control Recover(Func<string> message, UiDialogContext<bool> context)
    {
        var root = Page("Recover", out var column, false);
        Title(column, "save_content_title", 26);
        var scroll = new ScrollContainer
        {
            Name = "RecoveryDetails", CustomMinimumSize = new Vector2(350, 240),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
        };
        var details = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        _app.Text.Bind(details, () => details.Text = message());
        details.AddThemeFontSizeOverride("font_size", 15);
        details.AddThemeColorOverride("font_color", new Color("b2c1d8"));
        scroll.AddChild(details);
        column.AddChild(scroll);
        Button(column, "RecoveryCancel", "cancel", () => context.CompleteAsync(false));
        Button(column, "RecoveryProceed", "back_up_continue", () => context.CompleteAsync(true));
        return root;
    }
}
