// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using GFramework.Core.Abstractions.Logging;
using Microsoft.Extensions.Logging;

namespace GFramework.Godot.Logging;

/// <summary>
///     标准 Microsoft.Extensions.Logging provider，把标准日志调用渲染到 Godot 输出 API。
/// </summary>
/// <remarks>
///     <para>
///         provider 按类别缓存 <see cref="ILogger"/> 实例；每条日志先经过 <see cref="GodotLoggerSettings"/>
///         的类别级别（精确匹配、点号前缀匹配、默认级别合并取严）门禁，再由共享的
///         <see cref="GodotLogAppender"/> 按当前模式（Debug/Release）、模板与颜色渲染，
///         通过 GD.PrintRich/GD.Print/GD.PushError/GD.PushWarning 输出。
///     </para>
///     <para>
///         宿主通过 <c>LoggerFactory.Create(builder =&gt; builder.AddProvider(provider))</c> 挂接；
///         非依赖注入宿主可直接调用 <see cref="CreateLogger(string)"/>。注意：宿主工厂自身的过滤规则
///         （例如默认 Information 下限）叠加在本 provider 的类别门禁之上。
///     </para>
///     <para>
///         生命周期：provider 由标准 <see cref="ILoggerFactory"/> 拥有并在工厂释放时调用
///         <see cref="Dispose"/>。释放后不再创建新记录器；已存在的记录器实例继续以最后一次发布的
///         设置快照输出，因此共享宿主在关闭时不会因为残余日志调用而抛出异常。
///     </para>
/// </remarks>
public sealed class GodotLogProvider : ILoggerProvider
{
    /// <summary>按类别缓存的记录器；同一类别复用同一实例以观察热重载后的设置。</summary>
    private readonly ConcurrentDictionary<string, GodotLogger> _loggers = new(StringComparer.Ordinal);

    /// <summary>提供最新设置快照的访问器；读取是线程安全的。</summary>
    private readonly Func<GodotLoggerSettings> _settingsProvider;

    /// <summary>共享的输出 appender，按当前设置快照渲染每条日志。</summary>
    private readonly GodotLogAppender _appender;

    /// <summary>是否已释放；仅阻止新的记录器创建，不影响既有记录器输出。</summary>
    private volatile bool _disposed;

    /// <summary>
    ///     Initializes a provider with the default settings snapshot.
    /// </summary>
    public GodotLogProvider()
        : this(static () => GodotLoggerSettings.Default)
    {
    }

    /// <summary>
    ///     Initializes a provider with fixed Godot logger options.
    /// </summary>
    /// <param name="options">The formatting and routing options used for every rendered entry.</param>
    public GodotLogProvider(GodotLoggerOptions options)
        : this(CreateStaticSettingsProvider(options))
    {
    }

    /// <summary>
    ///     Initializes a provider with a dynamic settings provider.
    /// </summary>
    /// <param name="settingsProvider">
    ///     Provides the latest settings snapshot for each level check or render operation.
    /// </param>
    /// <remarks>
    ///     The composition entry <see cref="GodotLog"/> uses this constructor so cached loggers observe
    ///     hot-reloaded settings without being recreated. The provider must be fast and thread-safe because
    ///     it is called on the logging path.
    /// </remarks>
    internal GodotLogProvider(Func<GodotLoggerSettings> settingsProvider)
    {
        _settingsProvider = settingsProvider ?? throw new ArgumentNullException(nameof(settingsProvider));
        _appender = new GodotLogAppender(() => _settingsProvider().Options);
    }

    /// <summary>
    ///     Gets or sets the provider-level minimum level floor.
    /// </summary>
    /// <remarks>
    ///     The floor participates in the merged-max level calculation together with option floors and
    ///     category overrides; configuration can only make categories stricter, never more verbose.
    /// </remarks>
    public LogLevel MinLevel { get; set; }

    /// <inheritdoc />
    /// <exception cref="ObjectDisposedException">Thrown when the provider has been disposed.</exception>
    public ILogger CreateLogger(string categoryName)
    {
        ArgumentException.ThrowIfNullOrEmpty(categoryName);
        ObjectDisposedException.ThrowIf(_disposed, this);

        return _loggers.GetOrAdd(
            categoryName,
            static (loggerName, provider) => new GodotLogger(loggerName, provider),
            this);
    }

