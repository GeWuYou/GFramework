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
/// 抽象查询处理器基类
/// 继承自ContextAwareBase并实现IQueryHandler接口，为具体的查询处理器提供基础功能
/// 支持泛型查询和结果类型，实现CQRS模式中的查询处理
/// </summary>
/// <typeparam name="TQuery">查询类型，必须实现IQuery接口</typeparam>
/// <typeparam name="TResult">查询结果类型</typeparam>
public abstract class AbstractQueryHandler<TQuery, TResult> : ContextAwareBase, IQueryHandler<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    /// <summary>
    /// 处理指定的查询并返回结果
    /// 由具体的查询处理器子类实现查询处理逻辑
    /// </summary>
    /// <param name="query">要处理的查询对象</param>
    /// <param name="cancellationToken">取消令牌，用于取消操作</param>
    /// <returns>表示异步操作完成的ValueTask，包含查询结果</returns>
    public abstract ValueTask<TResult> Handle(TQuery query, CancellationToken cancellationToken);
}