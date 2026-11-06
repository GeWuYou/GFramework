// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GFramework.Cqrs.Benchmarks.Messaging;

/// <summary>
///     为 benchmark 场景提供统一的安静诊断日志工厂。
///     基准只度量调度耗时，不应让 runtime 诊断日志污染输出或计时，
///     因此统一基于标准 <see cref="ILoggerFactory" /> 挂接无输出 provider。
/// </summary>
internal static class BenchmarkLogging
{
    /// <summary>
    ///     benchmark 场景共用的标准日志工厂。
    /// </summary>
    private static readonly ILoggerFactory QuietLoggerFactory =
        LoggerFactory.Create(static builder => builder.AddProvider(new NoOpLoggerProvider()));

    /// <summary>
    ///     创建 benchmark 专用的安静诊断日志器。
    /// </summary>
    /// <param name="categoryName">日志类别名称。</param>
    /// <returns>所有级别都被过滤掉的标准日志器。</returns>
    public static ILogger CreateLogger(string categoryName)
    {
        ArgumentNullException.ThrowIfNull(categoryName);

        return QuietLoggerFactory.CreateLogger(categoryName);
    }

    /// <summary>
    ///     无输出日志提供程序：所有创建的日志器都退化为空实现，保证 benchmark 热路径零日志开销。
    /// </summary>
    private sealed class NoOpLoggerProvider : ILoggerProvider
    {
        /// <summary>
        ///     创建空实现日志器。
        /// </summary>
        /// <param name="categoryName">日志类别名称。</param>
        /// <returns>标准空日志器。</returns>
        public ILogger CreateLogger(string categoryName)
        {
            return NullLoggerFactory.Instance.CreateLogger(categoryName);
        }

        /// <summary>
        ///     释放提供程序。空实现无托管资源，无需额外清理。
        /// </summary>
        public void Dispose()
        {
        }
    }
}
