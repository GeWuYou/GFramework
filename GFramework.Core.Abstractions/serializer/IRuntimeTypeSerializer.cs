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

namespace GFramework.Core.Abstractions.serializer;

/// <summary>
/// 运行时类型序列化器接口，继承自ISerializer接口
/// 提供基于运行时类型的对象序列化和反序列化功能
/// </summary>
public interface IRuntimeTypeSerializer : ISerializer
{
    /// <summary>
    /// 将指定对象序列化为字符串
    /// </summary>
    /// <param name="obj">要序列化的对象</param>
    /// <param name="type">对象的运行时类型</param>
    /// <returns>序列化后的字符串表示</returns>
    string Serialize(object obj, Type type);

    /// <summary>
    /// 将字符串数据反序列化为指定类型的对象
    /// </summary>
    /// <param name="data">要反序列化的字符串数据</param>
    /// <param name="type">目标对象的运行时类型</param>
    /// <returns>反序列化后的对象实例</returns>
    object Deserialize(string data, Type type);
}