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

using GFramework.Core.Abstractions.coroutine;
using GFramework.Core.Abstractions.query;
using GFramework.Core.Abstractions.rule;

namespace GFramework.Core.coroutine.extensions;

/// <summary>
///     查询协程扩展方法类
///     提供将查询操作包装为协程的扩展方法
/// </summary>
public static class QueryCoroutineExtensions
{
    /// <summary>
    ///     将 Query 包装为协程（立即返回结果）
    /// </summary>
    /// <typeparam name="TQuery">查询类型，必须实现 IQuery&lt;TResult&gt; 接口</typeparam>
    /// <typeparam name="TResult">查询结果类型</typeparam>
    /// <param name="contextAware">上下文感知对象，用于获取执行上下文</param>
    /// <param name="query">要执行的查询对象</param>
    /// <param name="onResult">处理查询结果的回调方法</param>
    /// <returns>返回一个协程迭代器，立即执行并返回结果</returns>
    public static IEnumerator<IYieldInstruction> SendQueryCoroutine<TQuery, TResult>(
        this IContextAware contextAware,
        TQuery query,
        Action<TResult> onResult)
        where TQuery : IQuery<TResult>
    {
        // 执行查询并获取结果
        var result = contextAware.GetContext().SendQuery(query);

        // 调用结果处理回调
        onResult(result);

        // 协程立即结束
        yield break;
    }
}