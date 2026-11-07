// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading.Tasks;
using GFramework.Game.Abstractions.UI;
using GFramework.Game.UI;
using GFramework.Godot.UI;
using Godot;

namespace DodgeTheCreeps;

/// <summary>Builds ordinary native views; typed registration supplies arguments and instance actions.</summary>
internal sealed class DodgeMenus : IDisposable
{
    private static readonly Color Ink = new("101b30");
    private static readonly Color Mint = new("72efd0");
    private readonly DodgeApp _app;
    private readonly Theme _theme;
    private Label? _score;
    private Label? _status;

    /// <summary>Creates one menu composer with explicit business actions.</summary>
    internal DodgeMenus(DodgeApp app)
    {
        _app = app;
        _theme = new Theme { DefaultFont = GD.Load<Font>("res://fonts/Xolonium-Regular.ttf"), DefaultFontSize = 20 };
        _theme.SetColor("font_color", "Label", new Color("ecf5ff"));
        _theme.SetStylebox("normal", "Button", Box(new Color("24374f"), 10));
        _theme.SetStylebox("hover", "Button", Box(new Color("34516c"), 10));
        _theme.SetStylebox("pressed", "Button", Box(new Color("126e69"), 10));
        _theme.SetStylebox("focus", "Button", new StyleBoxFlat { BorderColor = Mint, BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2, DrawCenter = false, CornerRadiusTopLeft = 10, CornerRadiusTopRight = 10, CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10 });
    }

