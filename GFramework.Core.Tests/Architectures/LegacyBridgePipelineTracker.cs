// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Threading;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     为 legacy bridge pipeline 回归测试保存跨泛型闭包共享的计数状态。
/// </summary>
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

        if (string.Equals(requestType.Namespace, "GFramework.Core.Cqrs", StringComparison.Ordinal) &&
            requestType.Name.Contains("Legacy", StringComparison.Ordinal))
        {
            Interlocked.Increment(ref _invocationCount);
        }
    }
}
