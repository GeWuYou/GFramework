// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using GFramework.Game.Abstractions.UI;
using Godot;

namespace DodgeTheCreeps;

/// <summary>Builds a complete, independently editable audio, graphics and language settings view.</summary>
internal sealed partial class DodgeMenus
{
    // #region tutorial-settings
    /// <summary>Creates an audio/display draft; language changes are persisted immediately in place.</summary>
    private SettingsRoot Settings(UiViewContext context) => new SettingsEditor(this, context).Build();
    // #endregion tutorial-settings

    private Control SettingsUnsaved(UiDialogContext<SettingsCloseChoice> context)
    {
        var root = Page("SettingsUnsaved", out var column, background: false);
        Title(column, "unsaved_title", 26);
        Caption(column, "unsaved_question");
        Button(column, "ApplySettingsAndExit", "apply_changes", () => context.CompleteAsync(SettingsCloseChoice.Apply));
        Button(column, "DiscardSettingsAndExit", "discard_changes", () => context.CompleteAsync(SettingsCloseChoice.Discard));
        Button(column, "KeepEditingSettings", "cancel", () => context.CompleteAsync(SettingsCloseChoice.Cancel));
        return root;
    }

    /// <summary>Owns one detached view's draft controls and close flow; no draft is shared with preferences.</summary>
    private sealed class SettingsEditor
    {
        private readonly DodgeMenus _menus;
        private readonly UiViewContext _context;
        private readonly SettingsRoot _root;
        private HSlider _master = null!;
        private HSlider _bgm = null!;
        private HSlider _sfx = null!;
        private OptionButton _display = null!;
        private OptionButton _resolution = null!;
        private OptionButton _language = null!;
        private bool _closing;

        /// <summary>Creates a view owner without changing persisted or native audio/display settings.</summary>
        internal SettingsEditor(DodgeMenus menus, UiViewContext context)
        {
            _menus = menus;
            _context = context;
            _root = new SettingsRoot { Name = "Settings", Theme = menus._theme, MouseFilter = Control.MouseFilterEnum.Ignore };
            _root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            _root.RequestClose = () => menus._app.RunAction(RequestCloseAsync);
        }

        /// <summary>Builds controls once so language changes preserve focus, drafts and route ownership.</summary>
        internal SettingsRoot Build()
        {
            var column = CreateLayout();
            _menus.Title(column, "settings", 28);
            var scroll = new ScrollContainer
            {
                CustomMinimumSize = new Vector2(350, 300),
                SizeFlagsVertical = Control.SizeFlags.ExpandFill,
                HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            };
            var body = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            body.AddThemeConstantOverride("separation", 7);
            scroll.AddChild(body);
            column.AddChild(scroll);
            var applied = _menus._app.Preferences;
            _master = VolumeControl(body, "Volume", "master_volume", applied.Volume);
            _bgm = VolumeControl(body, "BgmVolume", "bgm_volume", applied.BgmVolume);
            _sfx = VolumeControl(body, "SfxVolume", "sfx_volume", applied.SfxVolume);
            AddGraphicsControls(body, applied);
            AddLanguageControl(body);
            _menus.Button(column, "SaveSettings", "apply_settings", ApplyAsync);
            var footer = new HBoxContainer();
            column.AddChild(footer);
            FooterButton(footer, "ResetSettings", "reset_settings", ResetAsync);
            FooterButton(footer, "Back", "back", RequestCloseAsync);
            _menus.Status(column, "settings_hint");
            return _root;
        }

        private VBoxContainer CreateLayout()
        {
            var panel = new PanelContainer { OffsetLeft = 12, OffsetRight = -12, OffsetTop = 12, OffsetBottom = -12 };
            panel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            panel.AddThemeStyleboxOverride("panel", Box(Ink, 12, 16));
            _root.AddChild(panel);
            var column = new VBoxContainer();
            column.AddThemeConstantOverride("separation", 8);
            panel.AddChild(column);
            return column;
        }

