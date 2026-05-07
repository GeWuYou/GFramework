// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Core.Cqrs;

/// <summary>
///     处理 legacy 异步无返回值命令的 bridge handler。
/// </summary>
internal sealed class LegacyAsyncCommandDispatchRequestHandler
    : LegacyCqrsDispatchHandlerBase, IRequestHandler<LegacyAsyncCommandDispatchRequest, Unit>
{
    /// <inheritdoc />
    public async ValueTask<Unit> Handle(
        LegacyAsyncCommandDispatchRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        // Legacy ExecuteAsync contract does not accept CancellationToken; use WaitAsync so the caller can still observe cancellation promptly.
        cancellationToken.ThrowIfCancellationRequested();
        PrepareTarget(request.Command);
        await request.Command.ExecuteAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