    /// <summary>
    ///     Releases the provider and stops creating new loggers.
    /// </summary>
    /// <remarks>
    ///     Existing logger instances intentionally keep working with the last published settings snapshot,
    ///     matching Godot hosts that keep process-wide logging for the whole game lifetime. The provider does
    ///     not own the appender's Godot resources, so no further disposal work is required.
    /// </remarks>
    public void Dispose()
    {
        _disposed = true;
        _loggers.Clear();
    }

    /// <summary>
    ///     Appends one rendered log entry to Godot's console and debugger output.
    /// </summary>
    /// <param name="entry">The log entry prepared by a cached logger.</param>
    internal void Append(LogEntry entry)
    {
        _appender.Append(entry);
    }

    /// <summary>
    ///     Calculates the effective minimum level for a category from the current settings snapshot.
    /// </summary>
    /// <param name="categoryName">The logger category name.</param>
    /// <returns>The strictest level selected from options, provider floor, and category configuration.</returns>
    internal LogLevel GetEffectiveMinLevel(string categoryName)
    {
        return _settingsProvider().GetEffectiveMinLevel(categoryName, MinLevel);
    }

    /// <summary>
    ///     Normalizes caller-provided structured properties for rendering.
    /// </summary>
    /// <param name="properties">The structured properties of one standard log call.</param>
    /// <returns>
    ///     A dictionary with blank keys removed and valid keys trimmed, or null when no valid property exists.
    /// </returns>
    /// <remarks>
    ///     The standard <c>{OriginalFormat}</c> message-template marker is excluded because it is not a
    ///     business property. Blank keys are skipped because they cannot produce useful structured output;
    ///     the returned dictionary is detached from the caller's state.
    /// </remarks>
    internal static IReadOnlyDictionary<string, object?>? NormalizeProperties(
        IEnumerable<KeyValuePair<string, object?>>? properties)
    {
        if (properties == null)
        {
            return null;
        }

        Dictionary<string, object?>? result = null;
        foreach (var pair in properties)
        {
            if (string.Equals(pair.Key, "{OriginalFormat}", StringComparison.Ordinal))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(pair.Key))
            {
                continue;
            }

            result ??= new Dictionary<string, object?>(StringComparer.Ordinal);
            result[pair.Key.Trim()] = pair.Value;
        }

        return result;
    }

    private static Func<GodotLoggerSettings> CreateStaticSettingsProvider(GodotLoggerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var settings = GodotLoggerSettings.FromOptions(options);
        return () => settings;
    }

    /// <summary>
    ///     标准 <see cref="ILogger"/> 实现，把一次标准日志调用转换为 <see cref="LogEntry"/> 并交给 provider 渲染。
    /// </summary>
    /// <remarks>
    ///     记录器实例由 provider 按类别缓存，因此级别检查与渲染都通过 provider 读取最新设置快照，
    ///     热重载后无需重建记录器。
    /// </remarks>
    internal sealed class GodotLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly GodotLogProvider _provider;

        /// <summary>
        ///     Creates a logger bound to the specified category and provider.
        /// </summary>
        /// <param name="categoryName">The category name used for level gating and rendered output.</param>
        /// <param name="provider">The owning provider that renders log entries.</param>
        public GodotLogger(string categoryName, GodotLogProvider provider)
        {
            _categoryName = categoryName;
            _provider = provider;
        }

        /// <inheritdoc />
        /// <remarks>Godot 输出不支持作用域；作用域状态被忽略。</remarks>
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        /// <inheritdoc />
        /// <remarks>
        ///     按 <see cref="GodotLoggerSettings"/> 的合并最大级别判断；<see cref="LogLevel.None"/> 永远关闭。
        /// </remarks>
        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None && logLevel >= _provider.GetEffectiveMinLevel(_categoryName);
        }

        /// <summary>
        ///     Converts one standard log call into a <see cref="LogEntry"/> and pushes it to the provider.
        /// </summary>
        /// <typeparam name="TState">The state type supplied by the standard logging pipeline.</typeparam>
        /// <param name="logLevel">The entry level.</param>
        /// <param name="eventId">The event identifier; Godot rendering ignores it.</param>
        /// <param name="state">The state containing the message template and structured properties.</param>
        /// <param name="exception">The optional exception.</param>
        /// <param name="formatter">The standard message formatter.</param>
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var properties = state is IEnumerable<KeyValuePair<string, object?>> pairs
                ? NormalizeProperties(pairs)
                : null;
            _provider.Append(new LogEntry(
                DateTime.UtcNow,
                logLevel,
                _categoryName,
                formatter(state, exception),
                exception,
                properties));
        }
    }
}
