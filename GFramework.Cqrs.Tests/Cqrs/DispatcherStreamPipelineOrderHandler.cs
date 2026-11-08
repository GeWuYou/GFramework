// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     处理 <see cref="DispatcherStreamPipelineOrderRequest" /> 并记录最终 handler 执行位置。
/// </summary>
[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)]
internal sealed class DispatcherStreamPipelineOrderHandler : IStreamRequestHandler<DispatcherStreamPipelineOrderRequest, int>
{
    /// <summary>
    ///     记录 handler 执行步骤并返回稳定元素。
    /// </summary>
    /// <param name="request">当前流式请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>包含一个固定元素的异步流。</returns>
    public async IAsyncEnumerable<int> Handle(
        DispatcherStreamPipelineOrderRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        DispatcherStreamPipelineOrderState.Record("Handler");
        yield return 21;
        await Task.CompletedTask.ConfigureAwait(false);
    }
}
