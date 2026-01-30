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

namespace GFramework.Game.Abstractions.setting.data;

/// <summary>
///     本地化设置类，用于管理游戏的语言本地化配置
///     实现了ISettingsData接口提供设置数据功能，实现IVersioned接口提供版本控制功能
/// </summary>
public class LocalizationSettings : ISettingsData
{
    /// <summary>
    ///     获取或设置当前使用的语言
    /// </summary>
    /// <value>默认值为"简体中文"</value>
    public string Language { get; set; } = "简体中文";

    /// <summary>
    ///     重置本地化设置到默认状态
    ///     将Language属性恢复为默认的"简体中文"值
    /// </summary>
    public void Reset()
    {
        Language = "简体中文";
    }

    /// <summary>
    ///     获取或设置设置数据的版本号
    /// </summary>
    public int Version { get; private set; } = 1;

    /// <summary>
    ///     获取设置数据最后修改的时间
    /// </summary>
    public DateTime LastModified { get; } = DateTime.Now;

    /// <summary>
    ///     从指定的数据源加载本地化设置
    /// </summary>
    /// <param name="source">要从中加载设置的源对象</param>
    /// <remarks>
    ///     该方法仅处理类型为LocalizationSettings的对象，
    ///     如果源对象不是LocalizationSettings类型，则直接返回不执行任何操作
    /// </remarks>
    public void LoadFrom(ISettingsData source)
    {
        if (source is not LocalizationSettings settings)
        {
            return;
        }

        Language = settings.Language;
        Version = settings.Version;
    }
}