// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using CoreCommand = GFramework.Core.Abstractions.Command;
using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Core.Cqrs;

/// <summary>
///     包装 legacy 异步无返回值命令，使其能够通过自有 CQRS runtime 调度。
/// </summary>
/// <param name="command">当前 bridge request 代理的 legacy 异步命令实例。</param>
internal sealed class LegacyAsyncCommandDispatchRequest(CoreCommand.IAsyncCommand command)
    : LegacyCqrsDispatchRequestBase(ValidateCommand(command)), IRequest<Unit>
{
    /// <summary>
    ///     获取当前 bridge request 代理的异步命令实例。
    /// </summary>
    public CoreCommand.IAsyncCommand Command { get; } = command;

    private static CoreCommand.IAsyncCommand ValidateCommand(CoreCommand.IAsyncCommand command)
    {
        return command ?? throw new ArgumentNullException(nameof(command));
    }
}
