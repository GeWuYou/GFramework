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

namespace GFramework.Godot.Setting.Data;

/// <summary>
///     本地化映射设置
/// </summary>
public class LocalizationMap
{
    private const string DefaultFrameworkLanguage = "eng";
    private const string DefaultGodotLocale = "en";

    /// <summary>
    ///     用户语言 -> Godot locale 映射表。
    /// </summary>
    public Dictionary<string, string> LanguageMap { get; set; } = new()
    {
        { "简体中文", "zh_CN" },
        { "English", "en" }
    };

    /// <summary>
    ///     用户语言 -> GFramework 本地化语言码映射表。
    /// </summary>
    public Dictionary<string, string> FrameworkLanguageMap { get; set; } = new()
    {
        { "简体中文", "zhs" },
        { "English", "eng" }
    };

    /// <summary>
    ///     解析用户保存的语言值对应的 Godot locale。
    /// </summary>
    /// <param name="storedLanguage">设置系统中保存的语言值。</param>
    /// <returns>对应的 Godot locale；未知值时回退为英文。</returns>
    public string ResolveGodotLocale(string? storedLanguage)
    {
        if (string.IsNullOrWhiteSpace(storedLanguage))
        {
            return DefaultGodotLocale;
        }

        return LanguageMap.GetValueOrDefault(storedLanguage, DefaultGodotLocale);
    }

    /// <summary>
    ///     解析用户保存的语言值对应的框架语言码。
    /// </summary>
    /// <param name="storedLanguage">设置系统中保存的语言值。</param>
    /// <returns>对应的框架语言码；未知值时回退为英文。</returns>
    public string ResolveFrameworkLanguage(string? storedLanguage)
    {
        if (string.IsNullOrWhiteSpace(storedLanguage))
        {
            return DefaultFrameworkLanguage;
        }

        return FrameworkLanguageMap.GetValueOrDefault(storedLanguage, DefaultFrameworkLanguage);
    }
}