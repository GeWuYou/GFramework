// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     为 hidden implementation stream invoker 回归提供“可见请求 + 隐藏实现类型”的测试替身容器。
/// </summary>
internal static class HiddenImplementationStreamInvokerContainer
{
    /// <summary>
    ///     用于验证 generated stream invoker metadata 在隐藏实现类型场景下仍可被 dispatcher 消费的流式请求。
    /// </summary>
    /// <param name="Start">用于构造 generated stream 输出的起始值。</param>
    internal sealed record VisibleStreamRequest(int Start) : IStreamRequest<int>;

    /// <summary>
    ///     供 registrar 通过可见 stream handler interface 注册、但自身保持隐藏的流式 handler 实现。
    /// </summary>
    private sealed class HiddenHandler : IStreamRequestHandler<VisibleStreamRequest, int>
    {
        /// <summary>
        ///     返回 runtime 路径专用异步流，便于与 generated invoker 路径区分。
        /// </summary>
        /// <param name="request">当前测试流式请求。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>runtime handler 生成的异步流结果。</returns>
        public IAsyncEnumerable<int> Handle(VisibleStreamRequest request, CancellationToken cancellationToken)
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

    /// <summary>
    ///     返回当前隐藏 stream handler 实现类型，供 generated registry 以反射注册语义模拟 hidden implementation 场景。
    /// </summary>
    internal static Type HiddenHandlerType => typeof(HiddenHandler);
}
