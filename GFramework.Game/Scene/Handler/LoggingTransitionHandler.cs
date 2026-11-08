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

using GFramework.Core.Abstractions.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using GFramework.Core.Logging;
using GFramework.Game.Abstractions.Enums;
using GFramework.Game.Abstractions.Scene;

namespace GFramework.Game.Scene.Handler;

/// <summary>
/// 日志场景切换处理器，用于记录场景切换的详细信息。
/// </summary>
/// <remarks>创建日志切换处理器；未注入日志时使用空输出。</remarks>
/// <param name="logger">标准日志记录器。</param>
public sealed class LoggingTransitionHandler(ILogger? logger = null) : SceneTransitionHandlerBase
{

    // 标准日志记录器；未注入时使用空输出，不读取进程级状态。
    private readonly ILogger _logger = logger ?? NullLogger<LoggingTransitionHandler>.Instance;

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
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Scene Transition: Phases={Phases}, Type={TransitionType}, From={FromScene}, To={ToScene}",
                @event.Get<string>("Phases", "Unknown"), @event.TransitionType,
                @event.FromSceneKey ?? "None", @event.ToSceneKey ?? "None");
        }

        return Task.CompletedTask;
    }
}
