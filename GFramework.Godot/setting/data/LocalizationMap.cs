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

namespace GFramework.Godot.setting.data;

/// <summary>
///     本地化映射设置
/// </summary>
public class LocalizationMap
{
    /// <summary>
    ///     用户语言 -> Godot locale 映射表
    /// </summary>
    public Dictionary<string, string> LanguageMap { get; set; } = new()
    {
        { "简体中文", "zh_CN" },
        { "English", "en" }
    };
}