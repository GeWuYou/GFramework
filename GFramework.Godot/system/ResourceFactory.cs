namespace GFramework.Godot.system;

/// <summary>
/// 资源工厂类，用于注册和解析各种资源的创建工厂
/// </summary>
public static class ResourceFactory
{
    /// <summary>
    /// 可预加载条目接口，定义了是否需要预加载以及执行工厂的方法
    /// </summary>
    private interface IPreloadableEntry
    {
        /// <summary>
        /// 获取一个值，表示该资源是否需要预加载
        /// </summary>
        bool Preload { get; }

        /// <summary>
        /// 执行与该条目关联的工厂方法
        /// </summary>
        void ExecuteFactory();
        
        /// <summary>
        /// 获取资源类型
        /// </summary>
        Type ResourceType { get; }
        
        /// <summary>
        /// 获取资源键值
        /// </summary>
        string Key { get; }
    }


    /// <summary>
    /// 表示一个具体的资源工厂条目，实现 IPreloadableEntry 接口
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    private sealed class Entry<T>(string key, Func<T> factory, bool preload) : IPreloadableEntry
    {
        /// <summary>
        /// 获取用于创建资源的工厂函数
        /// </summary>
        public Func<T> Factory { get; } = factory;

        /// <summary>
        /// 获取一个值，表示该资源是否需要预加载
        /// </summary>
        public bool Preload { get; } = preload;

        /// <summary>
        /// 执行工厂函数以创建资源实例
        /// </summary>
        public void ExecuteFactory() => Factory();

        /// <summary>
        /// 获取资源的类型
        /// </summary>
        public Type ResourceType => typeof(T);

        /// <summary>
        /// 获取资源的键值
        /// </summary>
        public string Key { get; } = key;
    }
    

    /// <summary>
    /// 工厂注册表，管理所有已注册的资源工厂
    /// </summary>
    internal sealed class Registry
    {
        /// <summary>
        /// 存储所有已注册的工厂函数，键为资源类型，值为对应的工厂条目对象
        /// </summary>
        private readonly Dictionary<(Type type, string key), IPreloadableEntry> _factories = new();

        /// <summary>
        /// 注册指定类型的资源工厂
        /// </summary>
        /// <typeparam name="T">要注册的资源类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="factory">创建该类型资源的工厂函数</param>
        /// <param name="preload">是否需要预加载该资源，默认为false</param>
        public void Register<T>(string key, Func<T> factory, bool preload = false)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Resource key cannot be null or empty.", nameof(key));

            var dictKey = (typeof(T), key);

            _factories[dictKey] = new Entry<T>(key, factory, preload);
        }

        /// <summary>
        /// 解析并获取指定类型的工厂函数
        /// </summary>
        /// <typeparam name="T">要获取工厂函数的资源类型</typeparam>
        /// <param name="key">资源键</param>
        /// <returns>指定类型的工厂函数</returns>
        /// <exception cref="InvalidOperationException">当指定类型的工厂未注册时抛出异常</exception>
        public Func<T> ResolveFactory<T>(string key)
        {
            var dictKey = (typeof(T), key);

            if (_factories.TryGetValue(dictKey, out var entry)
                && entry is Entry<T> typed)
            {
                return typed.Factory;
            }

            throw new InvalidOperationException(
                $"Factory not registered: {typeof(T).Name} with key '{key}'");
        }
        
        /// <summary>
        /// 预加载所有标记为需要预加载的资源
        /// </summary>
        public void PreloadAll()
        {
            // 遍历所有已注册的工厂
            foreach (var entry in _factories.Values.Where(entry => entry.Preload))
            {
                // 执行其工厂方法进行预加载
                entry.ExecuteFactory();
            }
        }
    }

}
