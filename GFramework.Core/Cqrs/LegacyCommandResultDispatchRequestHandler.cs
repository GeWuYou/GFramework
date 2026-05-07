// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Core.Cqrs;

/// <summary>
///     处理 legacy 带返回值命令的 bridge handler。
/// </summary>
internal sealed class LegacyCommandResultDispatchRequestHandler
    : LegacyCqrsDispatchHandlerBase, IRequestHandler<LegacyCommandResultDispatchRequest, object?>
{
    /// <inheritdoc />
    public ValueTask<object?> Handle(LegacyCommandResultDispatchRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        PrepareTarget(request.Target);
        return ValueTask.FromResult(request.Execute());
    }
}
