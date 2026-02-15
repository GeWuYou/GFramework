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
/// 场景加载进度接口，用于跟踪和报告场景资源的加载状态。
/// 实现此接口的类可以提供场景加载的实时进度信息。
/// </summary>
public interface ISceneLoadingProgress
{
    /// <summary>
    /// 获取正在加载的场景的唯一标识符。
    /// </summary>
    string SceneKey { get; }

    /// <summary>
    /// 获取当前加载进度，范围为 0.0 到 1.0。
    /// 0.0 表示刚开始加载，1.0 表示加载完成。
    /// </summary>
    float Progress { get; }

    /// <summary>
    /// 获取当前加载阶段的描述信息。
    /// 例如："加载纹理资源"、"初始化场景对象"等。
    /// 如果没有具体信息则返回 null。
    /// </summary>
    string? Message { get; }

    /// <summary>
    /// 获取加载是否已完成的状态。
    /// true 表示场景资源已全部加载完成，false 表示仍在加载中。
    /// </summary>
    bool IsCompleted { get; }
}