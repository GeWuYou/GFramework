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
using GFramework.Cqrs.Abstractions.Cqrs.Command;

namespace GFramework.Cqrs.Cqrs.Command;

/// <summary>
/// 抽象流式命令处理器基类。
/// 继承自轻量 CQRS 上下文基类并实现 <see cref="IStreamRequestHandler{TRequest,TResponse}" />，
/// 为具体的流式命令处理器提供基础功能。
/// </summary>
/// <typeparam name="TCommand">流式命令类型，必须实现 <see cref="IStreamCommand{TResponse}" />。</typeparam>
/// <typeparam name="TResponse">流式命令响应元素类型。</typeparam>
/// <remarks>
/// 框架会在每次调用 <c>CreateStream</c> 进入实际处理逻辑前，为当前处理器实例注入架构上下文，
/// 因此派生类只能在 <see cref="Handle" /> 执行期间及其返回的异步枚举序列内假定 <c>Context</c> 可用。
/// 默认注册器会将流式命令处理器注册为瞬态服务，以避免同一个上下文感知实例在多个流或并发请求之间复用。
/// 派生类不应缓存处理器实例，也不应把依赖当前上下文的可变状态泄漏到流外部。
/// 传入 <see cref="Handle" /> 的取消令牌同时约束流的创建与后续枚举，
/// 派生类应在启动阶段和每次生成响应前尊重取消请求，避免在调用方停止枚举后继续执行后台工作。
/// </remarks>
public abstract class AbstractStreamCommandHandler<TCommand, TResponse> : CqrsContextAwareHandlerBase,
    IStreamRequestHandler<TCommand, TResponse>
    where TCommand : IStreamCommand<TResponse>
{
    /// <summary>
    /// 处理流式命令并返回异步可枚举的响应序列。
    /// 由具体的流式命令处理器子类实现流式处理逻辑。
    /// </summary>
    /// <param name="command">要处理的流式命令对象。</param>
    /// <param name="cancellationToken">取消令牌，用于取消流式处理操作。</param>
    /// <returns>异步可枚举的响应序列，每个元素类型为 <typeparamref name="TResponse" />。</returns>
    public abstract IAsyncEnumerable<TResponse> Handle(TCommand command, CancellationToken cancellationToken);
}
