// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     处理 <see cref="DispatcherZeroPipelineStreamRequest" />，用于验证零管道 stream 的 dispatcher 缓存路径。
/// </summary>
[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)]
internal sealed class DispatcherZeroPipelineStreamHandler : IStreamRequestHandler<DispatcherZeroPipelineStreamRequest, int>
{
    /// <summary>
    ///     返回一个单元素异步流，便于在缓存测试中最小化处理噪音。
    /// </summary>
    /// <param name="request">当前零管道 stream 请求。</param>
    /// <param name="cancellationToken">用于终止异步枚举的取消令牌。</param>
    /// <returns>只包含一个元素的异步响应流。</returns>
    public IAsyncEnumerable<int> Handle(DispatcherZeroPipelineStreamRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Handle(request, cancellationToken);
        static async IAsyncEnumerable<int> Handle(DispatcherZeroPipelineStreamRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            _ = request;
            cancellationToken.ThrowIfCancellationRequested();

            yield return 1;
        }
    }
}
