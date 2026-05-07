// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using CoreCommand = GFramework.Core.Abstractions.Command;
using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Core.Cqrs;

/// <summary>
///     包装 legacy 无返回值命令，使其能够通过自有 CQRS runtime 调度。
/// </summary>
internal sealed class LegacyCommandDispatchRequest(CoreCommand.ICommand command)
    : LegacyCqrsDispatchRequestBase(command), IRequest<Unit>
{
    /// <summary>
    ///     获取当前 bridge request 代理的命令实例。
    /// </summary>
    public CoreCommand.ICommand Command { get; } = command ?? throw new ArgumentNullException(nameof(command));
}
