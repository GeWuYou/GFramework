// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GFramework.Game.Abstractions.UI;
using GFramework.Game.UI;
using GFramework.Godot.UI;
using Godot;

namespace DodgeTheCreeps;

/// <summary>Builds ordinary native views; typed registration supplies arguments and instance actions.</summary>
internal sealed partial class DodgeMenus : IDisposable
{
    private static readonly Color Ink = new("101b30");
    private static readonly Color Mint = new("72efd0");
    private readonly DodgeApp _app;
    private readonly Theme _theme;
    private readonly FontVariation _englishFont;
    private readonly FontVariation _chineseFont;
    private Label? _score;
    private Label? _status;
    private readonly Dictionary<Label, Func<string>> _statusText = new();

    /// <summary>Creates one menu composer with explicit business actions.</summary>
    internal DodgeMenus(DodgeApp app)
    {
        _app = app;
        _chineseFont = DodgeLocalization.CreateChineseFont();
        _englishFont = new FontVariation
        {
            BaseFont = GD.Load<Font>("res://fonts/Xolonium-Regular.ttf"),
            Fallbacks = new Godot.Collections.Array<Font> { _chineseFont }
        };
        _theme = new Theme { DefaultFont = _englishFont, DefaultFontSize = 20 };
        _theme.SetColor("font_color", "Label", new Color("ecf5ff"));
        _theme.SetStylebox("normal", "Button", Box(new Color("24374f"), 10));
        _theme.SetStylebox("hover", "Button", Box(new Color("34516c"), 10));
        _theme.SetStylebox("pressed", "Button", Box(new Color("126e69"), 10));
        _theme.SetStylebox("focus", "Button", new StyleBoxFlat { BorderColor = Mint, BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2, DrawCenter = false, CornerRadiusTopLeft = 10, CornerRadiusTopRight = 10, CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10 });
        _app.Text.Bind(_app, () => _theme.DefaultFont = _app.Text.IsChinese ? _chineseFont : _englishFont);
    }

    /// <summary>Releases owned theme/font variations after native views stop; shared GD-loaded font files remain borrowed.</summary>
    public void Dispose()
    {
        _theme.Dispose();
        _englishFont.Dispose();
        _chineseFont.Dispose();
    }

    // #region tutorial-register
    /// <summary>Registers each view once; call sites share the same typed definitions.</summary>
    internal void Register(UiRegistry registry)
    {
        registry.RegisterGodot(DodgeRoutes.Menu, (canContinue, _) => Menu(canContinue));
        registry.RegisterGodot(DodgeRoutes.Hud, (score, _) => Hud(score));
        registry.RegisterGodot(DodgeRoutes.Pause, (score, context) => Pause(score, context));
        registry.RegisterGodot(DodgeRoutes.Settings, (_, context) => Settings(context));
        registry.RegisterGodot(DodgeRoutes.GameOver, (score, _) => GameOver(score));
        registry.RegisterGodotDialog(DodgeRoutes.Confirm, (message, context) => Confirm(message, context));
        registry.RegisterGodot(DodgeRoutes.Content, (_, context) => ContentManager(context));
        registry.RegisterGodotDialog(DodgeRoutes.Recover, (message, context) => Recover(message, context));
        registry.RegisterGodotDialog(DodgeRoutes.SettingsUnsaved, (_, context) => SettingsUnsaved(context));
    }
    // #endregion tutorial-register

