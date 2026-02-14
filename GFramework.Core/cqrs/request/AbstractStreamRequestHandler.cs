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

namespace GFramework.Core.cqrs.request;

/// <summary>
/// 抽象流式请求处理器基类
/// 继承自ContextAwareBase并实现IStreamRequestHandler接口，为具体的流式请求处理器提供基础功能
/// 支持流式处理请求并产生异步可枚举的响应序列，适用于需要逐步返回结果的请求处理场景
/// </summary>
/// <typeparam name="TRequest">流式请求类型，必须实现IStreamRequest接口</typeparam>
/// <typeparam name="TResponse">流式请求响应元素类型</typeparam>
public abstract class AbstractStreamRequestHandler<TRequest, TResponse> : ContextAwareBase,
    IStreamRequestHandler<TRequest, TResponse>
    where TRequest : IStreamRequest<TResponse>
{
    /// <summary>
    /// 处理流式请求并返回异步可枚举的响应序列
    /// 由具体的流式请求处理器子类实现流式请求处理逻辑
    /// </summary>
    /// <param name="request">要处理的流式请求对象</param>
    /// <param name="cancellationToken">取消令牌，用于取消流式请求操作</param>
    /// <returns>异步可枚举的响应序列，每个元素类型为TResponse</returns>
    public abstract IAsyncEnumerable<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}