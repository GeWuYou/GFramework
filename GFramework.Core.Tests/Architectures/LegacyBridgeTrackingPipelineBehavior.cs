// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Threading;
using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     记录 legacy Core CQRS bridge request 是否经过统一 CQRS pipeline 的测试行为。
/// </summary>
public sealed class LegacyBridgeTrackingPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <inheritdoc />
    public async ValueTask<TResponse> Handle(
        TRequest message,
        MessageHandlerDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken)
    {
        LegacyBridgePipelineTracker.Record(typeof(TRequest));
        return await next(message, cancellationToken).ConfigureAwait(false);
    }
}
