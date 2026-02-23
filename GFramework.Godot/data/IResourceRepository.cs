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

using GFramework.Game.Abstractions.data;
using Godot;

namespace GFramework.Godot.data;

/// <summary>
/// 定义资源仓储接口，专门用于管理Godot资源的加载和存储
/// 继承自通用仓储接口，添加了从路径加载资源的功能
/// </summary>
/// <typeparam name="TKey">资源键的类型</typeparam>
/// <typeparam name="TResource">资源类型，必须继承自Godot.Resource</typeparam>
public interface IResourceRepository<in TKey, TResource> : IRepository<TKey, TResource> where TResource : Resource
{
    /// <summary>
    /// 从指定路径集合加载资源
    /// </summary>
    /// <param name="paths">资源文件路径集合</param>
    void LoadFromPath(IEnumerable<string> paths);

    /// <summary>
    /// 从指定路径数组加载资源
    /// </summary>
    /// <param name="paths">资源文件路径数组</param>
    void LoadFromPath(params string[] paths);

    /// <summary>
    /// 递归从指定路径集合加载资源
    /// </summary>
    /// <param name="paths">资源文件路径集合</param>
    void LoadFromPathRecursive(IEnumerable<string> paths);

    /// <summary>
    /// 递归从指定路径数组加载资源
    /// </summary>
    /// <param name="paths">资源文件路径数组</param>
    void LoadFromPathRecursive(params string[] paths);
}