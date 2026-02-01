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

namespace GFramework.Core.Abstractions.data;

/// <summary>
///     定义从指定类型数据源加载数据的接口
/// </summary>
/// <typeparam name="T">数据源的类型</typeparam>
public interface ILoadableFrom<in T>
{
    /// <summary>
    ///     从指定的数据源加载数据到当前对象
    /// </summary>
    /// <param name="source">用作数据源的对象，类型为T</param>
    void LoadFrom(T source);
}