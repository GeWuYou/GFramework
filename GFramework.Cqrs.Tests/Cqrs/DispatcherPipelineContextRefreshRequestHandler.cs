// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Threading;
using GFramework.Cqrs.Abstractions.Cqrs;
using GFramework.Cqrs.Cqrs;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     记录缓存 executor 复用场景下每次分发注入到 request handler 的上下文与实例身份。
/// </summary>
internal sealed class DispatcherPipelineContextRefreshRequestHandler
    : CqrsContextAwareHandlerBase,
        IRequestHandler<DispatcherPipelineContextRefreshRequest, int>
{
    private readonly int _instanceId = DispatcherPipelineContextRefreshState.AllocateHandlerInstanceId();

    /// <summary>
    ///     记录当前 handler 实例收到的上下文，并返回稳定结果。
    /// </summary>
    /// <param name="request">当前请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>固定整数结果。</returns>
    public ValueTask<int> Handle(
        DispatcherPipelineContextRefreshRequest request,
        CancellationToken cancellationToken)
    {
        DispatcherPipelineContextRefreshState.RecordHandler(request.DispatchId, _instanceId, Context);
        return ValueTask.FromResult(7);
    }
}
