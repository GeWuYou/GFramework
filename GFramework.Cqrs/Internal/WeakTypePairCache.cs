namespace GFramework.Cqrs.Internal;

/// <summary>
///     提供以两段 <see cref="Type" /> 为键的弱引用缓存。
///     适用于请求/响应或流请求/响应这类组合类型元数据的复用场景。
/// </summary>
/// <typeparam name="TValue">缓存值类型。</typeparam>
/// <remarks>
///     第一层和第二层键都使用弱键缓存，因此只要任一类型不再被外部引用，
///     对应条目都允许被 GC 清理，并在后续首次访问时重新建立。
///     线程安全：该类型支持并发访问，读写与清理由底层弱键缓存实现保证一致性。
///     失败模式：键被 GC 回收或调用 <see cref="Clear" /> 后，后续读取可能未命中并触发重建。
/// </remarks>
internal sealed class WeakTypePairCache<TValue>
    where TValue : class
{
    private readonly WeakKeyCache<Type, WeakKeyCache<Type, TValue>> _entries = new();

    /// <summary>
    ///     获取指定类型对对应的缓存值；若未命中则创建并写入。
    /// </summary>
    /// <param name="primaryType">第一段类型键。</param>
    /// <param name="secondaryType">第二段类型键。</param>
    /// <param name="valueFactory">创建缓存值的工厂方法。</param>
    /// <returns>已存在或新创建的缓存值。</returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="primaryType" />、<paramref name="secondaryType" /> 或
    ///     <paramref name="valueFactory" /> 为 <see langword="null" />。
    /// </exception>
    /// <exception cref="InvalidOperationException"><paramref name="valueFactory" /> 返回 <see langword="null" />。</exception>
    public TValue GetOrAdd(Type primaryType, Type secondaryType, Func<Type, Type, TValue> valueFactory)
    {
        ArgumentNullException.ThrowIfNull(primaryType);
        ArgumentNullException.ThrowIfNull(secondaryType);
        ArgumentNullException.ThrowIfNull(valueFactory);

        // 第一层按 primaryType 定位或创建二级缓存，避免每次命中都重新分配容器。
        var secondaryEntries = _entries.GetOrAdd(primaryType, static _ => new WeakKeyCache<Type, TValue>());
        return secondaryEntries.GetOrAdd(
            secondaryType,
            (PrimaryType: primaryType, Factory: valueFactory),
            // 使用 static lambda + state 传参，避免热路径上的闭包捕获与额外分配。
            static (cachedSecondaryType, state) => state.Factory(state.PrimaryType, cachedSecondaryType));
    }

    /// <summary>
    ///     尝试读取指定类型对的缓存值，而不触发新的创建逻辑。
    /// </summary>
    /// <param name="primaryType">第一段类型键。</param>
    /// <param name="secondaryType">第二段类型键。</param>
    /// <param name="value">命中时返回的缓存值。</param>
    /// <returns>若命中当前缓存则为 <see langword="true" />；否则为 <see langword="false" />。</returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="primaryType" /> 或 <paramref name="secondaryType" /> 为 <see langword="null" />。
    /// </exception>
    public bool TryGetValue(Type primaryType, Type secondaryType, out TValue? value)
    {
        ArgumentNullException.ThrowIfNull(primaryType);
        ArgumentNullException.ThrowIfNull(secondaryType);

        if (_entries.TryGetValue(primaryType, out var secondaryEntries) &&
            secondaryEntries is not null)
            return secondaryEntries.TryGetValue(secondaryType, out value);

        value = null;
        return false;
    }

    /// <summary>
    ///     清空当前缓存实例。
    /// </summary>
    /// <remarks>
    ///     该方法主要服务于测试，避免同一进程里的静态缓存污染后续断言。
    /// </remarks>
    public void Clear()
    {
        _entries.Clear();
    }

    /// <summary>
    ///     返回指定类型对当前命中的缓存对象；若未命中则返回 <see langword="null" />。
    /// </summary>
    /// <param name="primaryType">第一段类型键。</param>
    /// <param name="secondaryType">第二段类型键。</param>
    /// <returns>当前缓存对象，或 <see langword="null" />。</returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="primaryType" /> 或 <paramref name="secondaryType" /> 为 <see langword="null" />。
    /// </exception>
    /// <remarks>
    ///     该入口仅用于测试通过反射观察缓存状态，不应用于运行时代码路径。
    /// </remarks>
    public TValue? GetValueOrDefaultForTesting(Type primaryType, Type secondaryType)
    {
        return TryGetValue(primaryType, secondaryType, out var value) ? value : null;
    }
}
