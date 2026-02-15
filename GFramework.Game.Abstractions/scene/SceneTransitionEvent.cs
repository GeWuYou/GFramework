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

using GFramework.Game.Abstractions.enums;

namespace GFramework.Game.Abstractions.scene;

/// <summary>
/// 场景过渡事件，封装了场景切换过程中的上下文信息。
/// 提供了场景切换的元数据和自定义上下文数据的存储能力。
/// </summary>
public sealed class SceneTransitionEvent
{
    private readonly Dictionary<string, object> _context = new();

    /// <summary>
    /// 获取或初始化源场景的唯一标识符。
    /// 表示切换前的场景，如果是首次加载场景则为 null。
    /// </summary>
    public string? FromSceneKey { get; init; }

    /// <summary>
    /// 获取或初始化目标场景的唯一标识符。
    /// 表示切换后的场景，如果是清空操作则为 null。
    /// </summary>
    public string? ToSceneKey { get; init; }

    /// <summary>
    /// 获取或初始化场景过渡类型。
    /// 指示当前执行的是哪种场景切换操作（Push/Pop/Replace/Clear）。
    /// </summary>
    public SceneTransitionType TransitionType { get; init; }

    /// <summary>
    /// 获取或初始化场景进入参数。
    /// 包含传递给新场景的初始化数据或上下文信息。
    /// </summary>
    public ISceneEnterParam? EnterParam { get; init; }

    /// <summary>
    /// 从上下文中获取指定键的值。
    /// </summary>
    /// <typeparam name="T">值的类型。</typeparam>
    /// <param name="key">上下文键。</param>
    /// <param name="defaultValue">如果键不存在时返回的默认值。</param>
    /// <returns>上下文中存储的值，如果键不存在则返回默认值。</returns>
    public T Get<T>(string key, T defaultValue = default!)
    {
        if (_context.TryGetValue(key, out var value) && value is T typedValue)
            return typedValue;
        return defaultValue;
    }

    /// <summary>
    /// 设置上下文中指定键的值。
    /// </summary>
    /// <typeparam name="T">值的类型。</typeparam>
    /// <param name="key">上下文键。</param>
    /// <param name="value">要存储的值。</param>
    public void Set<T>(string key, T value)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));
        _context[key] = value!;
    }

    /// <summary>
    /// 尝试从上下文中获取指定键的值。
    /// </summary>
    /// <typeparam name="T">值的类型。</typeparam>
    /// <param name="key">上下文键。</param>
    /// <param name="value">输出参数，如果键存在则包含对应的值。</param>
    /// <returns>如果键存在且类型匹配则返回 true，否则返回 false。</returns>
    public bool TryGet<T>(string key, out T value)
    {
        if (_context.TryGetValue(key, out var obj) && obj is T typedValue)
        {
            value = typedValue;
            return true;
        }

        value = default!;
        return false;
    }

    /// <summary>
    /// 检查上下文中是否存在指定的键。
    /// </summary>
    /// <param name="key">上下文键。</param>
    /// <returns>如果键存在则返回 true，否则返回 false。</returns>
    public bool Has(string key)
    {
        return _context.ContainsKey(key);
    }

    /// <summary>
    /// 从上下文中移除指定的键。
    /// </summary>
    /// <param name="key">上下文键。</param>
    /// <returns>如果键存在并成功移除则返回 true，否则返回 false。</returns>
    public bool Remove(string key)
    {
        return _context.Remove(key);
    }
}