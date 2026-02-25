using System.Runtime.CompilerServices;

namespace GFramework.Core.extensions;

/// <summary>
///     参数验证扩展方法（Guard 模式）
/// </summary>
public static class GuardExtensions
{
    /// <summary>
    ///     如果值为 null 则抛出 ArgumentNullException
    /// </summary>
    /// <typeparam name="T">引用类型</typeparam>
    /// <param name="value">要检查的值</param>
    /// <param name="paramName">参数名称（自动捕获）</param>
    /// <returns>非 null 的值</returns>
    /// <exception cref="ArgumentNullException">当 value 为 null 时抛出</exception>
    /// <example>
    /// <code>
    /// public void Process(string? input)
    /// {
    ///     var safeInput = input.ThrowIfNull(); // 自动使用 "input" 作为参数名
    /// }
    /// </code>
    /// </example>
    public static T ThrowIfNull<T>(
        this T? value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null) where T : class
    {
        ArgumentNullException.ThrowIfNull(value, paramName);
        return value;
    }

    /// <summary>
    ///     如果字符串为 null 或空则抛出 ArgumentException
    /// </summary>
    /// <param name="value">要检查的字符串</param>
    /// <param name="paramName">参数名称（自动捕获）</param>
    /// <returns>非空字符串</returns>
    /// <exception cref="ArgumentNullException">当 value 为 null 时抛出</exception>
    /// <exception cref="ArgumentException">当 value 为空字符串时抛出</exception>
    /// <example>
    /// <code>
    /// public void SetName(string? name)
    /// {
    ///     var safeName = name.ThrowIfNullOrEmpty();
    /// }
    /// </code>
    /// </example>
    public static string ThrowIfNullOrEmpty(
        this string? value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        ArgumentNullException.ThrowIfNull(value, paramName);

        if (value.Length == 0)
            throw new ArgumentException("字符串不能为空", paramName);

        return value;
    }

    /// <summary>
    ///     如果字符串为 null、空或仅包含空白字符则抛出 ArgumentException
    /// </summary>
    /// <param name="value">要检查的字符串</param>
    /// <param name="paramName">参数名称（自动捕获）</param>
    /// <returns>非空白字符串</returns>
    /// <exception cref="ArgumentNullException">当 value 为 null 时抛出</exception>
    /// <exception cref="ArgumentException">当 value 为空或仅包含空白字符时抛出</exception>
    /// <example>
    /// <code>
    /// public void SetDescription(string? description)
    /// {
    ///     var safeDescription = description.ThrowIfNullOrWhiteSpace();
    /// }
    /// </code>
    /// </example>
    public static string ThrowIfNullOrWhiteSpace(
        this string? value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        ArgumentNullException.ThrowIfNull(value, paramName);

        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("字符串不能为空或仅包含空白字符", paramName);

        return value;
    }

    /// <summary>
    ///     如果集合为空则抛出 ArgumentException
    /// </summary>
    /// <typeparam name="T">集合元素类型</typeparam>
    /// <param name="source">要检查的集合</param>
    /// <param name="paramName">参数名称（自动捕获）</param>
    /// <returns>非空集合</returns>
    /// <exception cref="ArgumentNullException">当 source 为 null 时抛出</exception>
    /// <exception cref="ArgumentException">当 source 为空集合时抛出</exception>
    /// <example>
    /// <code>
    /// public void ProcessItems(IEnumerable&lt;int&gt;? items)
    /// {
    ///     var safeItems = items.ThrowIfEmpty();
    /// }
    /// </code>
    /// </example>
    public static IEnumerable<T> ThrowIfEmpty<T>(
        this IEnumerable<T>? source,
        [CallerArgumentExpression(nameof(source))] string? paramName = null)
    {
        ArgumentNullException.ThrowIfNull(source, paramName);

        if (!source.Any())
            throw new ArgumentException("集合不能为空", paramName);

        return source;
    }
}
