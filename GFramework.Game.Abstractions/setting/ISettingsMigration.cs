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

namespace GFramework.Game.Abstractions.setting;

/// <summary>
/// 定义设置数据迁移接口，用于处理不同版本设置数据之间的转换
/// </summary>
public interface ISettingsMigration
{
    /// <summary>
    /// 获取要迁移的设置类型
    /// </summary>
    Type SettingsType { get; }

    /// <summary>
    /// 获取源版本号（迁移前的版本）
    /// </summary>
    int FromVersion { get; }

    /// <summary>
    /// 获取目标版本号（迁移后的版本）
    /// </summary>
    int ToVersion { get; }

    /// <summary>
    /// 执行设置数据迁移操作
    /// </summary>
    /// <param name="oldData">需要迁移的旧版设置数据</param>
    /// <returns>迁移后的新版设置数据</returns>
    ISettingsSection Migrate(ISettingsSection oldData);
}