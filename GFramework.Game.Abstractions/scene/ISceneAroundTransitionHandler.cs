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

namespace GFramework.Game.Abstractions.scene;

/// <summary>
/// 场景切换中间件处理器接口，支持包裹整个变更过程的逻辑。
/// Around 处理器在变更前后都会执行，可以控制是否继续执行变更。
/// 适用于：性能监控、事务管理、权限验证、日志记录等横切关注点。
/// </summary>
public interface ISceneAroundTransitionHandler
{
    /// <summary>
    /// 获取处理器的执行优先级。
    /// 数值越小优先级越高，越先执行（外层）。
    /// 建议范围：-1000 到 1000。
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// 判断处理器是否应该处理当前场景过渡事件。
    /// </summary>
    /// <param name="event">场景过渡事件。</param>
    /// <returns>如果应该处理则返回 true，否则返回 false。</returns>
    bool ShouldHandle(SceneTransitionEvent @event);

    /// <summary>
    /// 执行中间件逻辑。
    /// </summary>
    /// <param name="event">场景过渡事件，包含切换的上下文信息。</param>
    /// <param name="next">下一个中间件或实际操作的委托。调用此委托以继续执行流程。</param>
    /// <param name="cancellationToken">取消令牌，用于支持操作取消。</param>
    /// <returns>表示处理操作完成的异步任务。</returns>
    Task HandleAsync(
        SceneTransitionEvent @event,
        Func<Task> next,
        CancellationToken cancellationToken);
}