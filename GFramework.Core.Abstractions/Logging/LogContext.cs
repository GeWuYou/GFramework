// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.Logging;

/// <summary>
///     日志上下文，用于在异步流中传递结构化属性
/// </summary>
public sealed class LogContext : IDisposable
{
    private static readonly AsyncLocal<Dictionary<string, object?>?> _context = new();
    private readonly bool _hadPreviousValue;
    private readonly string _key;
    private readonly object? _previousValue;

    private LogContext(string key, object? value)
    {
        _key = key;

        var current = _context.Value;
        if (current?.TryGetValue(key, out var prev) == true)
        {
            _previousValue = prev;
            _hadPreviousValue = true;
        }

        EnsureContext();
        _context.Value![key] = value;
    }

    /// <summary>
    ///     获取当前上下文中的所有属性
    /// </summary>
    public static IReadOnlyDictionary<string, object?> Current
    {
        get
        {
            var context = _context.Value;
            return context ??
                   (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>(StringComparer.Ordinal);
        }
    }

    /// <summary>
    ///     释放上下文，恢复之前的值
    /// </summary>
    public void Dispose()
    {
        var current = _context.Value;
        if (current == null) return;

        if (_hadPreviousValue)
        {
            current[_key] = _previousValue;
        }
        else
        {
            current.Remove(_key);
            if (current.Count == 0)
            {
                _context.Value = null;
            }
        }
    }

    /// <summary>
    ///     向当前上下文添加一个属性
    /// </summary>
    /// <param name="key">属性键</param>
    /// <param name="value">属性值</param>
    /// <returns>可释放的上下文对象，释放时会恢复之前的值</returns>
    public static IDisposable Push(string key, object? value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));

        return new LogContext(key, value);
    }

    /// <summary>
    ///     向当前上下文添加多个属性
    /// </summary>
    /// <param name="properties">属性键值对</param>
    /// <returns>可释放的上下文对象，释放时会恢复之前的值</returns>
    public static IDisposable PushProperties(params (string Key, object? Value)[] properties)
    {
        if (properties == null || properties.Length == 0)
            throw new ArgumentException("Properties cannot be null or empty.", nameof(properties));

        return new CompositeDisposable(properties.Select(p => Push(p.Key, p.Value)).ToArray());
    }

    /// <summary>
    ///     清除当前上下文中的所有属性
    /// </summary>
    public static void Clear()
    {
        _context.Value = null;
    }

    private static void EnsureContext()
    {
        _context.Value ??= new Dictionary<string, object?>(StringComparer.Ordinal);
    }

    /// <summary>
    ///     组合多个可释放对象
    /// </summary>
    private sealed class CompositeDisposable(IDisposable[] disposables) : IDisposable
    {
        public void Dispose()
        {
            // 按相反顺序释放
            for (int i = disposables.Length - 1; i >= 0; i--)
            {
                disposables[i].Dispose();
            }
        }
    }
}