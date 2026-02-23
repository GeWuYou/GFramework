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
/// 定义数据仓储接口，提供键值对数据的基本操作功能
/// </summary>
/// <typeparam name="TKey">键的类型</typeparam>
/// <typeparam name="TValue">值的类型</typeparam>
public interface IRepository<in TKey, TValue>
{
    /// <summary>
    /// 添加键值对到仓储中
    /// </summary>
    /// <param name="key">要添加的键</param>
    /// <param name="value">要添加的值</param>
    void Add(TKey key, TValue value);

    /// <summary>
    /// 根据键获取对应的值
    /// </summary>
    /// <param name="key">要查找的键</param>
    /// <returns>与指定键关联的值</returns>
    TValue Get(TKey key);

    /// <summary>
    /// 尝试根据键获取对应的值
    /// </summary>
    /// <param name="key">要查找的键</param>
    /// <param name="value">输出参数，如果找到则返回对应的值，否则返回默认值</param>
    /// <returns>如果找到键则返回true，否则返回false</returns>
    bool TryGet(TKey key, out TValue value);

    /// <summary>
    /// 获取仓储中的所有值
    /// </summary>
    /// <returns>包含所有值的只读集合</returns>
    IReadOnlyCollection<TValue> GetAll();

    /// <summary>
    /// 检查仓储中是否包含指定的键
    /// </summary>
    /// <param name="key">要检查的键</param>
    /// <returns>如果包含该键则返回true，否则返回false</returns>
    bool Contains(TKey key);

    /// <summary>
    /// 从仓储中移除指定键的项
    /// </summary>
    /// <param name="key">要移除的键</param>
    void Remove(TKey key);

    /// <summary>
    /// 清空仓储中的所有数据
    /// </summary>
    void Clear();
}