namespace GFramework.Core.extensions;

/// <summary>
///     Span 和 ReadOnlySpan 扩展方法，提供零分配的高性能操作
/// </summary>
public static class SpanExtensions
{
    /// <summary>
    ///     尝试将字符 span 解析为指定类型
    /// </summary>
    /// <typeparam name="T">目标类型，必须实现 ISpanParsable 接口</typeparam>
    /// <param name="span">要解析的字符 span</param>
    /// <param name="result">解析结果，失败时为 default(T)</param>
    /// <returns>如果解析成功返回 true，否则返回 false</returns>
    /// <example>
    /// <code>
    /// ReadOnlySpan&lt;char&gt; span = "123";
    /// if (span.TryParseValue&lt;int&gt;(out var result))
    /// {
    ///     Console.WriteLine(result); // 123
    /// }
    /// </code>
    /// </example>
    public static bool TryParseValue<T>(this ReadOnlySpan<char> span, out T result) where T : ISpanParsable<T>
    {
        return T.TryParse(span, null, out result);
    }

    /// <summary>
    ///     计算 span 中指定值出现的次数
    /// </summary>
    /// <typeparam name="T">元素类型，必须实现 IEquatable 接口</typeparam>
    /// <param name="span">要搜索的 span</param>
    /// <param name="value">要计数的值</param>
    /// <returns>值出现的次数</returns>
    /// <example>
    /// <code>
    /// ReadOnlySpan&lt;int&gt; span = stackalloc int[] { 1, 2, 3, 2, 1 };
    /// var count = span.CountOccurrences(2); // 2
    /// </code>
    /// </example>
    public static int CountOccurrences<T>(this ReadOnlySpan<T> span, T value) where T : IEquatable<T>
    {
        var count = 0;
        foreach (var item in span)
        {
            if (item.Equals(value))
                count++;
        }

        return count;
    }
}