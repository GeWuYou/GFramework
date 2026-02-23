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

namespace GFramework.Core.Abstractions.bases;

/// <summary>
/// 定义具有键值访问能力的接口契约
/// </summary>
/// <typeparam name="TKey">键的类型</typeparam>
public interface IHasKey<out TKey>
{
    /// <summary>
    /// 获取对象的键值
    /// </summary>
    TKey Key { get; }
}