        private HSlider VolumeControl(VBoxContainer column, string name, string key, float value)
        {
            var slider = new HSlider
            {
                Name = name,
                MinValue = 0,
                MaxValue = 1,
                Step = 0.05,
                Value = value,
                CustomMinimumSize = new Vector2(350, 30),
            };
            var label = new Label { HorizontalAlignment = HorizontalAlignment.Center };
            label.AddThemeFontSizeOverride("font_size", 15);
            label.AddThemeColorOverride("font_color", new Color("b2c1d8"));
            void RefreshLabel() => label.Text = _menus._app.Text.Get(key) + "  " +
                Math.Round(slider.Value * 100, MidpointRounding.ToEven).ToString(CultureInfo.InvariantCulture) + "%";
            _menus._app.Text.Bind(label, RefreshLabel);
            column.AddChild(label);
            column.AddChild(slider);
            slider.ValueChanged += _ => { RefreshLabel(); RefreshDirtyStatus(); };
            return slider;
        }

        private void AddGraphicsControls(VBoxContainer column, DodgePreferences applied)
        {
            _menus.Caption(column, "display_mode");
            _display = new OptionButton { Name = "Fullscreen", CustomMinimumSize = new Vector2(350, 38) };
            _display.AddItem(string.Empty);
            _display.AddItem(string.Empty);
            _display.Select(applied.Fullscreen ? 0 : 1);
            _menus._app.Text.Bind(_display, () =>
            {
                _display.SetItemText(0, _menus._app.Text.Get("fullscreen"));
                _display.SetItemText(1, _menus._app.Text.Get("windowed"));
            });
            column.AddChild(_display);
            _menus.Caption(column, "resolution");
            _resolution = new OptionButton { Name = "Resolution", CustomMinimumSize = new Vector2(350, 38), Disabled = applied.Fullscreen };
            AddResolutionChoices(applied);
            column.AddChild(_resolution);
            _display.ItemSelected += _ =>
            {
                _resolution.Disabled = _display.Selected == 0;
                RefreshDirtyStatus();
            };
            _resolution.ItemSelected += _ => RefreshDirtyStatus();
        }

        private void AddResolutionChoices(DodgePreferences applied)
        {
            for (var index = 0; index < DodgePreferences.ResolutionChoices.Count; index++)
            {
                var (Width, Height) = DodgePreferences.ResolutionChoices[index];
                _resolution.AddItem(string.Create(CultureInfo.InvariantCulture, $"{Width} × {Height}"));
                if (Width == applied.ResolutionWidth && Height == applied.ResolutionHeight)
                    _resolution.Select(index);
            }
            _menus._app.Text.Bind(_resolution, () => _resolution.SetItemText(1, _menus._app.Text.Get("resolution_portrait")));
        }

        private void AddLanguageControl(VBoxContainer column)
        {
            _menus.Caption(column, "language");
            _language = new OptionButton { Name = "Language", CustomMinimumSize = new Vector2(350, 38) };
            _language.AddItem("English");
            _language.AddItem("简体中文");
            _menus._app.Text.Bind(_language, () => _language.Select(_menus._app.Text.IsChinese ? 1 : 0));
            _language.ItemSelected += index => _menus._app.RunAction(async () =>
            {
                _language.Disabled = true;
                try
                {
                    await _menus._app.SetLanguageAsync(index == 1 ? "zh-CN" : "en").ConfigureAwait(true);
                }
                finally { if (GodotObject.IsInstanceValid(_language)) _language.Disabled = false; }
            });
            column.AddChild(_language);
        }

