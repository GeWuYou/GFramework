namespace GFramework.Core.extensions;

/// <summary>
///     集合扩展方法
/// </summary>
public static class CollectionExtensions
{
    /// <summary>
    ///     对集合中的每个元素执行指定操作
    /// </summary>
    /// <typeparam name="T">集合元素类型</typeparam>
    /// <param name="source">源集合</param>
    /// <param name="action">要对每个元素执行的操作</param>
    /// <exception cref="ArgumentNullException">当 source 或 action 为 null 时抛出</exception>
    /// <example>
    /// <code>
    /// var numbers = new[] { 1, 2, 3 };
    /// numbers.ForEach(n => Console.WriteLine(n));
    /// </code>
    /// </example>
    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(action);

        foreach (var item in source) action(item);
    }

    /// <summary>
    ///     检查集合是否为 null 或空
    /// </summary>
    /// <typeparam name="T">集合元素类型</typeparam>
    /// <param name="source">要检查的集合</param>
    /// <returns>如果集合为 null 或不包含任何元素，则返回 true；否则返回 false</returns>
    /// <example>
    /// <code>
    /// List&lt;int&gt;? numbers = null;
    /// if (numbers.IsNullOrEmpty()) { /* ... */ }
    /// </code>
    /// </example>
    public static bool IsNullOrEmpty<T>(this IEnumerable<T>? source)
    {
        return source is null || !source.Any();
    }

    /// <summary>
    ///     过滤掉集合中的 null 元素
    /// </summary>
    /// <typeparam name="T">集合元素类型（引用类型）</typeparam>
    /// <param name="source">源集合</param>
    /// <returns>不包含 null 元素的集合</returns>
    /// <exception cref="ArgumentNullException">当 source 为 null 时抛出</exception>
    /// <example>
    /// <code>
    /// var items = new string?[] { "a", null, "b", null, "c" };
    /// var nonNull = items.WhereNotNull(); // ["a", "b", "c"]
    /// </code>
    /// </example>
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source) where T : class
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Where(item => item is not null)!;
    }

    /// <summary>
    ///     将集合转换为字典，如果存在重复键则使用最后一个值
    /// </summary>
    /// <typeparam name="T">源集合元素类型</typeparam>
    /// <typeparam name="TKey">字典键类型</typeparam>
    /// <typeparam name="TValue">字典值类型</typeparam>
    /// <param name="source">源集合</param>
    /// <param name="keySelector">键选择器函数</param>
    /// <param name="valueSelector">值选择器函数</param>
    /// <returns>转换后的字典</returns>
    /// <exception cref="ArgumentNullException">当 source、keySelector 或 valueSelector 为 null 时抛出</exception>
    /// <example>
    /// <code>
    /// var items = new[] { ("a", 1), ("b", 2), ("a", 3) };
    /// var dict = items.ToDictionarySafe(x => x.Item1, x => x.Item2);
    /// // dict["a"] == 3 (最后一个值)
    /// </code>
    /// </example>
    public static Dictionary<TKey, TValue> ToDictionarySafe<T, TKey, TValue>(
        this IEnumerable<T> source,
        Func<T, TKey> keySelector,
        Func<T, TValue> valueSelector) where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keySelector);
        ArgumentNullException.ThrowIfNull(valueSelector);

        var dictionary = new Dictionary<TKey, TValue>();

        foreach (var item in source)
        {
            var key = keySelector(item);
            var value = valueSelector(item);
            dictionary[key] = value; // 覆盖重复键
        }

        return dictionary;
    }
}
