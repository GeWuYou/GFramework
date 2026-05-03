// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     为 hidden implementation request invoker 回归提供“可见请求 + 隐藏实现类型”的测试替身容器。
/// </summary>
internal static class HiddenImplementationRequestInvokerContainer
{
    /// <summary>
    ///     用于验证 generated request invoker metadata 在隐藏实现类型场景下仍可被 dispatcher 消费的请求。
    /// </summary>
    /// <param name="Value">用于断言 generated 返回值的请求负载。</param>
    internal sealed record VisibleRequest(string Value) : IRequest<string>;

    /// <summary>
    ///     供 registrar 通过可见 handler interface 注册、但自身保持隐藏的 request handler 实现。
    /// </summary>
    private sealed class HiddenHandler : IRequestHandler<VisibleRequest, string>
    {
        /// <summary>
        ///     返回 runtime 路径专用结果，便于与 generated invoker 路径区分。
        /// </summary>
        /// <param name="request">当前测试请求。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>runtime handler 生成的响应字符串。</returns>
        public ValueTask<string> Handle(VisibleRequest request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            return ValueTask.FromResult($"runtime-hidden:{request.Value}");
        }
    }

    /// <summary>
    ///     返回当前隐藏 request handler 实现类型，供 generated registry 以反射注册语义模拟 hidden implementation 场景。
    /// </summary>
    internal static Type HiddenHandlerType => typeof(HiddenHandler);
}