        private void FooterButton(HBoxContainer row, string name, string key, Func<Task> action)
        {
            var button = new Button { Name = name, CustomMinimumSize = new Vector2(0, 44), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            button.AddThemeFontSizeOverride("font_size", 14);
            _menus._app.Text.Bind(button, () => button.Text = _menus._app.Text.Get(key));
            row.AddChild(button);
            button.Pressed += () => _menus._app.RunAction(async () =>
            {
                button.Disabled = true;
                try
                {
                    await action().ConfigureAwait(true);
                }
                finally { if (GodotObject.IsInstanceValid(button)) button.Disabled = false; }
            });
        }

        private DodgePreferences ReadDraft()
        {
            var index = _resolution.Selected;
            if (index < 0 || index >= DodgePreferences.ResolutionChoices.Count)
                throw new InvalidOperationException("Select a supported output resolution.");
            var (Width, Height) = DodgePreferences.ResolutionChoices[index];
            return new DodgePreferences
            {
                Volume = (float)_master.Value,
                BgmVolume = (float)_bgm.Value,
                SfxVolume = (float)_sfx.Value,
                Fullscreen = _display.Selected == 0,
                ResolutionWidth = Width,
                ResolutionHeight = Height,
                Language = _menus._app.Preferences.Language,
                BestScore = _menus._app.Preferences.BestScore,
            };
        }

        private async Task ApplyAsync()
        {
            await _menus._app.SaveSettingsAsync(ReadDraft()).ConfigureAwait(true);
            RefreshDirtyStatus();
        }

        private async Task ResetAsync()
        {
            var defaults = new DodgePreferences();
            _master.Value = defaults.Volume;
            _bgm.Value = defaults.BgmVolume;
            _sfx.Value = defaults.SfxVolume;
            _display.Select(1);
            _resolution.Select(1);
            _resolution.Disabled = false;
            var language = OS.GetLocale().StartsWith("zh", StringComparison.OrdinalIgnoreCase) ? "zh-CN" : "en";
            await _menus._app.SetLanguageAsync(language).ConfigureAwait(true);
            _menus.SetStatus(() => _menus._app.Text.Get("settings_reset_hint"));
        }

        private async Task RequestCloseAsync()
        {
            if (_closing)
                return;
            _closing = true;
            try
            {
                var draft = ReadDraft();
                if (_menus._app.HasPendingSettings(draft))
                {
                    var choice = await _menus._app.Ui.ShowDialogAsync(DodgeRoutes.SettingsUnsaved, args: false).ConfigureAwait(true);
                    if (!choice.Accepted || choice.Value == SettingsCloseChoice.Cancel)
                        return;
                    if (choice.Value == SettingsCloseChoice.Apply)
                        await _menus._app.SaveSettingsAsync(draft).ConfigureAwait(true);
                }
                await _context.CloseAsync().ConfigureAwait(true);
            }
            finally { _closing = false; }
        }

        private void RefreshDirtyStatus()
        {
            if (_master is null || _bgm is null || _sfx is null || _display is null || _resolution is null)
                return;
            _menus.SetStatus(() => _menus._app.Text.Get(_menus._app.HasPendingSettings(ReadDraft()) ? "settings_unsaved_hint" : "settings_hint"));
        }
    }

    /// <summary>Consumes Escape and schedules close decisions outside the UI service's serialized dispatch callback.</summary>
    private sealed partial class SettingsRoot : Control, IUiActionTarget
    {
        /// <summary>Gets or sets the observed close-request scheduler supplied by this view's owner.</summary>
        internal Action? RequestClose
        {
            get; set;
        }

        /// <inheritdoc />
        public Task<bool> HandleActionAsync(UiInputAction action, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (action != UiInputAction.Cancel)
                return Task.FromResult(false);
            // DispatchAction owns the UI service's serialized operation. A native deferred
            // callback leaves that operation's execution context before requesting another
            // route, rather than reentering its queue from the action-target callback.
            Callable.From(() =>
            {
                if (GodotObject.IsInstanceValid(this) && IsInsideTree() && !IsQueuedForDeletion())
                    RequestClose?.Invoke();
            }).CallDeferred();
            return Task.FromResult(true);
        }
    }
}
