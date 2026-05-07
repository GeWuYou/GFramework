// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Threading;
using GFramework.Core.Cqrs;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     为 legacy bridge pipeline 回归测试保存跨泛型闭包共享的计数状态。
/// </summary>
/// <remarks>
///     该计数器通过 <see cref="Interlocked.Increment(ref int)" /> 原子递增，并使用
///     <see cref="Volatile" /> 读写，因此单次读写操作本身是线程安全的。
///     由于状态在同一进程内跨 fixture 共享，所有使用它的测试都必须在清理阶段调用 <see cref="Reset" />，
///     以避免并行或失败测试把旧计数泄露给后续断言。
/// </remarks>
public static class LegacyBridgePipelineTracker
{
    private static int _invocationCount;

    /// <summary>
    ///     获取当前进程内被识别为 legacy bridge request 的 pipeline 命中次数。
    /// </summary>
    public static int InvocationCount => Volatile.Read(ref _invocationCount);

    /// <summary>
    ///     重置计数器。
    /// </summary>
    public static void Reset()
    {
        Volatile.Write(ref _invocationCount, 0);
    }

    /// <summary>
    ///     若当前请求类型属于 Core legacy bridge request，则记录一次命中。
    /// </summary>
    public static void Record(Type requestType)
    {
        ArgumentNullException.ThrowIfNull(requestType);

        if (typeof(LegacyCqrsDispatchRequestBase).IsAssignableFrom(requestType))
        {
            Interlocked.Increment(ref _invocationCount);
        }
    }
}
