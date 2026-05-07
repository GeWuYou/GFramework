// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Command;
using GFramework.Core.Cqrs;
using GFramework.Cqrs.Abstractions.Cqrs;
using IAsyncCommand = GFramework.Core.Abstractions.Command.IAsyncCommand;

namespace GFramework.Core.Command;

/// <summary>
/// 表示一个命令执行器，用于执行命令操作。
/// 该类实现了 ICommandExecutor 接口，提供命令执行的核心功能。
/// </summary>
public sealed class CommandExecutor(ICqrsRuntime? runtime = null) : ICommandExecutor
{
    private readonly ICqrsRuntime? _runtime = runtime;

    /// <summary>
    ///     获取当前执行器是否已接入统一 CQRS runtime。
    /// </summary>
    /// <remarks>
    ///     当调用方只是直接 new 一个执行器做纯单元测试时，这里允许为空，并回退到 legacy 直接执行路径；
    ///     当执行器由架构容器提供给 <see cref="Architectures.ArchitectureContext" /> 使用时，应始终传入 runtime，
    ///     以便旧入口也复用统一 pipeline 与 handler 调度链路。
    /// </remarks>
    public bool UsesCqrsRuntime => _runtime is not null;

    /// <summary>
    ///     发送并执行无返回值的命令
    /// </summary>
    /// <param name="command">要执行的命令对象，不能为空</param>
    /// <exception cref="ArgumentNullException">当command参数为null时抛出</exception>
    public void Send(ICommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (TryExecuteThroughCqrsRuntime(command, static currentCommand => new LegacyCommandDispatchRequest(currentCommand)))
        {
            return;
        }

        command.Execute();
    }

    /// <summary>
    ///     发送并执行有返回值的命令
    /// </summary>
    /// <typeparam name="TResult">命令执行结果的类型</typeparam>
    /// <param name="command">要执行的命令对象，不能为空</param>
    /// <returns>命令执行的结果</returns>
    /// <exception cref="ArgumentNullException">当command参数为null时抛出</exception>
    public TResult Send<TResult>(ICommand<TResult> command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (TryExecuteThroughCqrsRuntime(
                command,
                static currentCommand => new LegacyCommandResultDispatchRequest(
                    currentCommand,
                    () => currentCommand.Execute()),
                out TResult? result))
        {
            return result!;
        }

        return command.Execute();
    }

    /// <summary>
    ///     发送并异步执行无返回值的命令
    /// </summary>
    /// <param name="command">要执行的命令对象，不能为空</param>
    /// <exception cref="ArgumentNullException">当command参数为null时抛出</exception>
    public Task SendAsync(IAsyncCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var cqrsRuntime = _runtime;

        if (LegacyCqrsDispatchHelper.TryResolveDispatchContext(cqrsRuntime, command, out var context))
        {
            return cqrsRuntime.SendAsync(context, new LegacyAsyncCommandDispatchRequest(command)).AsTask();
        }

        return command.ExecuteAsync();
    }

    /// <summary>
    ///     发送并异步执行有返回值的命令
    /// </summary>
    /// <typeparam name="TResult">命令执行结果的类型</typeparam>
    /// <param name="command">要执行的命令对象，不能为空</param>
    /// <returns>命令执行的结果</returns>
    /// <exception cref="ArgumentNullException">当command参数为null时抛出</exception>
    public Task<TResult> SendAsync<TResult>(IAsyncCommand<TResult> command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var cqrsRuntime = _runtime;

        if (LegacyCqrsDispatchHelper.TryResolveDispatchContext(cqrsRuntime, command, out var context))
        {
            return BridgeAsyncCommandWithResultAsync(cqrsRuntime, context, command);
        }

        return command.ExecuteAsync();
    }

    /// <summary>
    ///     尝试通过统一 CQRS runtime 执行当前 legacy 请求。
    /// </summary>
    /// <typeparam name="TTarget">legacy 目标对象类型。</typeparam>
    /// <typeparam name="TRequest">bridge request 类型。</typeparam>
    /// <param name="target">即将执行的 legacy 目标对象。</param>
    /// <param name="requestFactory">用于创建 bridge request 的工厂。</param>
    /// <returns>若成功切入 CQRS runtime 则返回 <see langword="true" />；否则返回 <see langword="false" />。</returns>
    private bool TryExecuteThroughCqrsRuntime<TTarget, TRequest>(
        TTarget target,
        Func<TTarget, TRequest> requestFactory)
        where TTarget : class
        where TRequest : IRequest<Unit>
    {
        var cqrsRuntime = _runtime;

        if (!LegacyCqrsDispatchHelper.TryResolveDispatchContext(cqrsRuntime, target, out var context))
        {
            return false;
        }

        LegacyCqrsDispatchHelper.SendSynchronously(cqrsRuntime, context, requestFactory(target));
        return true;
    }

    /// <summary>
    ///     尝试通过统一 CQRS runtime 执行当前 legacy 请求，并返回装箱结果。
    /// </summary>
    /// <typeparam name="TTarget">legacy 目标对象类型。</typeparam>
    /// <typeparam name="TResult">预期结果类型。</typeparam>
    /// <typeparam name="TRequest">bridge request 类型。</typeparam>
    /// <param name="target">即将执行的 legacy 目标对象。</param>
    /// <param name="requestFactory">用于创建 bridge request 的工厂。</param>
    /// <param name="result">若命中 bridge，则返回执行结果；否则返回默认值。</param>
    /// <returns>若成功切入 CQRS runtime 则返回 <see langword="true" />；否则返回 <see langword="false" />。</returns>
    private bool TryExecuteThroughCqrsRuntime<TTarget, TResult, TRequest>(
        TTarget target,
        Func<TTarget, TRequest> requestFactory,
        out TResult? result)
        where TTarget : class
        where TRequest : IRequest<object?>
    {
        var cqrsRuntime = _runtime;

        if (!LegacyCqrsDispatchHelper.TryResolveDispatchContext(cqrsRuntime, target, out var context))
        {
            result = default;
            return false;
        }

        var boxedResult = LegacyCqrsDispatchHelper.SendSynchronously(cqrsRuntime, context, requestFactory(target));
        result = (TResult)boxedResult!;
        return true;
    }

    /// <summary>
    ///     通过统一 CQRS runtime 异步执行 legacy 带返回值命令，并把装箱结果还原为目标类型。
    /// </summary>
    /// <typeparam name="TResult">命令返回值类型。</typeparam>
    /// <param name="runtime">负责调度当前 bridge request 的统一 CQRS runtime。</param>
    /// <param name="context">当前架构上下文。</param>
    /// <param name="command">要桥接的 legacy 命令。</param>
    /// <returns>命令执行结果。</returns>
    private static async Task<TResult> BridgeAsyncCommandWithResultAsync<TResult>(
        ICqrsRuntime runtime,
        GFramework.Core.Abstractions.Architectures.IArchitectureContext context,
        IAsyncCommand<TResult> command)
    {
        var boxedResult = await runtime.SendAsync(
                context,
                new LegacyAsyncCommandResultDispatchRequest(
                    command,
                    async () => await command.ExecuteAsync().ConfigureAwait(false)))
            .ConfigureAwait(false);
        return (TResult)boxedResult!;
    }
}
