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
/// 抽象请求处理器基类，用于处理不返回具体响应的请求
/// 继承自ContextAwareBase并实现IRequestHandler接口
/// </summary>
/// <typeparam name="TRequest">请求类型，必须实现IRequest[Unit]接口</typeparam>
public abstract class AbstractRequestHandler<TRequest> : ContextAwareBase, IRequestHandler<TRequest>
    where TRequest : IRequest<Unit>
{
    /// <summary>
    /// 处理请求的核心方法
    /// </summary>
    /// <param name="request">要处理的请求对象</param>
    /// <param name="cancellationToken">取消令牌，用于取消操作</param>
    /// <returns>表示异步操作的ValueTask，完成时返回Unit值</returns>
    public abstract ValueTask<Unit> Handle(TRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// 抽象请求处理器基类，用于处理需要返回具体响应的请求
/// 继承自ContextAwareBase并实现IRequestHandler接口
/// </summary>
/// <typeparam name="TRequest">请求类型，必须实现IRequest[TResponse]接口</typeparam>
/// <typeparam name="TResponse">响应类型</typeparam>
public abstract class AbstractRequestHandler<TRequest, TResponse> : ContextAwareBase,
    IRequestHandler<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// 处理请求并返回响应的核心方法
    /// </summary>
    /// <param name="request">要处理的请求对象</param>
    /// <param name="cancellationToken">取消令牌，用于取消操作</param>
    /// <returns>表示异步操作的ValueTask，完成时返回处理结果</returns>
    public abstract ValueTask<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}