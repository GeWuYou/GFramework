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

using GFramework.Core.Rule;
using GFramework.Cqrs.Abstractions.Cqrs;
using GFramework.Cqrs.Abstractions.Cqrs.Command;

namespace GFramework.Core.Cqrs.Command;

/// <summary>
/// 抽象命令处理器基类
/// 继承自 ContextAwareBase 并实现 IRequestHandler 接口，为具体的命令处理器提供基础功能。
/// 框架会在每次分发前注入当前架构上下文，因此派生类可以通过 Context 访问架构级服务。
/// </summary>
/// <typeparam name="TCommand">命令类型</typeparam>
public abstract class AbstractCommandHandler<TCommand> : ContextAwareBase, IRequestHandler<TCommand, Unit>
    where TCommand : ICommand<Unit>
{
    /// <summary>
    /// 处理指定的命令
    /// 由具体的命令处理器子类实现命令处理逻辑
    /// </summary>
    /// <param name="command">要处理的命令对象</param>
    /// <param name="cancellationToken">取消令牌，用于取消操作</param>
    /// <returns>表示异步操作完成的ValueTask，返回Unit类型表示无返回值</returns>
    public abstract ValueTask<Unit> Handle(TCommand command, CancellationToken cancellationToken);
}

/// <summary>
/// 抽象命令处理器基类（带返回值版本）
/// 继承自 ContextAwareBase 并实现 IRequestHandler 接口，为具体的命令处理器提供基础功能。
/// 支持泛型命令和结果类型，框架会在每次分发前注入当前架构上下文。
/// </summary>
/// <typeparam name="TCommand">命令类型，必须实现ICommand接口</typeparam>
/// <typeparam name="TResult">命令执行结果类型</typeparam>
public abstract class AbstractCommandHandler<TCommand, TResult> : ContextAwareBase, IRequestHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    /// <summary>
    /// 处理指定的命令并返回结果
    /// 由具体的命令处理器子类实现命令处理逻辑
    /// </summary>
    /// <param name="command">要处理的命令对象</param>
    /// <param name="cancellationToken">取消令牌，用于取消操作</param>
    /// <returns>表示异步操作完成的ValueTask，包含命令执行结果</returns>
    public abstract ValueTask<TResult> Handle(TCommand command, CancellationToken cancellationToken);
}
