// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Command;
using GFramework.Core.Rule;
using GFramework.Cqrs.Abstractions.Cqrs.Command;
using ICommand = GFramework.Core.Abstractions.Command.ICommand;

namespace GFramework.Core.Command;

/// <summary>
///     抽象命令类，实现 ICommand 接口，为具体命令提供基础架构支持
/// </summary>
public abstract class AbstractCommand : ContextAwareBase, ICommand
{
    /// <summary>
    ///     执行命令的入口方法，实现 ICommand 接口的 Execute 方法
    /// </summary>
    void ICommand.Execute()
    {
        OnExecute();
    }

    /// <summary>
    ///     命令执行的抽象方法，由派生类实现具体的命令逻辑
    /// </summary>
    protected abstract void OnExecute();
}

/// <summary>
///     抽象命令类，实现 <see cref="ICommand" /> 接口，为需要命令输入的具体命令提供基础架构支持。
/// </summary>
/// <typeparam name="TInput">命令输入参数类型，必须实现 <see cref="ICommandInput" /> 接口。</typeparam>
/// <param name="input">命令执行所需的输入参数。</param>
public abstract class AbstractCommand<TInput>(TInput input) : ContextAwareBase, ICommand
    where TInput : ICommandInput
{
    /// <summary>
    ///     执行命令的入口方法，实现 <see cref="ICommand" /> 接口的 <c>Execute</c> 方法。
    /// </summary>
    void ICommand.Execute()
    {
        OnExecute(input);
    }

    /// <summary>
    ///     命令执行的抽象方法，由派生类实现具体的命令逻辑。
    /// </summary>
    /// <param name="input">命令执行所需的输入参数。</param>
    protected abstract void OnExecute(TInput input);
}

/// <summary>
///     带返回值的抽象命令类，为需要输入和返回值的命令提供统一执行骨架。
/// </summary>
/// <typeparam name="TInput">命令输入参数类型，必须实现 <see cref="ICommandInput" /> 接口。</typeparam>
/// <typeparam name="TResult">命令执行后返回的结果类型。</typeparam>
/// <param name="input">命令执行所需的输入参数。</param>
public abstract class AbstractCommand<TInput, TResult>(TInput input)
    : ContextAwareBase, GFramework.Core.Abstractions.Command.ICommand<TResult>
    where TInput : ICommandInput
{
    /// <summary>
    ///     执行命令的入口方法，实现 <see cref="GFramework.Core.Abstractions.Command.ICommand{TResult}" /> 接口的
    ///     <c>Execute</c> 方法。
    /// </summary>
    /// <returns>命令执行后的结果。</returns>
    TResult GFramework.Core.Abstractions.Command.ICommand<TResult>.Execute()
    {
        return OnExecute(input);
    }

    /// <summary>
    ///     命令执行的抽象方法，由派生类实现具体的命令逻辑。
    /// </summary>
    /// <param name="input">命令执行所需的输入参数。</param>
    /// <returns>命令执行后的结果。</returns>
    protected abstract TResult OnExecute(TInput input);
}
