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

using GFramework.Core.Abstractions.logging;
using GFramework.Core.logging;
using GFramework.Game.Abstractions.enums;
using GFramework.Game.Abstractions.scene;

namespace GFramework.Game.scene.handler;

/// <summary>
/// 日志场景切换处理器，用于记录场景切换的详细信息。
/// </summary>
public sealed class LoggingTransitionHandler : SceneTransitionHandlerBase
{
    private static readonly ILogger Log = LoggerFactoryResolver.Provider.CreateLogger(nameof(LoggingTransitionHandler));

    /// <summary>
    /// 获取处理器优先级，数值越大优先级越低（最后执行）。
    /// </summary>
    public override int Priority => 999;

    /// <summary>
    /// 获取处理器处理的场景切换阶段，处理所有阶段。
    /// </summary>
    public override SceneTransitionPhases Phases => SceneTransitionPhases.All;

    /// <summary>
    /// 处理场景切换事件的异步方法。
    /// </summary>
    /// <param name="event">场景切换事件对象，包含切换的相关信息。</param>
    /// <param name="cancellationToken">取消令牌，用于控制异步操作的取消。</param>
    /// <returns>表示异步操作的任务。</returns>
    public override Task HandleAsync(SceneTransitionEvent @event, CancellationToken cancellationToken)
    {
        Log.Info(
            "Scene Transition: Phases={0}, Type={1}, From={2}, To={3}",
            @event.Get<string>("Phases", "Unknown"),
            @event.TransitionType,
            @event.FromSceneKey ?? "None",
            @event.ToSceneKey ?? "None"
        );

        return Task.CompletedTask;
    }
}