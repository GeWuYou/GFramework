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

using GFramework.Core.Abstractions.utility;

namespace GFramework.Game.Abstractions.scene;

/// <summary>
/// 场景工厂接口，继承自上下文工具接口。
/// 负责根据场景键值创建对应的场景行为实例，是场景管理系统的核心工厂模式实现。
/// </summary>
public interface ISceneFactory : IContextUtility
{
    /// <summary>
    /// 根据指定的场景键值创建场景行为实例。
    /// </summary>
    /// <param name="sceneKey">场景的唯一标识符键值。</param>
    /// <returns>创建的场景行为对象，如果无法创建则返回null。</returns>
    ISceneBehavior Create(string sceneKey);
}