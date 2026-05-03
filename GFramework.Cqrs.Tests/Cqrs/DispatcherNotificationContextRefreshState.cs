// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Threading;
using GFramework.Core.Abstractions.Architectures;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     记录 notification dispatch binding 缓存回归中每次分发实际使用的上下文与实例身份。
/// </summary>
internal static class DispatcherNotificationContextRefreshState
{
    private static readonly Lock SyncRoot = new();
    private static int _nextHandlerInstanceId;
    private static readonly List<DispatcherPipelineContextSnapshot> _handlerSnapshots = [];

    /// <summary>
    ///     获取每次 notification 分发时记录的快照副本。
    ///     共享状态通过 <c>SyncRoot</c> 串行化，避免并行测试写入抖动。
    /// </summary>
    public static IReadOnlyList<DispatcherPipelineContextSnapshot> HandlerSnapshots
    {
        get
        {
            lock (SyncRoot)
            {
                return _handlerSnapshots.ToArray();
            }
        }
    }

    /// <summary>
    ///     为新的 handler 测试实例分配稳定编号。
    /// </summary>
    public static int AllocateHandlerInstanceId()
    {
        return Interlocked.Increment(ref _nextHandlerInstanceId);
    }

    /// <summary>
    ///     记录 handler 在当前分发中观察到的上下文。
    /// </summary>
    public static void Record(string dispatchId, int instanceId, IArchitectureContext context)
    {
        lock (SyncRoot)
        {
            _handlerSnapshots.Add(new DispatcherPipelineContextSnapshot(dispatchId, instanceId, context));
        }
    }

    /// <summary>
    ///     清空历史记录与实例编号，避免跨测试污染断言。
    /// </summary>
    public static void Reset()
    {
        lock (SyncRoot)
        {
            _nextHandlerInstanceId = 0;
            _handlerSnapshots.Clear();
        }
    }
}