    /// <summary>Updates a live HUD without retaining gameplay event subscriptions in the view.</summary>
    internal void SetScore(int score)
    {
        if (_score is not null && GodotObject.IsInstanceValid(_score)) _score.Text = score.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
    /// <summary>Shows operation feedback in the currently open menu.</summary>
    internal void SetStatus(string text) => SetStatus(() => text);
    /// <summary>Keeps operation feedback bound to the active locale without replacing its view.</summary>
    internal void SetStatus(Func<string> text)
    {
        if (_status is null || !GodotObject.IsInstanceValid(_status) || _status.IsQueuedForDeletion() || !_status.IsInsideTree())
            _status = _statusText.Keys.LastOrDefault(static label => GodotObject.IsInstanceValid(label) &&
                !label.IsQueuedForDeletion() && label.IsInsideTree());
        if (_status is not null)
        {
            _statusText[_status] = text;
            _status.Text = text();
        }
    }

    /// <summary>Creates the entry page with a directly inspectable saved-run choice.</summary>
    private Control Menu(bool canContinue)
    {
        var root = Page("Menu", out var column);
        // The bilingual fallback font has taller line metrics; keep the complete entry page
        // inside the portrait canvas without shrinking labels or removing available actions.
        column.AddThemeConstantOverride("separation", 8);
        var art = new TextureRect
        {
            Texture = GD.Load<Texture2D>("res://art/playerGrey_walk1.png"),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            CustomMinimumSize = new Vector2(90, 90), MouseFilter = Control.MouseFilterEnum.Ignore
        };
        column.AddChild(art);
        Title(column, "game_title", 38);
        Caption(column, "game_subtitle");
        Gap(column, 12);
        Button(column, "Start", "new_run", () => _app.StartGameAsync(false)).Disabled = !_app.CanStart;
        Button(column, "Continue", "continue_run", () => _app.StartGameAsync(true)).Disabled = !canContinue || !_app.CanStart;
        Button(column, "Content", "content", _app.OpenContentAsync);
        Button(column, "Settings", "settings", _app.OpenSettingsAsync);
        QuitButton(column);
        Caption(column, () => _app.Text.Get("best", ("score", _app.Preferences.BestScore)));
        Status(column, "controls");
        return root;
    }

    /// <summary>Creates a pointer-transparent HUD with its own explicit pause button.</summary>
    private Control Hud(int score)
    {
        var root = FullRect("Hud");
        root.MouseFilter = Control.MouseFilterEnum.Ignore;
        _score = new Label { Name = "Score", Text = score.ToString(System.Globalization.CultureInfo.InvariantCulture), Position = new Vector2(26, 18), MouseFilter = Control.MouseFilterEnum.Ignore };
        _score.AddThemeFontSizeOverride("font_size", 36);
        root.AddChild(_score);
        var pause = new Button { Name = "Pause", Text = "II", Position = new Vector2(397, 18), Size = new Vector2(58, 48) };
        pause.Pressed += () => _app.RunAction(() => _app.Ui.ShowAsync(DodgeRoutes.Pause, _app.World?.Score ?? 0));
        root.AddChild(pause);
        var hint = new Label
        {
            Name = "HudStatus", Position = new Vector2(26, 646), Size = new Vector2(428, 68),
            MouseFilter = Control.MouseFilterEnum.Ignore, AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        // Keep restored-layout notices observable after the menu is replaced by gameplay.
        _status = hint;
        _statusText.Add(hint, () => _app.Text.Get("hud_controls"));
        _app.Text.Bind(hint, () => hint.Text = _statusText[hint]());
        hint.TreeExiting += () => _statusText.Remove(hint);
        hint.AddThemeColorOverride("font_color", new Color("8dacc2"));
        hint.AddThemeFontSizeOverride("font_size", 14);
        root.AddChild(hint);
        return root;
    }

    /// <summary>Creates a pause window; the route policy owns its pause request.</summary>
    private Control Pause(int score, UiViewContext context)
    {
        var root = Page("Pause", out var column, false);
        Title(column, "paused", 28);
        Caption(column, () => _app.Text.Get("survived_seconds", ("score", score)));
        Button(column, "Resume", "resume", () => context.CloseAsync());
        Button(column, "Save", "save_run", _app.SaveGameAsync);
        Button(column, "Settings", "settings", _app.OpenSettingsAsync);
        Button(column, "MainMenu", "main_menu", _app.ReturnToMenuAsync);
        Status(column, "escape_hint");
        return root;
    }

    /// <summary>Shows the completed score and offers a fresh run.</summary>
    private Control GameOver(int score)
    {
        var root = Page("GameOver", out var column);
        Title(column, "caught", 46);
        Caption(column, "you_survived");
        Title(column, () => _app.Text.Get("seconds", ("score", score)), 64);
        Caption(column, () => _app.Text.Get("best_seconds", ("score", _app.Preferences.BestScore)));
        Button(column, "Retry", "retry", () => _app.StartGameAsync(false));
        Button(column, "MainMenu", "main_menu", _app.ReturnToMenuAsync);
        Status(column, "gameover_hint");
        return root;
    }

    // #region tutorial-confirm
    /// <summary>Returns a typed decision through the dialog's own completion context.</summary>
    private Control Confirm(Func<string> message, UiDialogContext<bool> context)
    {
        var root = Page("Confirm", out var column, false);
        Title(column, "leave_title", 30);
        Caption(column, message);
        Button(column, "KeepPlaying", "keep_playing", () => context.CompleteAsync(false));
        Button(column, "Leave", "leave_without_saving", () => context.CompleteAsync(true));
        return root;
    }
    // #endregion tutorial-confirm

    /// <summary>Creates a centered, authored Control hierarchy usable by the default host.</summary>
    private Control Page(string name, out VBoxContainer column, bool background = true)
    {
        var root = FullRect(name);
        if (background)
        {
            var backdrop = new ColorRect { Color = new Color("091222"), MouseFilter = Control.MouseFilterEnum.Stop };
            backdrop.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            root.AddChild(backdrop);
        }
        var center = new CenterContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(center);
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(408, 0) };
        panel.AddThemeStyleboxOverride("panel", Box(Ink, 18, 25));
        center.AddChild(panel);
        column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 13);
        panel.AddChild(column);
        return root;
    }
    /// <summary>Creates a full-viewport native view root.</summary>
    private Control FullRect(string name)
    {
        var control = new Control { Name = name, Theme = _theme, MouseFilter = Control.MouseFilterEnum.Ignore };
        control.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        return control;
    }
    // #region tutorial-buttons
    /// <summary>Connects one button to an observed task and prevents double submission on that button.</summary>
    private Button Button(VBoxContainer column, string name, string key, Func<Task> action)
        => Button(column, name, () => _app.Text.Get(key), action);

