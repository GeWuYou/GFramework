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

namespace GFramework.Game.Abstractions.enums;

/// <summary>
/// 场景过渡类型枚举，定义了场景切换的不同操作模式。
/// </summary>
public enum SceneTransitionType
{
    /// <summary>
    /// 压入新场景到场景栈顶部。
    /// 当前场景会被暂停，新场景成为活动场景。
    /// </summary>
    Push,

    /// <summary>
    /// 弹出当前场景并恢复下一个场景。
    /// 当前场景会被卸载，栈中的下一个场景变为活动场景。
    /// </summary>
    Pop,

    /// <summary>
    /// 替换所有场景，清空整个场景栈并加载新场景。
    /// 此操作会卸载所有现有场景，然后加载指定的新场景。
    /// </summary>
    Replace,

    /// <summary>
    /// 清空所有已加载的场景。
    /// 卸载场景栈中的所有场景，使系统回到无场景状态。
    /// </summary>
    Clear
}