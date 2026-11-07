// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Localization;
using GFramework.Game.Abstractions.Setting;
using GFramework.Game.Abstractions.Setting.Data;
using GFramework.Godot.Setting.Data;

namespace GFramework.Godot.Setting;

/// <summary>Applies persisted language to Godot and an explicitly injected localization manager.</summary>
public sealed class GodotLocalizationSettings : IResetApplyAbleSettings
{
    private readonly ISettingsModel _model;
    private readonly LocalizationMap _map;
    private readonly ILocalizationManager? _manager;
    private readonly Action<string> _applyLocale;

    /// <summary>Creates a language applicator without resolving an architecture context.</summary>
    /// <param name="model">Settings data service.</param>
    /// <param name="localizationMap">Display name to language-code mapping.</param>
    /// <param name="manager">Optional framework localization manager.</param>
    /// <param name="applyGodotLocale">Optional engine bridge, defaulting to TranslationServer.SetLocale.</param>
    public GodotLocalizationSettings(ISettingsModel model, LocalizationMap localizationMap,
        ILocalizationManager? manager = null, Action<string>? applyGodotLocale = null)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _map = localizationMap ?? throw new ArgumentNullException(nameof(localizationMap));
        _manager = manager;
        _applyLocale = applyGodotLocale ?? TranslationServer.SetLocale;
    }

    /// <inheritdoc />
    public Task ApplyAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var language = _model.GetData<LocalizationSettings>().Language;
        _applyLocale(_map.ResolveGodotLocale(language));
        _manager?.SetLanguage(_map.ResolveFrameworkLanguage(language));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Reset() => _model.GetData<LocalizationSettings>().Reset();
    /// <inheritdoc />
    public ISettingsData Data => _model.GetData<LocalizationSettings>();
    /// <inheritdoc />
    public Type DataType => typeof(LocalizationSettings);
}
