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

using GFramework.Core.Abstractions.bases;
using GFramework.Core.Abstractions.logging;
using GFramework.Core.logging;
using Godot;

namespace GFramework.Godot.data;

/// <summary>
/// Godot资源仓储实现类，用于管理Godot资源的存储和加载。
/// 实现了IResourceRepository接口，提供基于键的资源存取功能。
/// </summary>
/// <typeparam name="TKey">资源键的类型</typeparam>
/// <typeparam name="TResource">资源类型，必须继承自Godot.Resource并实现IHasKey接口</typeparam>
public class GodotResourceRepository<TKey, TResource>
    : IResourceRepository<TKey, TResource>
    where TResource : Resource, IHasKey<TKey>
    where TKey : notnull
{
    private static readonly ILogger Log =
        LoggerFactoryResolver.Provider.CreateLogger(nameof(GodotResourceRepository<TKey, TResource>));

    /// <summary>
    /// 内部存储字典，用于保存键值对形式的资源
    /// </summary>
    private readonly Dictionary<TKey, TResource> _storage = new();

    /// <summary>
    /// 向仓储中添加资源
    /// </summary>
    /// <param name="key">资源的键</param>
    /// <param name="value">要添加的资源对象</param>
    /// <exception cref="InvalidOperationException">当键已存在时抛出异常</exception>
    public void Add(TKey key, TResource value)
    {
        if (!_storage.TryAdd(key, value))
            throw new InvalidOperationException($"Duplicate key detected: {key}");
    }

    /// <summary>
    /// 根据键获取资源
    /// </summary>
    /// <param name="key">资源的键</param>
    /// <returns>对应的资源对象</returns>
    /// <exception cref="KeyNotFoundException">当键不存在时抛出异常</exception>
    public TResource Get(TKey key)
    {
        if (!_storage.TryGetValue(key, out var value))
            throw new KeyNotFoundException($"Resource with key '{key}' not found.");

        return value;
    }

    /// <summary>
    /// 尝试根据键获取资源
    /// </summary>
    /// <param name="key">资源的键</param>
    /// <param name="value">输出参数，返回找到的资源对象</param>
    /// <returns>如果找到资源返回true，否则返回false</returns>
    public bool TryGet(TKey key, out TResource value)
        => _storage.TryGetValue(key, out value!);

    /// <summary>
    /// 获取所有资源的只读集合
    /// </summary>
    /// <returns>包含所有资源的只读集合</returns>
    public IReadOnlyCollection<TResource> GetAll()
        => _storage.Values.ToArray();

    /// <summary>
    /// 检查是否包含指定键的资源
    /// </summary>
    /// <param name="key">要检查的键</param>
    /// <returns>如果包含该键返回true，否则返回false</returns>
    public bool Contains(TKey key)
        => _storage.ContainsKey(key);

    /// <summary>
    /// 从仓储中移除指定键的资源
    /// </summary>
    /// <param name="key">要移除的资源键</param>
    /// <exception cref="KeyNotFoundException">当键不存在时抛出异常</exception>
    public void Remove(TKey key)
    {
        if (!_storage.Remove(key))
            throw new KeyNotFoundException($"Resource with key '{key}' not found.");
    }

    /// <summary>
    /// 清空仓储中的所有资源
    /// </summary>
    public void Clear()
        => _storage.Clear();

    /// <summary>
    /// 从指定路径集合加载资源（非递归）
    /// </summary>
    /// <param name="paths">资源文件路径集合</param>
    public void LoadFromPath(IEnumerable<string> paths)
    {
        LoadFromPathInternal(paths, recursive: false);
    }

    /// <summary>
    /// 从指定路径数组加载资源（非递归）
    /// </summary>
    /// <param name="paths">资源文件路径数组</param>
    public void LoadFromPath(params string[] paths)
    {
        LoadFromPathInternal(paths, recursive: false);
    }

    /// <summary>
    /// 递归从指定路径集合加载资源
    /// </summary>
    /// <param name="paths">资源文件路径集合</param>
    public void LoadFromPathRecursive(IEnumerable<string> paths)
    {
        LoadFromPathInternal(paths, recursive: true);
    }

    /// <summary>
    /// 递归从指定路径数组加载资源
    /// </summary>
    /// <param name="paths">资源文件路径数组</param>
    public void LoadFromPathRecursive(params string[] paths)
    {
        LoadFromPathInternal(paths, recursive: true);
    }

    /// <summary>
    /// 内部方法，根据路径集合和递归标志加载资源
    /// </summary>
    /// <param name="paths">资源文件路径集合</param>
    /// <param name="recursive">是否递归加载子目录中的资源</param>
    private void LoadFromPathInternal(IEnumerable<string> paths, bool recursive)
    {
        foreach (var path in paths)
        {
            LoadSinglePath(path, recursive);
        }
    }

    /// <summary>
    /// 从单个路径加载资源
    /// 遍历目录中的所有.tres和.res文件并加载为资源
    /// </summary>
    /// <param name="path">要加载资源的目录路径</param>
    /// <param name="recursive">是否递归加载子目录中的资源</param>
    private void LoadSinglePath(string path, bool recursive)
    {
        // 尝试打开指定路径的目录
        var dir = DirAccess.Open(path);
        if (dir == null)
        {
            Log.Warn($"Path not found: {path}");
            return;
        }

        // 开始遍历目录
        dir.ListDirBegin();
        // 循环读取目录中的每个条目
        while (true)
        {
            var entry = dir.GetNext();
            if (string.IsNullOrEmpty(entry))
                break;
            // 跳过当前目录和父目录的特殊条目
            if (entry is not ("." or ".."))
                ProcessEntry(path, entry, dir.CurrentIsDir(), recursive);
        }

        // 结束目录遍历
        dir.ListDirEnd();
    }

    /// <summary>
    /// 处理目录中的单个条目
    /// 如果是目录且启用递归则继续深入处理，如果是资源文件则加载到存储中
    /// </summary>
    /// <param name="basePath">基础路径</param>
    /// <param name="entry">当前处理的条目名称</param>
    /// <param name="isDir">当前条目是否为目录</param>
    /// <param name="recursive">是否递归处理子目录</param>
    private void ProcessEntry(string basePath, string entry, bool isDir, bool recursive)
    {
        // 构建完整的文件路径
        var fullPath = $"{basePath}/{entry}";

        // 处理目录条目
        if (isDir)
        {
            // 如果启用递归，则递归处理子目录
            if (recursive)
                LoadSinglePath(fullPath, true);
            return;
        }

        // 只处理.tres和.res扩展名的资源文件
        if (!entry.EndsWith(".tres") && !entry.EndsWith(".res"))
            return;

        // 加载资源文件
        var resource = GD.Load<TResource>(fullPath);
        if (resource == null)
        {
            Log.Warn($"Failed to load resource: {fullPath}");
            return;
        }

        // 将资源添加到存储中，如果键已存在则记录警告
        if (!_storage.TryAdd(resource.Key, resource))
            Log.Warn($"Duplicate key detected: {resource.Key}");
    }
}