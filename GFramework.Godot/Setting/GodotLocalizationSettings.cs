// Copyright (c) 2026 GeWuYou
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using GFramework.Core.Abstractions.Localization;
using GFramework.Core.Architectures;
using GFramework.Game.Abstractions.Setting;
using GFramework.Game.Abstractions.Setting.Data;
using GFramework.Godot.Setting.Data;

namespace GFramework.Godot.Setting;

/// <summary>
///     Godot 本地化设置类，负责将持久化语言配置同时应用到 Godot 引擎与 GFramework 本地化管理器。
/// </summary>
public class GodotLocalizationSettings : IResetApplyAbleSettings
{
    private readonly Action<string> _applyGodotLocale;
    private readonly Func<ILocalizationManager?> _localizationManagerResolver;
    private readonly LocalizationMap _localizationMap;
    private readonly ISettingsModel _model;

    /// <summary>
    ///     初始化 Godot 本地化设置应用器，并默认从当前架构上下文解析框架本地化管理器。
    /// </summary>
    /// <param name="model">设置模型。</param>
    /// <param name="localizationMap">本地化映射表。</param>
    public GodotLocalizationSettings(ISettingsModel model, LocalizationMap localizationMap)
        : this(model, localizationMap, TryResolveLocalizationManager, TranslationServer.SetLocale)
    {
    }

    /// <summary>
    ///     初始化 Godot 本地化设置应用器。
    /// </summary>
    /// <param name="model">设置模型。</param>
    /// <param name="localizationMap">本地化映射表。</param>
    /// <param name="localizationManagerResolver">框架本地化管理器解析器。</param>
    public GodotLocalizationSettings(
        ISettingsModel model,
        LocalizationMap localizationMap,
        Func<ILocalizationManager?> localizationManagerResolver)
        : this(model, localizationMap, localizationManagerResolver, TranslationServer.SetLocale)
    {
    }

    /// <summary>
    ///     初始化 Godot 本地化设置应用器，并显式指定 Godot locale 应用动作。
    ///     该重载主要用于测试或自定义引擎桥接。
    /// </summary>
    /// <param name="model">设置模型。</param>
    /// <param name="localizationMap">本地化映射表。</param>
    /// <param name="localizationManagerResolver">框架本地化管理器解析器。</param>
    /// <param name="applyGodotLocale">Godot locale 应用动作。</param>
    public GodotLocalizationSettings(
        ISettingsModel model,
        LocalizationMap localizationMap,
        Func<ILocalizationManager?> localizationManagerResolver,
        Action<string> applyGodotLocale)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _localizationMap = localizationMap ?? throw new ArgumentNullException(nameof(localizationMap));
        _localizationManagerResolver =
            localizationManagerResolver ?? throw new ArgumentNullException(nameof(localizationManagerResolver));
        _applyGodotLocale = applyGodotLocale ?? throw new ArgumentNullException(nameof(applyGodotLocale));
    }

    /// <summary>
    ///     应用本地化设置到 Godot 引擎与 GFramework 本地化管理器。
    /// </summary>
    /// <returns>完成的任务</returns>
    public Task Apply()
    {
        var settings = _model.GetData<LocalizationSettings>();
        var locale = _localizationMap.ResolveGodotLocale(settings.Language);
        var frameworkLanguage = _localizationMap.ResolveFrameworkLanguage(settings.Language);

        _applyGodotLocale(locale);

        // 设置系统持久化的是用户可见语言值；这里需要同步框架语言码，避免 Godot 与框架状态分裂。
        _localizationManagerResolver()?.SetLanguage(frameworkLanguage);
        return Task.CompletedTask;
    }

    /// <summary>
    ///     重置本地化设置
    /// </summary>
    public void Reset()
    {
        _model.GetData<LocalizationSettings>().Reset();
    }

    /// <summary>
    ///     获取本地化设置的数据对象。
    ///     该属性提供对本地化设置数据的只读访问。
    /// </summary>
    public ISettingsData Data => _model.GetData<LocalizationSettings>();

    /// <summary>
    ///     获取本地化设置数据的类型。
    ///     该属性返回本地化设置数据的具体类型信息。
    /// </summary>
    public Type DataType { get; } = typeof(LocalizationSettings);

    private static ILocalizationManager? TryResolveLocalizationManager()
    {
        try
        {
            return GameContext.GetFirstArchitectureContext().GetSystem<ILocalizationManager>();
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }
}