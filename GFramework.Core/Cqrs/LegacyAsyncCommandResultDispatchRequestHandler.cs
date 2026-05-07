// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Core.Cqrs;

/// <summary>
///     处理 legacy 异步带返回值命令的 bridge handler。
/// </summary>
internal sealed class LegacyAsyncCommandResultDispatchRequestHandler
    : LegacyCqrsDispatchHandlerBase, IRequestHandler<LegacyAsyncCommandResultDispatchRequest, object?>
{
    /// <inheritdoc />
    public async ValueTask<object?> Handle(
        LegacyAsyncCommandResultDispatchRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        // Legacy ExecuteAsync contract does not accept CancellationToken; use WaitAsync so the caller can observe cancellation promptly.
        cancellationToken.ThrowIfCancellationRequested();
        PrepareTarget(request.Target);
        return await request.ExecuteAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
    }
}
