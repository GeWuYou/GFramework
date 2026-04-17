namespace GFramework.Cqrs.Internal;

/// <summary>
///     提供基于弱键语义的线程安全缓存。
///     该缓存用于跨容器复用与 <see cref="Assembly" /> 或 <see cref="Type" /> 绑定的派生元数据，
///     同时避免静态强引用阻止 collectible 程序集或热重载类型被卸载。
/// </summary>
/// <typeparam name="TKey">缓存键类型。</typeparam>
/// <typeparam name="TValue">缓存值类型。</typeparam>
/// <remarks>
///     该缓存只保证“命中时复用”，不保证“永久保留”。
///     当键对象被 GC 回收后，条目会自然失效，后续访问会重新计算对应值。
///     这是 CQRS 运行时在卸载安全与热路径性能之间的显式权衡。
/// </remarks>
internal sealed class WeakKeyCache<TKey, TValue>
    where TKey : class
    where TValue : class
{
    private readonly object _gate = new();
    private ConditionalWeakTable<TKey, TValue> _entries = new();

    /// <summary>
    ///     获取指定键对应的缓存值；若当前未命中，则在锁保护下创建并写入。
    /// </summary>
    /// <param name="key">缓存键。</param>
    /// <param name="valueFactory">创建缓存值的工厂方法。</param>
    /// <returns>已存在或新创建的缓存值。</returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="key" /> 或 <paramref name="valueFactory" /> 为 <see langword="null" />。
    ///     或 <paramref name="valueFactory" /> 返回 <see langword="null" />。
    /// </exception>
    public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(valueFactory);

        var entries = Volatile.Read(ref _entries);
        if (entries.TryGetValue(key, out var cachedValue))
            return cachedValue;

        lock (_gate)
        {
            entries = _entries;
            if (entries.TryGetValue(key, out cachedValue))
                return cachedValue;

            var createdValue = valueFactory(key);
            ArgumentNullException.ThrowIfNull(createdValue);
            entries.Add(key, createdValue);
            return createdValue;
        }
    }

    /// <summary>
    ///     获取指定键对应的缓存值；若当前未命中，则在锁保护下使用附加状态创建并写入。
    /// </summary>
    /// <typeparam name="TState">创建缓存值时需要携带的附加状态类型。</typeparam>
    /// <param name="key">缓存键。</param>
    /// <param name="state">创建缓存值时复用的附加状态。</param>
    /// <param name="valueFactory">基于键与附加状态创建缓存值的工厂方法。</param>
    /// <returns>已存在或新创建的缓存值。</returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="key" /> 或 <paramref name="valueFactory" /> 为 <see langword="null" />。
    ///     或 <paramref name="valueFactory" /> 返回 <see langword="null" />。
    /// </exception>
    public TValue GetOrAdd<TState>(TKey key, TState state, Func<TKey, TState, TValue> valueFactory)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(valueFactory);

        var entries = Volatile.Read(ref _entries);
        if (entries.TryGetValue(key, out var cachedValue))
            return cachedValue;

        lock (_gate)
        {
            entries = _entries;
            if (entries.TryGetValue(key, out cachedValue))
                return cachedValue;

            var createdValue = valueFactory(key, state);
            ArgumentNullException.ThrowIfNull(createdValue);
            entries.Add(key, createdValue);
            return createdValue;
        }
    }

    /// <summary>
    ///     尝试读取当前缓存中的值，而不触发新的创建逻辑。
    /// </summary>
    /// <param name="key">缓存键。</param>
    /// <param name="value">命中时返回的缓存值。</param>
    /// <returns>若命中当前缓存则为 <see langword="true" />；否则为 <see langword="false" />。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> 为 <see langword="null" />。</exception>
    public bool TryGetValue(TKey key, out TValue? value)
    {
        ArgumentNullException.ThrowIfNull(key);
        return Volatile.Read(ref _entries).TryGetValue(key, out value);
    }

    /// <summary>
    ///     清空当前缓存实例。
    /// </summary>
    /// <remarks>
    ///     该方法主要服务于测试，便于在同一进程内隔离不同用例的静态缓存状态。
    /// </remarks>
    public void Clear()
    {
        lock (_gate)
        {
            _entries = new ConditionalWeakTable<TKey, TValue>();
        }
    }

    /// <summary>
    ///     返回指定键当前命中的缓存对象；若未命中则返回 <see langword="null" />。
    /// </summary>
    /// <param name="key">缓存键。</param>
    /// <returns>当前缓存对象，或 <see langword="null" />。</returns>
    /// <remarks>
    ///     该入口仅用于测试通过反射观察缓存状态，不应用于运行时代码路径。
    /// </remarks>
    public TValue? GetValueOrDefaultForTesting(TKey key)
    {
        return TryGetValue(key, out var value) ? value : null;
    }
}

/// <summary>
///     提供以两段 <see cref="Type" /> 为键的弱引用缓存。
///     适用于请求/响应或流请求/响应这类组合类型元数据的复用场景。
/// </summary>
/// <typeparam name="TValue">缓存值类型。</typeparam>
/// <remarks>
///     第一层和第二层键都使用弱键缓存，因此只要任一类型不再被外部引用，
///     对应条目都允许被 GC 清理，并在后续首次访问时重新建立。
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
    public TValue GetOrAdd(Type primaryType, Type secondaryType, Func<Type, Type, TValue> valueFactory)
    {
        ArgumentNullException.ThrowIfNull(primaryType);
        ArgumentNullException.ThrowIfNull(secondaryType);
        ArgumentNullException.ThrowIfNull(valueFactory);

        var secondaryEntries = _entries.GetOrAdd(primaryType, static _ => new WeakKeyCache<Type, TValue>());
        return secondaryEntries.GetOrAdd(
            secondaryType,
            (PrimaryType: primaryType, Factory: valueFactory),
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
    /// <remarks>
    ///     该入口仅用于测试通过反射观察缓存状态，不应用于运行时代码路径。
    /// </remarks>
    public TValue? GetValueOrDefaultForTesting(Type primaryType, Type secondaryType)
    {
        return TryGetValue(primaryType, secondaryType, out var value) ? value : null;
    }
}
