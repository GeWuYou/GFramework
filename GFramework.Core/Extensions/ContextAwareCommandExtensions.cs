// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Command;
using GFramework.Core.Abstractions.Rule;

namespace GFramework.Core.Extensions;

/// <summary>
///     提供对 IContextAware 接口的命令执行扩展方法
/// </summary>
public static class ContextAwareCommandExtensions
{
    /// <summary>
    ///     发送一个带返回结果的命令
    /// </summary>
    /// <typeparam name="TResult">命令执行结果类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <param name="command">要发送的命令</param>
    /// <returns>命令执行结果</returns>
    /// <exception cref="ArgumentNullException">当 contextAware 或 command 为 null 时抛出</exception>
    public static TResult SendCommand<TResult>(this IContextAware contextAware,
        ICommand<TResult> command)
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        ArgumentNullException.ThrowIfNull(command);

        var context = contextAware.GetContext();
        return context.SendCommand(command);
    }

    /// <summary>
    ///     发送一个无返回结果的命令
    /// </summary>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <param name="command">要发送的命令</param>
    /// <exception cref="ArgumentNullException">当 contextAware 或 command 为 null 时抛出</exception>
    public static void SendCommand(this IContextAware contextAware, ICommand command)
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        ArgumentNullException.ThrowIfNull(command);

        var context = contextAware.GetContext();
        context.SendCommand(command);
    }


    /// <summary>
    ///     发送并异步执行一个无返回值的命令
    /// </summary>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <param name="command">要发送的命令</param>
    /// <exception cref="ArgumentNullException">当 contextAware 或 command 为 null 时抛出</exception>
    public static async Task SendCommandAsync(this IContextAware contextAware, IAsyncCommand command)
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        ArgumentNullException.ThrowIfNull(command);

        var context = contextAware.GetContext();
        await context.SendCommandAsync(command).ConfigureAwait(false);
    }

    /// <summary>
    ///     发送并异步执行一个带返回值的命令
    /// </summary>
    /// <typeparam name="TResult">命令执行结果类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <param name="command">要发送的命令</param>
    /// <returns>命令执行结果</returns>
    /// <exception cref="ArgumentNullException">当 contextAware 或 command 为 null 时抛出</exception>
    public static async Task<TResult> SendCommandAsync<TResult>(this IContextAware contextAware,
        IAsyncCommand<TResult> command)
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        ArgumentNullException.ThrowIfNull(command);

        var context = contextAware.GetContext();
        return await context.SendCommandAsync(command).ConfigureAwait(false);
    }
}
