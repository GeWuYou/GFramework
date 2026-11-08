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

namespace GFramework.Game.Abstractions.Data;

/// <summary>
///     数据仓库配置选项。
/// </summary>
/// <remarks>
///     该选项描述的是仓库层的公开行为契约，而不是某一种固定的落盘格式。
///     因此不同实现可以分别使用“每项单文件”或“统一聚合文件”存储，只要对外遵守同一套备份与事件语义：
///     <list type="bullet">
///         <item>
///             <description>
///                 <see cref="AutoBackup" /> 在覆盖已有持久化数据前保留上一份可恢复快照。对于聚合型仓库，备份粒度是整个统一文件。
///             </description>
///         </item>
///         <item>
///             <description>
///                 <see cref="EnableEvents" /> 仅控制公开仓库操作产生的事件；内部缓存预热、迁移回写或批量保存中的子步骤不会额外发出单项事件。
///             </description>
///         </item>
///     </list>
/// </remarks>
public class DataRepositoryOptions
{
    /// <summary>
    ///     获取或设置仓库使用的基础存储路径。
    /// </summary>
    /// <remarks>
    ///     具体实现会在该路径下组织自己的键空间。调用方应将其视为仓库级根目录，而不是具体文件名。
    /// </remarks>
    public string BasePath { get; set; } = "";

    /// <summary>
    ///     获取或设置是否在覆盖已有持久化数据前自动创建备份。
    /// </summary>
    /// <remarks>
    ///     该选项只影响覆盖写入；首次写入不会生成备份。聚合型仓库会为统一文件创建单份备份，而不是为内部 section 分别备份。
    /// </remarks>
    public bool AutoBackup { get; set; } = false;

    /// <summary>
    ///     获取或设置是否启用仓库层加载、保存、删除与批量保存事件。
    /// </summary>
    /// <remarks>
    ///     当该值为 <see langword="true" /> 时，<c>SaveAllAsync</c> 只会发出批量事件，不会重复发出每个条目的单项保存事件。
    /// </remarks>
    public bool EnableEvents { get; set; } = true;
}
