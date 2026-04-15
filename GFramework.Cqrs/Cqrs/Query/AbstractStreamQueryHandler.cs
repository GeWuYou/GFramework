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

using GFramework.Cqrs.Abstractions.Cqrs;
using GFramework.Cqrs.Abstractions.Cqrs.Query;

namespace GFramework.Cqrs.Cqrs.Query;

/// <summary>
///     为流式查询处理器提供共享的 CQRS 上下文访问基类。
/// </summary>
/// <typeparam name="TQuery">流式查询类型，必须实现 <see cref="IStreamQuery{TResponse}" />。</typeparam>
/// <typeparam name="TResponse">流式查询响应元素类型。</typeparam>
/// <remarks>
///     该基类复用 <see cref="CqrsContextAwareHandlerBase" /> 的上下文注入能力，并实现
///     <see cref="IStreamRequestHandler{TQuery,TResponse}" /> 契约，让派生类只需聚焦于结果流的生成逻辑。
///     适用于需要逐步产出大量结果或长生命周期响应流的查询场景。
/// </remarks>
public abstract class AbstractStreamQueryHandler<TQuery, TResponse> : CqrsContextAwareHandlerBase,
    IStreamRequestHandler<TQuery, TResponse>
    where TQuery : IStreamQuery<TResponse>
{
    /// <summary>
    ///     处理流式查询并返回异步可枚举的响应序列。
    /// </summary>
    /// <param name="query">要处理的流式查询对象。</param>
    /// <param name="cancellationToken">用于停止结果流生成的取消令牌。</param>
    /// <returns>按需生成的异步响应序列。</returns>
    public abstract IAsyncEnumerable<TResponse> Handle(TQuery query, CancellationToken cancellationToken);
}
