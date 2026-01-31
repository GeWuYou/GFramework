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
/// 定义设置数据仓库接口，用于管理应用程序设置数据的存储和检索
/// </summary>
/// <remarks>
/// 该接口继承自IDataRepository，专门用于处理配置设置相关的数据操作
/// </remarks>
public interface ISettingsDataRepository : IDataRepository
{
    /// <summary>
    /// 异步加载所有设置项
    /// </summary>
    /// <returns>
    /// 返回一个包含所有设置键值对的字典，其中键为设置名称，值为对应的设置数据对象
    /// </returns>
    /// <remarks>
    /// 此方法将从数据源中异步读取所有可用的设置项，并将其组织成字典格式返回
    /// </remarks>
    Task<IDictionary<string, IData>> LoadAllAsync();

    /// <summary>
    ///     注册数据类型到类型注册表中
    /// </summary>
    /// <param name="location">数据位置信息，用于获取键值</param>
    /// <param name="type">数据类型</param>
    void RegisterDataType(IDataLocation location, Type type);
}