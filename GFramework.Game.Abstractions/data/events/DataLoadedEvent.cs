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

namespace GFramework.Game.Abstractions.data.events;

/// <summary>
///     表示数据加载完成事件的泛型类
/// </summary>
/// <typeparam name="T">数据类型参数</typeparam>
/// <param name="Data">加载完成的数据对象</param>
public sealed record DataLoadedEvent<T>(T Data);