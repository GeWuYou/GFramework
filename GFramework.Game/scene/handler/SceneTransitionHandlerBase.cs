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
using GFramework.Game.Abstractions.scene;

namespace GFramework.Game.scene.handler;

/// <summary>
/// 场景过渡处理器抽象基类，提供了 ISceneTransitionHandler 接口的默认实现。
/// 派生类只需重写必要的方法即可快速实现自定义处理器。
/// </summary>
public abstract class SceneTransitionHandlerBase : ISceneTransitionHandler
{
    /// <summary>
    /// 获取处理器适用的场景过渡阶段。
    /// 默认为所有阶段（BeforeChange 和 AfterChange）。
    /// </summary>
    public virtual SceneTransitionPhases Phases => SceneTransitionPhases.All;

    /// <summary>
    /// 获取处理器的执行优先级。
    /// 派生类必须实现此属性以指定优先级。
    /// </summary>
    public abstract int Priority { get; }

    /// <summary>
    /// 判断处理器是否应该处理当前场景过渡事件。
    /// 默认实现总是返回 true，派生类可以重写此方法以添加自定义过滤逻辑。
    /// </summary>
    /// <param name="event">场景过渡事件。</param>
    /// <param name="phases">当前执行的阶段。</param>
    /// <returns>默认返回 true，表示总是处理。</returns>
    public virtual bool ShouldHandle(SceneTransitionEvent @event, SceneTransitionPhases phases)
        => true;

    /// <summary>
    /// 异步处理场景过渡事件。
    /// 派生类必须实现此方法以定义具体的处理逻辑。
    /// </summary>
    /// <param name="event">场景过渡事件，包含切换的上下文信息。</param>
    /// <param name="cancellationToken">取消令牌，用于支持操作取消。</param>
    /// <returns>表示处理操作完成的异步任务。</returns>
    public abstract Task HandleAsync(SceneTransitionEvent @event, CancellationToken cancellationToken);
}