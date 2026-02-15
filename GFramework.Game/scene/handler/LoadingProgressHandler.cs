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
/// 加载进度处理器，用于在场景加载前显示加载界面和进度反馈。
/// </summary>
public sealed class LoadingProgressHandler : SceneTransitionHandlerBase
{
    private static readonly ILogger Log = LoggerFactoryResolver.Provider.CreateLogger(nameof(LoadingProgressHandler));

    /// <summary>
    /// 获取处理器优先级，数值越小优先级越高（优先执行）。
    /// </summary>
    public override int Priority => -100;

    /// <summary>
    /// 获取处理器处理的场景切换阶段，只处理 BeforeChange 阶段。
    /// </summary>
    public override SceneTransitionPhases Phases => SceneTransitionPhases.BeforeChange;

    /// <summary>
    /// 处理场景切换事件的异步方法。
    /// </summary>
    /// <param name="event">场景切换事件对象，包含切换的相关信息。</param>
    /// <param name="cancellationToken">取消令牌，用于控制异步操作的取消。</param>
    /// <returns>表示异步操作的任务。</returns>
    public override async Task HandleAsync(SceneTransitionEvent @event, CancellationToken cancellationToken)
    {
        // 只处理 Push 和 Replace 操作（需要加载场景）
        if (@event.TransitionType != SceneTransitionType.Push &&
            @event.TransitionType != SceneTransitionType.Replace)
            return;

        // 显示加载界面
        // TODO: 调用 UI 系统显示加载界面
        Log.Info("Loading scene: {0}", @event.ToSceneKey);

        // 监听加载进度（如果需要）
        // 这里可以通过事件上下文传递进度信息
        // 例如：@event.Set("LoadingProgress", progressValue);

        await Task.CompletedTask;
    }
}