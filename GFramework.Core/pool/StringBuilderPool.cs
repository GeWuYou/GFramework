using System.Text;

namespace GFramework.Core.pool;

/// <summary>
///     StringBuilder 对象池，提供高性能的字符串构建器复用
/// </summary>
public static class StringBuilderPool
{
    private const int DefaultCapacity = 256;
    private const int MaxRetainedCapacity = 4096;

    /// <summary>
    ///     从池中租用一个 StringBuilder
    /// </summary>
    /// <param name="capacity">初始容量，默认为 256</param>
    /// <returns>StringBuilder 实例</returns>
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
        var sb = new StringBuilder(capacity);
        return sb;
    }

    /// <summary>
    ///     将 StringBuilder 归还到池中
    /// </summary>
    /// <param name="builder">要归还的 StringBuilder</param>
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

        builder.Clear();
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
