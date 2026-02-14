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

using GFramework.Core.rule;
using Mediator;

namespace GFramework.Core.cqrs.query;

/// <summary>
/// 抽象流式查询处理器基类
/// 继承自ContextAwareBase并实现IStreamQueryHandler接口，为具体的流式查询处理器提供基础功能
/// 支持流式处理查询并产生异步可枚举的响应序列，适用于大数据量或实时数据查询场景
/// </summary>
/// <typeparam name="TQuery">流式查询类型，必须实现IStreamQuery接口</typeparam>
/// <typeparam name="TResponse">流式查询响应元素类型</typeparam>
public abstract class AbstractStreamQueryHandler<TQuery, TResponse> : ContextAwareBase,
    IStreamQueryHandler<TQuery, TResponse>
    where TQuery : IStreamQuery<TResponse>
{
    /// <summary>
    /// 处理流式查询并返回异步可枚举的响应序列
    /// 由具体的流式查询处理器子类实现流式查询处理逻辑
    /// </summary>
    /// <param name="query">要处理的流式查询对象</param>
    /// <param name="cancellationToken">取消令牌，用于取消流式查询操作</param>
    /// <returns>异步可枚举的响应序列，每个元素类型为TResponse</returns>
    public abstract IAsyncEnumerable<TResponse> Handle(TQuery query, CancellationToken cancellationToken);
}