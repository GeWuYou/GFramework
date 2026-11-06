// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using GFramework.Core.Abstractions.Logging;
using Microsoft.Extensions.Logging;

namespace GFramework.Core.Logging;

/// <summary>
///     标准 <see cref="ILoggerProvider"/> 到自有输出引擎（appender/formatter/filter 管线）的适配器。
///     标准日志调用被转换为 <see cref="LogEntry"/> 后交给注册的 appender 集合，
///     使 Console/File/RollingFile/Async/Statistics 等既有输出能力继续可用，
///     而消费者只需面向 Microsoft.Extensions.Logging 契约。
/// </summary>
/// <remarks>
///     生命周期：provider 由标准 <see cref="ILoggerFactory"/> 持有，工厂释放时调用
///     <see cref="Dispose"/>，转发给实现 <see cref="IDisposable"/> 的 appender。
///     级别过滤：结构性级别过滤由工厂的 filter 规则完成；appender 自身的
///     <see cref="ILogFilter"/> 在输出管线内继续生效。
/// </remarks>
public sealed class AppenderLoggerProvider : ILoggerProvider
{
    /// <summary>按类别缓存的适配记录器；标准工厂同样按类别缓存，这里避免重复分配。</summary>
    private readonly ConcurrentDictionary<string, AppenderLogger> _loggers = new(StringComparer.Ordinal);

    /// <summary>接收所有条目的 appender 集合，顺序即输出顺序。</summary>
    private readonly IReadOnlyList<ILogAppender> _appenders;

    /// <summary>
    ///     创建适配器 provider。
    /// </summary>
    /// <param name="appenders">接收日志条目的 appender 集合；不允许为空。</param>
    public AppenderLoggerProvider(IEnumerable<ILogAppender> appenders)
    {
        ArgumentNullException.ThrowIfNull(appenders);
        var materialized = appenders.ToArray();
        if (materialized.Length == 0)
            throw new ArgumentException("At least one appender is required.", nameof(appenders));
        _appenders = materialized;
    }

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName)
    {
        ArgumentException.ThrowIfNullOrEmpty(categoryName);
        return _loggers.GetOrAdd(categoryName, static (name, provider) => new AppenderLogger(name, provider), this);
    }

    /// <summary>
    ///     释放 provider 并转发给所有实现 <see cref="IDisposable"/> 的 appender。
    ///     单个 appender 释放失败不阻止其余 appender 释放。
    /// </summary>
    public void Dispose()
    {
        foreach (var appender in _appenders)
        {
            if (appender is IDisposable disposable)
            {
                try
                {
                    disposable.Dispose();
                }
                catch (Exception error)
                {
                    // 释放失败只记录到目标输出，不中断其余 appender 的清理。
                    Console.Error.WriteLine($"Log appender disposal failed: {error.Message}");
                }
            }
        }
        _loggers.Clear();
    }

    /// <summary>向全部 appender 追加一个条目；单个 appender 异常不阻断其余输出。</summary>
    private void Append(LogEntry entry)
    {
        foreach (var appender in _appenders)
        {
            try
            {
                appender.Append(entry);
            }
            catch (Exception error)
            {
                // 单个 appender 故障不应让业务日志调用抛出或阻断其它 appender。
                Console.Error.WriteLine($"Log appender '{appender.GetType().Name}' failed: {error.Message}");
            }
        }
    }

    /// <summary>把标准 <see cref="ILogger"/> 调用转换为 <see cref="LogEntry"/> 的适配记录器。</summary>
    private sealed class AppenderLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly AppenderLoggerProvider _provider;

        /// <summary>创建绑定到指定类别与 provider 的适配记录器。</summary>
        public AppenderLogger(string categoryName, AppenderLoggerProvider provider)
        {
            _categoryName = categoryName;
            _provider = provider;
        }

        /// <inheritdoc />
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel) =>
            logLevel != LogLevel.None;

        /// <summary>
        ///     把一次标准日志调用转换为 <see cref="LogEntry"/> 并推入 appender 管线。
        ///     结构化状态为 MEL 的键值列表时转换为属性字典。
        /// </summary>
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);
            if (!IsEnabled(logLevel))
                return;

            var message = formatter(state, exception);
            var properties = ExtractProperties(state);
            _provider.Append(new LogEntry(
                DateTime.Now,
                logLevel,
                _categoryName,
                message,
                exception,
                properties));
        }

        /// <summary>识别标准结构化状态（键值对列表）并提取为属性字典；其余状态返回 null。</summary>
        private static IReadOnlyDictionary<string, object?>? ExtractProperties<TState>(TState state)
        {
            if (state is not IEnumerable<KeyValuePair<string, object?>> pairs)
                return null;
            var dictionary = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var pair in pairs)
            {
                // 标准状态列表中的 OriginalFormat 是消息模板，不作为业务属性输出。
                if (string.Equals(pair.Key, "{OriginalFormat}", StringComparison.Ordinal))
                    continue;
                dictionary[pair.Key] = pair.Value;
            }
            return dictionary;
        }
    }
}
