// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Core.Cqrs;

/// <summary>
///     处理 legacy 无返回值命令的 bridge handler。
/// </summary>
internal sealed class LegacyCommandDispatchRequestHandler
    : LegacyCqrsDispatchHandlerBase, IRequestHandler<LegacyCommandDispatchRequest, Unit>
{
    /// <inheritdoc />
    public ValueTask<Unit> Handle(LegacyCommandDispatchRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        PrepareTarget(request.Command);
        request.Command.Execute();
        return ValueTask.FromResult(Unit.Value);
    }
}
