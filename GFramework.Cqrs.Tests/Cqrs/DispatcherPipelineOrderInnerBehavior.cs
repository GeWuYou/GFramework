// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     作为内层行为验证缓存 executor 复用后仍保持注册顺序。
/// </summary>
internal sealed class DispatcherPipelineOrderInnerBehavior : IPipelineBehavior<DispatcherPipelineOrderCacheRequest, int>
{
    /// <summary>
    ///     记录内层行为的前后执行节点。
    /// </summary>
    /// <param name="request">当前请求。</param>
    /// <param name="next">下一个处理阶段。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>下游处理器结果。</returns>
    public async ValueTask<int> Handle(
        DispatcherPipelineOrderCacheRequest request,
        MessageHandlerDelegate<DispatcherPipelineOrderCacheRequest, int> next,
        CancellationToken cancellationToken)
    {
        DispatcherPipelineOrderState.Record("Inner:Before");
        var result = await next(request, cancellationToken).ConfigureAwait(false);
        DispatcherPipelineOrderState.Record("Inner:After");
        return result;
    }
}
