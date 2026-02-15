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
/// 场景过渡阶段枚举，定义了场景切换过程中的不同执行阶段。
/// 使用 Flags 特性支持组合多个阶段。
/// </summary>
[Flags]
public enum SceneTransitionPhases
{
    /// <summary>
    /// 场景切换前阶段（阻塞执行）。
    /// 用于执行需要在场景切换前完成的操作，如显示加载动画、弹出确认对话框等。
    /// 此阶段的处理器会阻塞场景切换流程，直到所有处理器执行完成。
    /// </summary>
    BeforeChange = 1,

    /// <summary>
    /// 场景切换后阶段（非阻塞执行）。
    /// 用于执行场景切换后的后续操作，如播放音效、记录日志、发送统计数据等。
    /// 此阶段的处理器异步执行，不会阻塞场景切换流程。
    /// </summary>
    AfterChange = 2,

    /// <summary>
    /// 所有阶段的组合标志。
    /// 表示处理器适用于场景切换的所有阶段。
    /// </summary>
    All = BeforeChange | AfterChange
}