// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Reflection;

namespace GFramework.Cqrs.Benchmarks.Messaging;

/// <summary>
///     提供 benchmark 共享的 dispatcher 静态缓存清理入口。
/// </summary>
/// <remarks>
///     `GFramework.Cqrs` runtime 会把反射绑定与 generated invoker 元数据缓存在静态字段中。
///     benchmark 需要在同一进程内重复比较 cold-start、reflection 与 generated 路径时，
///     显式清空这些缓存，避免前一组 benchmark 污染后续结果。
/// </remarks>
internal static class BenchmarkDispatcherCacheHelper
{
    /// <summary>
    ///     清空 dispatcher 上与 benchmark 对照相关的全部静态缓存。
    /// </summary>
    public static void ClearDispatcherCaches()
    {
        ClearDispatcherCache("NotificationDispatchBindings");
        ClearDispatcherCache("RequestDispatchBindings");
        ClearDispatcherCache("StreamDispatchBindings");
        ClearDispatcherCache("GeneratedRequestInvokers");
        ClearDispatcherCache("GeneratedStreamInvokers");
    }

    /// <summary>
    ///     通过反射定位并清空 dispatcher 的指定缓存字段。
    /// </summary>
    /// <param name="fieldName">要清理的静态缓存字段名。</param>
    /// <exception cref="InvalidOperationException">指定缓存字段不存在、返回空值或未暴露清理方法。</exception>
    internal static void ClearDispatcherCache(string fieldName)
    {
        var field = typeof(GFramework.Cqrs.CqrsRuntimeFactory).Assembly
            .GetType("GFramework.Cqrs.Internal.CqrsDispatcher", throwOnError: true)!
            .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException($"Missing dispatcher cache field {fieldName}.");
        var cache = field.GetValue(null)
                    ?? throw new InvalidOperationException($"Dispatcher cache field {fieldName} returned null.");
        var clearMethod = cache.GetType().GetMethod("Clear", BindingFlags.Public | BindingFlags.Instance)
                          ?? throw new InvalidOperationException(
                              $"Dispatcher cache field {fieldName} does not expose a Clear method.");
        _ = clearMethod.Invoke(cache, parameters: null);
    }
}
