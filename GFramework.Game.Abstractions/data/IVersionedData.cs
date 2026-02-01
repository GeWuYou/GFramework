// Copyright (c) 2025 GeWuYou
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
///     版本化数据接口，继承自IData接口
///     提供版本控制和修改时间跟踪功能
/// </summary>
public interface IVersionedData : IData
{
    /// <summary>
    ///     获取数据的版本号
    /// </summary>
    /// <returns>当前数据的版本号，用于标识数据的版本状态</returns>
    int Version { get; }

    /// <summary>
    ///     获取数据最后修改的时间
    /// </summary>
    /// <returns>DateTime类型的最后修改时间戳</returns>
    DateTime LastModified { get; }
}