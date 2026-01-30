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

using GFramework.Core.Abstractions.utility;

namespace GFramework.Game.Abstractions.data;

/// <summary>
/// 定义数据位置提供者的接口，用于获取指定类型的数据位置信息
/// </summary>
public interface IDataLocationProvider:IUtility
{
    /// <summary>
    /// 获取指定类型的数据位置
    /// </summary>
    /// <param name="type">需要获取位置信息的类型</param>
    /// <returns>与指定类型关联的数据位置对象</returns>
    IDataLocation GetLocation(Type type);
}
