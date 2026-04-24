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

using System;
using System.Collections.Generic;

namespace GFramework.Godot.Setting.Data;

/// <summary>
///     本地化映射设置
/// </summary>
public class LocalizationMap
{
    private const string DefaultFrameworkLanguage = "eng";
    private const string DefaultGodotLocale = "en";
    private readonly Dictionary<string, string> _frameworkLanguageMap;
    private readonly Dictionary<string, string> _languageMap;

    /// <summary>
    ///     使用默认的 Godot locale 与框架语言码映射初始化本地化设置。
    /// </summary>
    public LocalizationMap()
        : this(CreateDefaultLanguageMap(), CreateDefaultFrameworkLanguageMap())
    {
    }

    /// <summary>
    ///     使用外部提供的映射初始化本地化设置。
    ///     构造函数会复制输入字典，避免调用方在实例创建后继续修改内部状态。
    /// </summary>
    /// <param name="languageMap">用户语言到 Godot locale 的映射。</param>
    /// <param name="frameworkLanguageMap">用户语言到 GFramework 本地化语言码的映射。</param>
    /// <exception cref="ArgumentNullException">
    ///     当 <paramref name="languageMap" /> 或 <paramref name="frameworkLanguageMap" /> 为 <see langword="null" /> 时抛出。
    /// </exception>
    public LocalizationMap(
        IReadOnlyDictionary<string, string> languageMap,
        IReadOnlyDictionary<string, string> frameworkLanguageMap)
    {
        ArgumentNullException.ThrowIfNull(languageMap);
        ArgumentNullException.ThrowIfNull(frameworkLanguageMap);

        // 复制外部输入，避免公共属性把可变集合直接暴露给调用方。
        _languageMap = new Dictionary<string, string>(languageMap, StringComparer.Ordinal);
        _frameworkLanguageMap = new Dictionary<string, string>(frameworkLanguageMap, StringComparer.Ordinal);
    }

    /// <summary>
    ///     获取用户语言到 Godot locale 的只读映射表。
    /// </summary>
    public IReadOnlyDictionary<string, string> LanguageMap => _languageMap;

    /// <summary>
    ///     获取用户语言到 GFramework 本地化语言码的只读映射表。
    /// </summary>
    public IReadOnlyDictionary<string, string> FrameworkLanguageMap => _frameworkLanguageMap;

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

    private static Dictionary<string, string> CreateDefaultLanguageMap()
    {
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "简体中文", "zh_CN" },
            { "English", "en" }
        };
    }

    private static Dictionary<string, string> CreateDefaultFrameworkLanguageMap()
    {
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "简体中文", "zhs" },
            { "English", "eng" }
        };
    }
}