    /// <summary>Releases the explicitly owned theme after native views have left the tree.</summary>
    public void Dispose() => _theme.Dispose();

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
    }
    // #endregion tutorial-register

    /// <summary>Updates a live HUD without retaining gameplay event subscriptions in the view.</summary>
    internal void SetScore(int score)
    {
        if (_score is not null && GodotObject.IsInstanceValid(_score)) _score.Text = score.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
    /// <summary>Shows operation feedback in the currently open menu.</summary>
    internal void SetStatus(string text)
    {
        if (_status is not null && GodotObject.IsInstanceValid(_status)) _status.Text = text;
    }

    /// <summary>Creates the entry page with a directly inspectable saved-run choice.</summary>
    private Control Menu(bool canContinue)
    {
        var root = Page("Menu", out var column);
        var art = new TextureRect
        {
            Texture = GD.Load<Texture2D>("res://art/playerGrey_walk1.png"),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            CustomMinimumSize = new Vector2(90, 90), MouseFilter = Control.MouseFilterEnum.Ignore
        };
        column.AddChild(art);
        Title(column, "DODGE THE\nCREEPS!", 38);
        Caption(column, "Survive to increase your score.");
        Gap(column, 12);
        Button(column, "Start", "NEW RUN", () => _app.StartGameAsync(false));
        Button(column, "Continue", "CONTINUE SAVED RUN", () => _app.StartGameAsync(true)).Disabled = !canContinue;
        Button(column, "Settings", "SETTINGS", _app.OpenSettingsAsync);
        QuitButton(column);
        Caption(column, "BEST  " + _app.Preferences.BestScore);
        Status(column, "Move with WASD or arrow keys.\nEscape pauses the game.");
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
        var hint = new Label { Text = "WASD / ARROWS", Position = new Vector2(26, 680), MouseFilter = Control.MouseFilterEnum.Ignore };
        hint.AddThemeColorOverride("font_color", new Color("657b99"));
        hint.AddThemeFontSizeOverride("font_size", 14);
        root.AddChild(hint);
        return root;
    }

    /// <summary>Creates a pause window; the route policy owns its pause request.</summary>
    private Control Pause(int score, UiViewContext context)
    {
        var root = Page("Pause", out var column, false);
        Title(column, "PAUSED", 28);
        Caption(column, "SURVIVED  " + score + "s");
        Button(column, "Resume", "RESUME", () => context.CloseAsync());
        Button(column, "Save", "SAVE THIS RUN", _app.SaveGameAsync);
        Button(column, "Settings", "SETTINGS", _app.OpenSettingsAsync);
        Button(column, "MainMenu", "MAIN MENU", _app.ReturnToMenuAsync);
        Status(column, "Escape closes the top menu.");
        return root;
    }

    // #region tutorial-settings
    /// <summary>Creates settings with local draft values and explicit save.</summary>
    private Control Settings(UiViewContext context)
    {
        var root = Page("Settings", out var column, false);
        Title(column, "SETTINGS", 30);
        Caption(column, "MASTER VOLUME");
        var volume = new HSlider { Name = "Volume", MinValue = 0, MaxValue = 1, Step = 0.05, Value = _app.Preferences.Volume, CustomMinimumSize = new Vector2(330, 38) };
        column.AddChild(volume);
        var fullscreen = new CheckButton { Name = "Fullscreen", Text = "Fullscreen", ButtonPressed = _app.Preferences.Fullscreen };
        column.AddChild(fullscreen);
        Button(column, "SaveSettings", "SAVE SETTINGS", () => _app.SavePreferencesAsync((float)volume.Value, fullscreen.ButtonPressed));
        Button(column, "Back", "BACK", () => context.CloseAsync());
        Status(column, "Changes are applied when saved.");
        return root;
    }
    // #endregion tutorial-settings

    /// <summary>Shows the completed score and offers a fresh run.</summary>
    private Control GameOver(int score)
    {
        var root = Page("GameOver", out var column);
        Title(column, "CAUGHT!", 46);
        Caption(column, "YOU SURVIVED");
        Title(column, score + "s", 64);
        Caption(column, "BEST  " + _app.Preferences.BestScore + "s");
        Button(column, "Retry", "TRY AGAIN", () => _app.StartGameAsync(false));
        Button(column, "MainMenu", "MAIN MENU", _app.ReturnToMenuAsync);
        Status(column, "Keep moving. Give the creeps some space.");
        return root;
    }

    // #region tutorial-confirm
    /// <summary>Returns a typed decision through the dialog's own completion context.</summary>
    private Control Confirm(string message, UiDialogContext<bool> context)
    {
        var root = Page("Confirm", out var column, false);
        Title(column, "LEAVE RUN?", 30);
        Caption(column, message);
        Button(column, "KeepPlaying", "KEEP PLAYING", () => context.CompleteAsync(false));
        Button(column, "Leave", "LEAVE WITHOUT SAVING", () => context.CompleteAsync(true));
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
    private Button Button(VBoxContainer column, string name, string text, Func<Task> action)
    {
        var button = new Button { Name = name, Text = text, CustomMinimumSize = new Vector2(350, 48) };
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
        var button = new Button { Name = "Quit", Text = "QUIT", CustomMinimumSize = new Vector2(350, 45) };
        column.AddChild(button);
        button.Pressed += _app.RequestQuit;
    }
    // #endregion tutorial-buttons
    /// <summary>Adds a high-contrast title.</summary>
    private static void Title(VBoxContainer column, string text, int size)
    {
        var label = new Label { Text = text, HorizontalAlignment = HorizontalAlignment.Center };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", Mint);
        column.AddChild(label);
    }
    /// <summary>Adds readable secondary text with wrapping for narrow viewports.</summary>
    private static void Caption(VBoxContainer column, string text)
    {
        var label = new Label { Text = text, HorizontalAlignment = HorizontalAlignment.Center, AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(350, 0) };
        label.AddThemeFontSizeOverride("font_size", 15);
        label.AddThemeColorOverride("font_color", new Color("b2c1d8"));
        column.AddChild(label);
    }
    /// <summary>Creates the operation feedback label for this menu.</summary>
    private void Status(VBoxContainer column, string text)
    {
        _status = new Label { Name = "Status", Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(350, 0), HorizontalAlignment = HorizontalAlignment.Center };
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