    /// <summary>Connects a live translated label to an independently observed native action.</summary>
    private Button Button(VBoxContainer column, string name, Func<string> text, Func<Task> action)
    {
        var button = new Button { Name = name, CustomMinimumSize = new Vector2(350, 48) };
        _app.Text.Bind(button, () => button.Text = text());
        button.AddThemeFontSizeOverride("font_size", 17);
        column.AddChild(button);
        button.Pressed += () => _app.RunAction(async () =>
        {
            button.Disabled = true;
            try { await action().ConfigureAwait(true); }
            finally { if (GodotObject.IsInstanceValid(button) && !button.IsQueuedForDeletion()) button.Disabled = false; }
        });
        return button;
    }
    /// <summary>Quit owns a separate shutdown boundary, so shutdown never awaits itself.</summary>
    private void QuitButton(VBoxContainer column)
    {
        var button = new Button { Name = "Quit", CustomMinimumSize = new Vector2(350, 45) };
        _app.Text.Bind(button, () => button.Text = _app.Text.Get("quit"));
        column.AddChild(button);
        button.Pressed += _app.RequestQuit;
    }
    // #endregion tutorial-buttons
    /// <summary>Adds a high-contrast title.</summary>
    private void Title(VBoxContainer column, string key, int size) => Title(column, () => _app.Text.Get(key), size);
    /// <summary>Adds a title whose formatted values remain bound to the active locale.</summary>
    private void Title(VBoxContainer column, Func<string> text, int size)
    {
        var label = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        _app.Text.Bind(label, () => label.Text = text());
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", Mint);
        column.AddChild(label);
    }
    /// <summary>Adds readable secondary text with wrapping for narrow viewports.</summary>
    private void Caption(VBoxContainer column, string key) => Caption(column, () => _app.Text.Get(key));
    /// <summary>Adds a wrapped caption that refreshes without replacing its owner view.</summary>
    private void Caption(VBoxContainer column, Func<string> text)
    {
        var label = new Label { HorizontalAlignment = HorizontalAlignment.Center, AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(350, 0) };
        _app.Text.Bind(label, () => label.Text = text());
        label.AddThemeFontSizeOverride("font_size", 15);
        label.AddThemeColorOverride("font_color", new Color("b2c1d8"));
        column.AddChild(label);
    }
    /// <summary>Creates the operation feedback label for this menu.</summary>
    private void Status(VBoxContainer column, string key)
    {
        _status = new Label { Name = "Status", AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(350, 0), HorizontalAlignment = HorizontalAlignment.Center };
        var label = _status;
        _statusText.Add(label, () => _app.Text.Get(key));
        _app.Text.Bind(label, () => label.Text = _statusText[label]());
        label.TreeExiting += () => _statusText.Remove(label);
        _status.AddThemeFontSizeOverride("font_size", 13);
        _status.AddThemeColorOverride("font_color", new Color("8dacc2"));
        column.AddChild(_status);
    }
    /// <summary>Adds intentional breathing room between title and actions.</summary>
    private static void Gap(VBoxContainer column, float height) => column.AddChild(new Control { CustomMinimumSize = new Vector2(0, height) });
    /// <summary>Creates a rounded native style with consistent internal spacing.</summary>
    private static StyleBoxFlat Box(Color color, int radius, int padding = 12) => new()
    {
        BgColor = color, CornerRadiusTopLeft = radius, CornerRadiusTopRight = radius,
        CornerRadiusBottomLeft = radius, CornerRadiusBottomRight = radius,
        ContentMarginLeft = padding, ContentMarginRight = padding, ContentMarginTop = padding, ContentMarginBottom = padding
    };
}
