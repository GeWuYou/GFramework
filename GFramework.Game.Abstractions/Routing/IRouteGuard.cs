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

namespace GFramework.Game.Abstractions.Routing;

/// <summary>
/// 路由守卫接口,用于控制路由的进入和离开
/// </summary>
/// <typeparam name="TRoute">路由项类型</typeparam>
public interface IRouteGuard<TRoute> where TRoute : IRoute
{
    /// <summary>
    /// 守卫优先级,数值越小优先级越高
    /// </summary>
    /// <remarks>
    /// 守卫按优先级从小到大依次执行。
    /// 建议使用 0-100 的范围,默认为 50。
    /// </remarks>
    int Priority { get; }

    /// <summary>
    /// 是否可以中断后续守卫的执行
    /// </summary>
    /// <remarks>
    /// 如果为 true,当此守卫返回 true 或抛出异常时,将中断后续守卫的执行。
    /// 如果为 false,将继续执行后续守卫。
    /// </remarks>
    bool CanInterrupt { get; }

    /// <summary>
    /// 检查是否可以进入指定路由
    /// </summary>
    /// <param name="routeKey">路由键值</param>
    /// <param name="context">路由上下文</param>
    /// <returns>如果允许进入返回 true,否则返回 false</returns>
    ValueTask<bool> CanEnterAsync(string routeKey, IRouteContext? context);

    /// <summary>
    /// 检查是否可以离开指定路由
    /// </summary>
    /// <param name="routeKey">路由键值</param>
    /// <returns>如果允许离开返回 true,否则返回 false</returns>
    ValueTask<bool> CanLeaveAsync(string routeKey);
}