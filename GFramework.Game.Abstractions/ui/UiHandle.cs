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

namespace GFramework.Game.Abstractions.ui;

/// <summary>
/// 表示一个UI句柄，用于唯一标识和管理UI实例。
/// </summary>
public readonly struct UiHandle
{
    /// <summary>
    /// 获取UI实例的唯一标识符。
    /// </summary>
    public string InstanceId { get; }

    /// <summary>
    /// 获取UI的键值，通常用于标识UI的类型或名称。
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// 获取UI所在的层级，用于控制UI的显示顺序。
    /// </summary>
    public UiLayer Layer { get; }

    /// <summary>
    /// 初始化一个新的UiHandle实例。
    /// </summary>
    /// <param name="key">UI的键值，用于标识UI的类型或名称。</param>
    /// <param name="instanceId">UI实例的唯一标识符。</param>
    /// <param name="layer">UI所在的层级，用于控制UI的显示顺序。</param>
    internal UiHandle(string key, string instanceId, UiLayer layer)
    {
        Key = key;
        InstanceId = instanceId;
        Layer = layer;
    }
}