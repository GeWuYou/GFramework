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

using GFramework.Core.Abstractions.cqrs.query;
using Mediator;

namespace GFramework.Core.cqrs.query;

/// <summary>
/// 表示一个基础查询类，用于处理带有输入和响应的查询模式实现。
/// 该类继承自 Mediator.IQuery&lt;TResponse&gt; 接口，提供了通用的查询结构。
/// </summary>
/// <typeparam name="TInput">查询输入数据的类型，必须实现 IQueryInput 接口</typeparam>
/// <typeparam name="TResponse">查询执行后返回结果的类型</typeparam>
/// <param name="input">查询执行所需的输入数据</param>
public class QueryBase<TInput, TResponse>(TInput input) : IQuery<TResponse> where TInput : IQueryInput
{
    /// <summary>
    /// 获取查询的输入数据。
    /// </summary>
    public TInput Input => input;
}