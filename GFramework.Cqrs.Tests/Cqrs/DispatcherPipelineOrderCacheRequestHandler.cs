// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     为双行为顺序回归提供最终请求处理器。
/// </summary>
internal sealed class DispatcherPipelineOrderCacheRequestHandler : IRequestHandler<DispatcherPipelineOrderCacheRequest, int>
{
    /// <summary>
    ///     记录处理器执行并返回固定结果。
    /// </summary>
    /// <param name="request">当前请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>固定整数结果。</returns>
    public ValueTask<int> Handle(DispatcherPipelineOrderCacheRequest request, CancellationToken cancellationToken)
    {
        DispatcherPipelineOrderState.Record("Handler");
        return ValueTask.FromResult(3);
    }
}
