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

namespace GFramework.Game.Abstractions.data;

/// <summary>
///     数据仓库配置选项
/// </summary>
public class DataRepositoryOptions
{
    /// <summary>
    ///     存储基础路径（如 "user://data/"）
    /// </summary>
    public string BasePath { get; set; } = "";

    /// <summary>
    ///     键名前缀（如 "Game"，生成的键为 "Game_SettingsData"）
    /// </summary>
    public string KeyPrefix { get; set; } = "";

    /// <summary>
    ///     是否在保存时自动备份
    /// </summary>
    public bool AutoBackup { get; set; } = false;

    /// <summary>
    ///     是否启用加载/保存事件
    /// </summary>
    public bool EnableEvents { get; set; } = true;
}