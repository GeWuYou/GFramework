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

namespace GFramework.Core.Abstractions.state;

/// <summary>
///     异步状态机状态接口，定义了状态的异步行为和转换规则
/// </summary>
public interface IAsyncState
{
    /// <summary>
    ///     当状态被激活进入时异步调用
    /// </summary>
    /// <param name="from">从哪个状态转换而来，可能为null表示初始状态</param>
    Task OnEnterAsync(IState? from);

    /// <summary>
    ///     当状态退出时异步调用
    /// </summary>
    /// <param name="to">将要转换到的目标状态，可能为null表示结束状态</param>
    Task OnExitAsync(IState? to);

    /// <summary>
    ///     异步判断当前状态是否可以转换到目标状态
    /// </summary>
    /// <param name="target">目标状态</param>
    /// <returns>如果可以转换则返回true，否则返回false</returns>
    Task<bool> CanTransitionToAsync(IState target);
}