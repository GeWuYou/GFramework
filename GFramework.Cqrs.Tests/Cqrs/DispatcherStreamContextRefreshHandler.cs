// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using GFramework.Cqrs.Abstractions.Cqrs;
using GFramework.Cqrs.Cqrs;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     记录缓存 stream binding 复用场景下每次分发注入到 handler 的上下文与实例身份。
/// </summary>
internal sealed class DispatcherStreamContextRefreshHandler
    : CqrsContextAwareHandlerBase,
        IStreamRequestHandler<DispatcherStreamContextRefreshRequest, int>
{
    private readonly int _instanceId = DispatcherStreamContextRefreshState.AllocateHandlerInstanceId();

    /// <summary>
    ///     记录当前 handler 实例收到的上下文，并返回稳定元素。
    /// </summary>
    /// <param name="request">当前流请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>包含一个固定元素的异步流。</returns>
    public async IAsyncEnumerable<int> Handle(
        DispatcherStreamContextRefreshRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        DispatcherStreamContextRefreshState.Record(request.DispatchId, _instanceId, Context);
        yield return 11;
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
