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

namespace GFramework.Game.Abstractions.setting;

/// <summary>
///     定义一个可重置且可应用设置的接口
///     该接口继承自IResettable和IApplyAbleSettings接口，组合了重置功能和应用设置功能
/// </summary>
public interface IResetApplyAbleSettings : IResettable, IApplyAbleSettings
{
    /// <summary>
    ///     获取设置数据对象
    /// </summary>
    /// <returns>ISettingsData类型的设置数据</returns>
    ISettingsData Data { get; }

    /// <summary>
    ///     获取数据类型信息
    /// </summary>
    /// <returns>表示数据类型的Type对象</returns>
    Type DataType { get; }
}