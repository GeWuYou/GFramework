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
///     表示数据保存事件的记录类型
/// </summary>
/// <typeparam name="T">保存的数据类型</typeparam>
/// <param name="Data">保存的数据实例</param>
public sealed record DataSavedEvent<T>(T Data);