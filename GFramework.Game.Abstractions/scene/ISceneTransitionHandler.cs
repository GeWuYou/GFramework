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
/// 场景过渡处理器接口，定义了场景切换过程中的扩展点。
/// 实现此接口可以在场景切换的不同阶段执行自定义逻辑。
/// </summary>
public interface ISceneTransitionHandler
{
    /// <summary>
    /// 获取处理器的执行优先级。
    /// 数值越小优先级越高，越先执行。
    /// 建议范围：-1000 到 1000。
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// 获取处理器适用的场景过渡阶段。
    /// 可以使用 Flags 组合多个阶段（如 BeforeChange | AfterChange）。
    /// </summary>
    SceneTransitionPhases Phases { get; }

    /// <summary>
    /// 判断处理器是否应该处理当前场景过渡事件。
    /// </summary>
    /// <param name="event">场景过渡事件。</param>
    /// <param name="phases">当前执行的阶段。</param>
    /// <returns>如果应该处理则返回 true，否则返回 false。</returns>
    bool ShouldHandle(SceneTransitionEvent @event, SceneTransitionPhases phases);

    /// <summary>
    /// 异步处理场景过渡事件。
    /// </summary>
    /// <param name="event">场景过渡事件，包含切换的上下文信息。</param>
    /// <param name="cancellationToken">取消令牌，用于支持操作取消。</param>
    /// <returns>表示处理操作完成的异步任务。</returns>
    Task HandleAsync(SceneTransitionEvent @event, CancellationToken cancellationToken);
}