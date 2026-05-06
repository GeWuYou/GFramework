// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace GFramework.Cqrs.Benchmarks.Messaging;

/// <summary>
///     统一处理 benchmark 宿主的资源释放，避免前一个 <see cref="IDisposable" /> 抛错后中断后续清理。
/// </summary>
internal static class BenchmarkCleanupHelper
{
    /// <summary>
    ///     按顺序释放一组 benchmark 资源，并在全部资源都尝试释放后再回抛异常。
    /// </summary>
    /// <param name="disposables">当前 benchmark 宿主拥有并负责释放的资源。</param>
    /// <exception cref="Exception">
    ///     当且仅当至少一个资源释放失败时抛出。
    ///     单个失败会回抛原始异常，多个失败会聚合为 <see cref="AggregateException" />。
    /// </exception>
    public static void DisposeAll(params IDisposable?[] disposables)
    {
        List<Exception>? exceptions = null;

        foreach (var disposable in disposables)
        {
            if (disposable is null)
            {
                continue;
            }

            try
            {
                disposable.Dispose();
            }
            catch (Exception exception)
            {
                exceptions ??= [];
                exceptions.Add(exception);
            }
        }

        if (exceptions is null)
        {
            return;
        }

        if (exceptions.Count == 1)
        {
            ExceptionDispatchInfo.Capture(exceptions[0]).Throw();
        }

        throw new AggregateException("One or more benchmark resources failed to dispose cleanly.", exceptions);
    }
}
