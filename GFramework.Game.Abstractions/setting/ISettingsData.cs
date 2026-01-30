// Copyright (c) 2025 GeWuYou
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

using GFramework.Core.Abstractions.data;
using GFramework.Game.Abstractions.data;

namespace GFramework.Game.Abstractions.setting;

/// <summary>
/// 定义游戏设置数据的接口
/// 该接口继承自IData和IResettable接口，提供数据管理和重置功能
/// </summary>
public interface ISettingsData : IResettable, IVersionedData, ILoadableFrom<ISettingsData>;