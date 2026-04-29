namespace GFramework.Cqrs;

/// <summary>
///     标记程序集中的 CQRS 生成注册器仍需要运行时补充反射扫描。
/// </summary>
/// <remarks>
///     该特性通常由源码生成器自动添加到消费端程序集。
///     当生成器只能安全生成部分 handler 映射时，运行时会先执行生成注册器，再补一次带去重的反射扫描，
///     以覆盖那些生成代码无法直接引用的 handler 类型。
///     允许同一程序集声明多个该特性实例，以便生成器把“可直接引用的 fallback handlers”
///     和“仍需按名称恢复的 fallback handlers”拆成独立元数据块，进一步减少运行时字符串查找成本。
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class CqrsReflectionFallbackAttribute : Attribute
{
    /// <summary>
    ///     初始化 <see cref="CqrsReflectionFallbackAttribute" />，保留旧版“仅标记需要补扫”的语义。
    /// </summary>
    public CqrsReflectionFallbackAttribute()
    {
        FallbackHandlerTypeNames = [];
        FallbackHandlerTypes = [];
    }

    /// <summary>
    ///     初始化 <see cref="CqrsReflectionFallbackAttribute" />。
    /// </summary>
    /// <param name="fallbackHandlerTypeNames">
    ///     需要运行时补充反射注册的处理器类型全名。
    ///     当该清单为空时，运行时会回退到整程序集扫描，以兼容旧版 marker 语义。
    /// </param>
    public CqrsReflectionFallbackAttribute(params string[] fallbackHandlerTypeNames)
    {
        ArgumentNullException.ThrowIfNull(fallbackHandlerTypeNames);

        FallbackHandlerTypeNames = fallbackHandlerTypeNames
            .Where(static typeName => !string.IsNullOrWhiteSpace(typeName))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static typeName => typeName, StringComparer.Ordinal)
            .ToArray();
        FallbackHandlerTypes = [];
    }

    /// <summary>
    ///     初始化 <see cref="CqrsReflectionFallbackAttribute" />。
    /// </summary>
    /// <param name="fallbackHandlerTypes">
    ///     需要运行时补充反射注册的处理器类型。
    ///     该重载适合手写或第三方程序集显式声明可直接引用的 fallback handlers，
    ///     避免再通过字符串名称回查程序集元数据。
    /// </param>
    public CqrsReflectionFallbackAttribute(params Type[] fallbackHandlerTypes)
    {
        ArgumentNullException.ThrowIfNull(fallbackHandlerTypes);

        FallbackHandlerTypeNames = [];
        FallbackHandlerTypes = fallbackHandlerTypes
            .Where(static type => type is not null)
            .Distinct()
            .OrderBy(static type => type.FullName ?? type.Name, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    ///     获取需要运行时补充反射注册的处理器类型全名集合。
    /// </summary>
    public IReadOnlyList<string> FallbackHandlerTypeNames { get; }

    /// <summary>
    ///     获取可直接供运行时补充反射注册的处理器类型集合。
    /// </summary>
    public IReadOnlyList<Type> FallbackHandlerTypes { get; }
}
