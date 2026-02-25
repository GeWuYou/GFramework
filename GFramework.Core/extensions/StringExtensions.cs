namespace GFramework.Core.extensions;

/// <summary>
///     字符串扩展方法
/// </summary>
public static class StringExtensions
{
    /// <summary>
    ///     指示指定的字符串是 null 还是空字符串
    /// </summary>
    /// <param name="str">要测试的字符串</param>
    /// <returns>如果 str 参数为 null 或空字符串 ("")，则为 true；否则为 false</returns>
    /// <example>
    /// <code>
    /// string? text = null;
    /// if (text.IsNullOrEmpty()) { /* ... */ }
    /// </code>
    /// </example>
    public static bool IsNullOrEmpty(this string? str)
    {
        return string.IsNullOrEmpty(str);
    }

    /// <summary>
    ///     指示指定的字符串是 null、空还是仅由空白字符组成
    /// </summary>
    /// <param name="str">要测试的字符串</param>
    /// <returns>如果 str 参数为 null、空字符串或仅包含空白字符，则为 true；否则为 false</returns>
    /// <example>
    /// <code>
    /// string? text = "   ";
    /// if (text.IsNullOrWhiteSpace()) { /* ... */ }
    /// </code>
    /// </example>
    public static bool IsNullOrWhiteSpace(this string? str)
    {
        return string.IsNullOrWhiteSpace(str);
    }

    /// <summary>
    ///     如果字符串为空，则返回 null；否则返回原字符串
    /// </summary>
    /// <param name="str">要检查的字符串</param>
    /// <returns>如果字符串为空则返回 null，否则返回原字符串</returns>
    /// <example>
    /// <code>
    /// string text = "";
    /// var result = text.NullIfEmpty(); // 返回 null
    /// </code>
    /// </example>
    public static string? NullIfEmpty(this string? str)
    {
        return string.IsNullOrEmpty(str) ? null : str;
    }

    /// <summary>
    ///     截断字符串到指定的最大长度，并可选地添加后缀
    /// </summary>
    /// <param name="str">要截断的字符串</param>
    /// <param name="maxLength">最大长度（包括后缀）</param>
    /// <param name="suffix">截断时添加的后缀，默认为 "..."</param>
    /// <returns>截断后的字符串</returns>
    /// <exception cref="ArgumentNullException">当 str 为 null 时抛出</exception>
    /// <exception cref="ArgumentOutOfRangeException">当 maxLength 小于后缀长度时抛出</exception>
    /// <example>
    /// <code>
    /// var text = "Hello World";
    /// var truncated = text.Truncate(8); // "Hello..."
    /// </code>
    /// </example>
    public static string Truncate(this string str, int maxLength, string suffix = "...")
    {
        ArgumentNullException.ThrowIfNull(str);
        ArgumentNullException.ThrowIfNull(suffix);

        if (maxLength < suffix.Length)
            throw new ArgumentOutOfRangeException(nameof(maxLength),
                $"最大长度必须至少为后缀长度 ({suffix.Length})");

        if (str.Length <= maxLength)
            return str;

        return string.Concat(str.AsSpan(0, maxLength - suffix.Length), suffix);
    }

    /// <summary>
    ///     使用指定的分隔符连接字符串集合
    /// </summary>
    /// <param name="values">要连接的字符串集合</param>
    /// <param name="separator">分隔符</param>
    /// <returns>连接后的字符串</returns>
    /// <exception cref="ArgumentNullException">当 values 或 separator 为 null 时抛出</exception>
    /// <example>
    /// <code>
    /// var words = new[] { "Hello", "World" };
    /// var result = words.Join(", "); // "Hello, World"
    /// </code>
    /// </example>
    public static string Join(this IEnumerable<string> values, string separator)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(separator);

        return string.Join(separator, values);
    }
}
