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

using GFramework.Game.Abstractions.setting;
using GFramework.Game.Abstractions.setting.data;
using GFramework.Godot.setting.data;
using Godot;

namespace GFramework.Godot.setting;

/// <summary>
/// Godot本地化设置类，负责应用本地化配置到Godot引擎
/// </summary>
/// <param name="settings">本地化设置对象</param>
/// <param name="localizationMap">本地化映射表</param>
public class GodotLocalizationSettings(LocalizationSettings settings, LocalizationMap localizationMap)
    : IPersistentApplyAbleSettings
{
    /// <summary>
    /// 应用本地化设置到Godot引擎
    /// </summary>
    /// <returns>完成的任务</returns>
    public Task Apply()
    {
        // 尝试从映射表获取 Godot locale
        var locale = localizationMap.LanguageMap.GetValueOrDefault(settings.Language, "en");
        // 默认值
        TranslationServer.SetLocale(locale);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 重置本地化设置
    /// </summary>
    public void Reset() => settings.Reset();
}