// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Command;
using GFramework.Core.Rule;
using GFramework.Cqrs.Abstractions.Cqrs.Command;

namespace GFramework.Core.Command;

/// <summary>
///     异步命令的抽象基类，实现了IAsyncCommand接口
///     提供异步命令执行的基础框架和上下文感知功能
/// </summary>
public abstract class AbstractAsyncCommand : ContextAwareBase, IAsyncCommand
{
    /// <summary>
    ///     执行异步命令的实现方法
    ///     该方法通过调用受保护的抽象方法OnExecuteAsync来执行具体的命令逻辑
    /// </summary>
    /// <returns>表示异步操作的任务</returns>
    async Task IAsyncCommand.ExecuteAsync()
    {
        await OnExecuteAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     子类必须实现的异步执行方法
    ///     包含具体的命令执行逻辑
    /// </summary>
    /// <returns>表示异步操作的任务</returns>
    protected abstract Task OnExecuteAsync();
}

/// <summary>
///     抽象异步命令基类，为需要命令输入且无返回值的异步命令提供统一执行骨架。
/// </summary>
/// <typeparam name="TInput">命令输入类型，必须实现 <see cref="ICommandInput" /> 接口。</typeparam>
/// <param name="input">命令输入参数。</param>
public abstract class AbstractAsyncCommand<TInput>(TInput input) : ContextAwareBase, IAsyncCommand
    where TInput : ICommandInput
{
    /// <summary>
    ///     执行异步命令的实现方法。
    /// </summary>
    /// <returns>表示异步操作的任务。</returns>
    async Task IAsyncCommand.ExecuteAsync()
    {
        await OnExecuteAsync(input).ConfigureAwait(false);
    }

    /// <summary>
    ///     定义异步执行逻辑的抽象方法，由派生类实现具体业务逻辑。
    /// </summary>
    /// <param name="input">命令输入参数。</param>
    /// <returns>表示异步操作的任务。</returns>
    protected abstract Task OnExecuteAsync(TInput input);
}

/// <summary>
///     抽象异步命令基类，为需要命令输入且返回结果的异步命令提供统一执行骨架。
/// </summary>
/// <typeparam name="TInput">命令输入类型，必须实现 <see cref="ICommandInput" /> 接口。</typeparam>
/// <typeparam name="TResult">命令执行结果类型。</typeparam>
/// <param name="input">命令输入参数。</param>
public abstract class AbstractAsyncCommand<TInput, TResult>(TInput input) : ContextAwareBase, IAsyncCommand<TResult>
    where TInput : ICommandInput
{
    /// <summary>
    ///     执行异步命令并返回结果的实现方法。
    /// </summary>
    /// <returns>表示异步操作且包含结果的任务。</returns>
    async Task<TResult> IAsyncCommand<TResult>.ExecuteAsync()
    {
        return await OnExecuteAsync(input).ConfigureAwait(false);
    }

    /// <summary>
    ///     定义异步执行逻辑的抽象方法，由派生类实现具体业务逻辑并返回结果。
    /// </summary>
    /// <param name="input">命令输入参数。</param>
    /// <returns>表示异步操作且包含结果的任务。</returns>
    protected abstract Task<TResult> OnExecuteAsync(TInput input);
}
