// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Threading;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     记录双行为 pipeline 的实际执行顺序。
/// </summary>
internal static class DispatcherPipelineOrderState
{
    private static readonly Lock SyncRoot = new();
    private static readonly List<string> _steps = [];

    /// <summary>
    ///     获取按执行顺序追加的步骤快照。
    ///     共享状态通过 <c>SyncRoot</c> 串行化，避免并行行为测试互相污染步骤列表。
    /// </summary>
    public static IReadOnlyList<string> Steps
    {
        get
        {
            lock (SyncRoot)
            {
                return _steps.ToArray();
            }
        }
    }

    /// <summary>
    ///     记录一个新的 pipeline 执行步骤。
    /// </summary>
    /// <param name="step">要追加的步骤名称。</param>
    public static void Record(string step)
    {
        lock (SyncRoot)
        {
            _steps.Add(step);
        }
    }

    /// <summary>
    ///     清空当前记录，供下一次断言使用。
    /// </summary>
    public static void Reset()
    {
        lock (SyncRoot)
        {
            _steps.Clear();
        }
    }
}
