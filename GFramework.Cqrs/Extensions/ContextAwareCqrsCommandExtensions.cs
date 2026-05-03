// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Rule;
using GFramework.Cqrs.Abstractions.Cqrs.Command;

namespace GFramework.Cqrs.Extensions;

/// <summary>
///     提供对 <see cref="IContextAware" /> 接口的 CQRS 命令扩展方法。
/// </summary>
/// <remarks>
///     该扩展类将命令分发统一路由到架构上下文中的 CQRS 运行时。
/// </remarks>
public static class ContextAwareCqrsCommandExtensions
{
    /// <summary>
    ///     发送命令的同步版本（不推荐，仅用于兼容同步调用链）。
    /// </summary>
    /// <typeparam name="TResponse">命令响应类型。</typeparam>
    /// <param name="contextAware">实现 <see cref="IContextAware" /> 接口的对象。</param>
    /// <param name="command">要发送的命令对象。</param>
    /// <returns>命令执行结果。</returns>
    /// <exception cref="ArgumentNullException">
    ///     当 <paramref name="contextAware" /> 或 <paramref name="command" /> 为 <see langword="null" /> 时抛出。
    /// </exception>
    /// <remarks>
    ///     同步方法仅用于兼容同步调用链；新代码建议优先使用异步版本。
    /// </remarks>
    public static TResponse SendCommand<TResponse>(this IContextAware contextAware, ICommand<TResponse> command)
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        ArgumentNullException.ThrowIfNull(command);

        return contextAware.GetContext().SendCommand(command);
    }

    /// <summary>
    ///     异步发送命令并返回结果。
    /// </summary>
    /// <typeparam name="TResponse">命令响应类型。</typeparam>
    /// <param name="contextAware">实现 <see cref="IContextAware" /> 接口的对象。</param>
    /// <param name="command">要发送的命令对象。</param>
    /// <param name="cancellationToken">取消令牌，用于取消操作。</param>
    /// <returns>包含命令执行结果的 <see cref="ValueTask{TResult}" />。</returns>
    /// <exception cref="ArgumentNullException">
    ///     当 <paramref name="contextAware" /> 或 <paramref name="command" /> 为 <see langword="null" /> 时抛出。
    /// </exception>
    /// <remarks>
    ///     该方法直接返回底层 <see cref="ValueTask{TResult}" />，避免额外的 async 状态机分配。
    /// </remarks>
    public static ValueTask<TResponse> SendCommandAsync<TResponse>(
        this IContextAware contextAware,
        ICommand<TResponse> command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        ArgumentNullException.ThrowIfNull(command);

        return contextAware.GetContext().SendCommandAsync(command, cancellationToken);
    }
}
