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

namespace GFramework.Game.data;

/// <summary>
///     存档系统配置
/// </summary>
public sealed class SaveConfiguration
{
    /// <summary>
    ///     存档根目录 (如 "user://saves")
    /// </summary>
    public string SaveRoot { get; init; } = "user://saves";

    /// <summary>
    ///     存档槽位前缀 (如 "slot_")
    /// </summary>
    public string SaveSlotPrefix { get; init; } = "slot_";

    /// <summary>
    ///     存档文件名 (如 "save.json")
    /// </summary>
    public string SaveFileName { get; init; } = "save.json";
}
