// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using System.Text;

namespace GFramework.Core.Pool;

/// <summary>
///     StringBuilder 对象池，提供高性能的字符串构建器复用
/// </summary>
public static class StringBuilderPool
{
    private const int DefaultCapacity = 256;
    private const int MaxRetainedCapacity = 4096;

    // 使用 ConcurrentBag 实现线程安全的对象池
    private static readonly ConcurrentBag<StringBuilder> Pool = new();

    /// <summary>
    ///     从池中租用一个 StringBuilder
    /// </summary>
    /// <param name="capacity">初始容量，默认为 256</param>
    /// <returns>StringBuilder 实例</returns>
    /// <remarks>
    ///     优先从池中获取可复用的实例，如果池为空则创建新实例。
    ///     使用完毕后应调用 <see cref="Return"/> 方法归还到池中以便复用。
    /// </remarks>
    /// <example>
    /// <code>
    /// var sb = StringBuilderPool.Rent();
    /// try
    /// {
    ///     sb.Append("Hello");
    ///     sb.Append(" World");
    ///     return sb.ToString();
    /// }
    /// finally
    /// {
    ///     StringBuilderPool.Return(sb);
    /// }
    /// </code>
    /// </example>
    public static StringBuilder Rent(int capacity = DefaultCapacity)
    {
        if (Pool.TryTake(out var sb))
        {
            // 从池中获取到实例，确保容量满足需求
            if (sb.Capacity < capacity)
            {
                sb.Capacity = capacity;
            }

            return sb;
        }

        // 池为空，创建新实例
        return new StringBuilder(capacity);
    }

    /// <summary>
    ///     将 StringBuilder 归还到池中
    /// </summary>
    /// <param name="builder">要归还的 StringBuilder</param>
    /// <remarks>
    ///     如果 StringBuilder 的容量超过 <see cref="MaxRetainedCapacity"/>，
    ///     则不会放回池中，而是直接丢弃以避免保留过大的对象。
    /// </remarks>
    /// <example>
    /// <code>
    /// var sb = StringBuilderPool.Rent();
    /// try
    /// {
    ///     sb.Append("Hello World");
    ///     Console.WriteLine(sb.ToString());
    /// }
    /// finally
    /// {
    ///     StringBuilderPool.Return(sb);
    /// }
    /// </code>
    /// </example>
    public static void Return(StringBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // 如果容量过大，不放回池中
        if (builder.Capacity > MaxRetainedCapacity)
        {
            return;
        }

        // 清空内容并放回池中
        builder.Clear();
        Pool.Add(builder);
    }

    /// <summary>
    ///     获取一个 StringBuilder，使用完后自动归还
    /// </summary>
    /// <param name="capacity">初始容量</param>
    /// <returns>可自动释放的 StringBuilder 包装器</returns>
    /// <example>
    /// <code>
    /// using var sb = StringBuilderPool.GetScoped();
    /// sb.Value.Append("Hello");
    /// sb.Value.Append(" World");
    /// return sb.Value.ToString();
    /// </code>
    /// </example>
    public static ScopedStringBuilder GetScoped(int capacity = DefaultCapacity)
    {
        return new ScopedStringBuilder(Rent(capacity));
    }

    /// <summary>
    ///     可自动释放的 StringBuilder 包装器
    /// </summary>
    public readonly struct ScopedStringBuilder : IDisposable
    {
        /// <summary>
        ///     获取 StringBuilder 实例
        /// </summary>
        public StringBuilder Value { get; }

        internal ScopedStringBuilder(StringBuilder value)
        {
            Value = value;
        }

        /// <summary>
        ///     释放 StringBuilder 并归还到池中
        /// </summary>
        public void Dispose()
        {
            Return(Value);
        }
    }
}