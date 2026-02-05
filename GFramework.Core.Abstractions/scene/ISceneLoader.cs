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

namespace GFramework.Core.Abstractions.scene;

/// <summary>
/// 定义场景加载器的接口，用于管理场景的加载、替换和卸载操作。
/// </summary>
public interface ISceneLoader<out T>
{
    /// <summary>
    /// 获取当前加载的场景对象。
    /// </summary>
    /// <returns>当前场景对象，如果未加载任何场景则返回 null。</returns>
    T? Current { get; }

    /// <summary>
    /// 替换当前场景为指定键对应的场景。
    /// </summary>
    /// <param name="key">场景的唯一标识符或键值。</param>
    void Replace(string key);

    /// <summary>
    /// 卸载当前加载的场景。
    /// </summary>
    void Unload();
}