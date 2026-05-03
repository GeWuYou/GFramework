// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     供 generated stream invoker provider 测试使用的流式请求处理器。
/// </summary>
internal sealed class GeneratedStreamInvokerRequestHandler : IStreamRequestHandler<GeneratedStreamInvokerRequest, int>
{
    /// <summary>
    ///     返回带有运行时处理器语义的异步流，便于和 generated invoker 自定义结果区分。
    /// </summary>
    /// <param name="request">当前测试流式请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>运行时处理器生成的异步流结果。</returns>
    public IAsyncEnumerable<int> Handle(GeneratedStreamInvokerRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return StreamResultsAsync(request.Start, cancellationToken);
    }

    /// <summary>
    ///     生成用于区分 runtime 路径的固定异步流结果。
    /// </summary>
    private static async IAsyncEnumerable<int> StreamResultsAsync(
        int start,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return start;
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();
        yield return start + 1;
    }
}
