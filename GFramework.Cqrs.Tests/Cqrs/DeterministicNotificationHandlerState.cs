// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     记录确定性通知处理器的实际执行顺序。
/// </summary>
internal static class DeterministicNotificationHandlerState
{
    /// <summary>
    ///     获取当前测试中的通知处理器执行顺序。
    /// </summary>
    /// <remarks>
    ///     该集合仅供顺序测试断言使用，不提供并发安全保证。
    ///     若多个处理器在并行测试中同时写入，调用方可能观察到竞争条件或未定义顺序。
    /// </remarks>
    public static List<string> InvocationOrder { get; } = [];

    /// <summary>
    ///     重置共享的执行顺序状态。
    /// </summary>
    /// <remarks>
    ///     该方法只支持在单线程测试准备阶段调用；并发调用会与 <see cref="InvocationOrder" /> 的直接写入互相竞争。
    /// </remarks>
    public static void Reset()
    {
        InvocationOrder.Clear();
    }
}
