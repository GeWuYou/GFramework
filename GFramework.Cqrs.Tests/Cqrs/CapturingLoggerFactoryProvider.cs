// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Threading;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Logging;
using GFramework.Cqrs.Tests.Logging;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     为 CQRS 注册测试捕获真实启动路径中创建的日志记录器。
/// </summary>
/// <remarks>
///     处理器注册入口会分别为测试运行时、容器和注册器创建日志器。
///     该提供程序统一保留这些测试日志器，以便断言警告是否经由公开入口真正发出。
///     并发创建日志器时会通过内部锁串行化，<see cref="Loggers" /> 每次返回快照，避免调用方观察到可变集合。
/// </remarks>
internal sealed class CapturingLoggerFactoryProvider : ILoggerFactoryProvider
{
    private readonly List<TestLogger> _loggers = [];
    private LogLevel _minLevel;
    private readonly Lock _sync = new();

    /// <summary>
    ///     使用指定的最小日志级别初始化一个新的捕获型日志工厂提供程序。
    /// </summary>
    /// <param name="minLevel">要应用到新建测试日志器的最小日志级别。</param>
    public CapturingLoggerFactoryProvider(LogLevel minLevel = LogLevel.Info)
    {
        _minLevel = minLevel;
    }

    /// <summary>
    ///     获取通过当前提供程序创建的全部测试日志器快照。
    /// </summary>
    public IReadOnlyList<TestLogger> Loggers
    {
        get
        {
            lock (_sync)
            {
                return _loggers.ToArray();
            }
        }
    }

    /// <summary>
    ///     获取或设置新建测试日志器的最小日志级别。
    /// </summary>
    public LogLevel MinLevel
    {
        get
        {
            lock (_sync)
            {
                return _minLevel;
            }
        }

        set
        {
            lock (_sync)
            {
                _minLevel = value;
            }
        }
    }

    /// <summary>
    ///     创建一个测试日志器并将其纳入捕获集合。
    /// </summary>
    /// <param name="name">日志记录器名称。</param>
    /// <returns>用于后续断言的测试日志器。</returns>
    public ILogger CreateLogger(string name)
    {
        lock (_sync)
        {
            var logger = new TestLogger(name, _minLevel);
            _loggers.Add(logger);
            return logger;
        }
    }
}
