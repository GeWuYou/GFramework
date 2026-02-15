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
/// 场景过渡处理器选项，定义了处理器执行时的配置参数。
/// </summary>
/// <param name="TimeoutMs">
/// 处理器执行的超时时间（毫秒）。
/// 设置为 0 表示无超时限制。
/// 如果处理器执行超过此时间，将触发超时异常。
/// </param>
/// <param name="ContinueOnError">
/// 当处理器执行失败时是否继续执行后续处理器。
/// true 表示即使当前处理器失败，管道仍会继续执行后续处理器。
/// false 表示当前处理器失败时，管道会立即停止并抛出异常。
/// </param>
public record SceneTransitionHandlerOptions(
    int TimeoutMs = 0,
    bool ContinueOnError = true
);