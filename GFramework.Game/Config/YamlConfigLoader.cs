using System.IO;
using GFramework.Game.Abstractions.Config;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GFramework.Game.Config;

/// <summary>
///     基于文件目录的 YAML 配置加载器。
///     该实现用于 Runtime MVP 的文本配置接入阶段，通过显式注册表定义描述要加载的配置域，
///     再在一次加载流程中统一解析并写入配置注册表。
/// </summary>
public sealed class YamlConfigLoader : IConfigLoader
{
    private const string RootPathCannotBeNullOrWhiteSpaceMessage = "Root path cannot be null or whitespace.";
    private const string TableNameCannotBeNullOrWhiteSpaceMessage = "Table name cannot be null or whitespace.";
    private const string RelativePathCannotBeNullOrWhiteSpaceMessage = "Relative path cannot be null or whitespace.";

    private readonly IDeserializer _deserializer;
    private readonly List<IYamlTableRegistration> _registrations = new();
    private readonly string _rootPath;

    /// <summary>
    ///     使用指定配置根目录创建 YAML 配置加载器。
    /// </summary>
    /// <param name="rootPath">配置根目录。</param>
    /// <exception cref="ArgumentException">当 <paramref name="rootPath" /> 为空时抛出。</exception>
    public YamlConfigLoader(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new ArgumentException(RootPathCannotBeNullOrWhiteSpaceMessage, nameof(rootPath));
        }

        _rootPath = rootPath;
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    /// <summary>
    ///     获取配置根目录。
    /// </summary>
    public string RootPath => _rootPath;

    /// <summary>
    ///     获取当前已注册的配置表定义数量。
    /// </summary>
    public int RegistrationCount => _registrations.Count;

    /// <inheritdoc />
    public async Task LoadAsync(IConfigRegistry registry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(registry);

        var loadedTables = new List<(string name, IConfigTable table)>(_registrations.Count);

        foreach (var registration in _registrations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            loadedTables.Add(await registration.LoadAsync(_rootPath, _deserializer, cancellationToken));
        }

        // 仅当本轮所有配置表都成功加载后才写入注册表，避免暴露部分成功的中间状态。
        foreach (var (name, table) in loadedTables)
        {
            RegistrationDispatcher.Register(registry, name, table);
        }
    }

    /// <summary>
    ///     注册一个 YAML 配置表定义。
    ///     主键提取逻辑由调用方显式提供，以避免在 Runtime MVP 阶段引入额外特性或约定推断。
    /// </summary>
    /// <typeparam name="TKey">配置主键类型。</typeparam>
    /// <typeparam name="TValue">配置值类型。</typeparam>
    /// <param name="tableName">配置表名称。</param>
    /// <param name="relativePath">相对配置根目录的子目录。</param>
    /// <param name="keySelector">配置项主键提取器。</param>
    /// <param name="comparer">可选主键比较器。</param>
    /// <returns>当前加载器实例，以便链式注册。</returns>
    public YamlConfigLoader RegisterTable<TKey, TValue>(
        string tableName,
        string relativePath,
        Func<TValue, TKey> keySelector,
        IEqualityComparer<TKey>? comparer = null)
        where TKey : notnull
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            throw new ArgumentException(TableNameCannotBeNullOrWhiteSpaceMessage, nameof(tableName));
        }

        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new ArgumentException(RelativePathCannotBeNullOrWhiteSpaceMessage, nameof(relativePath));
        }

        ArgumentNullException.ThrowIfNull(keySelector);

        _registrations.Add(new YamlTableRegistration<TKey, TValue>(tableName, relativePath, keySelector, comparer));
        return this;
    }

    /// <summary>
    ///     负责在非泛型配置表与泛型注册表方法之间做分派。
    ///     该静态助手将运行时反射局部封装在加载器内部，避免向外暴露弱类型注册 API。
    /// </summary>
    private static class RegistrationDispatcher
    {
        /// <summary>
        ///     将强类型配置表写入注册表。
        /// </summary>
        /// <param name="registry">目标配置注册表。</param>
        /// <param name="name">配置表名称。</param>
        /// <param name="table">已加载的配置表实例。</param>
        /// <exception cref="InvalidOperationException">当传入表未实现强类型配置表契约时抛出。</exception>
        public static void Register(IConfigRegistry registry, string name, IConfigTable table)
        {
            var tableInterface = table.GetType()
                .GetInterfaces()
                .FirstOrDefault(static type =>
                    type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IConfigTable<,>));

            if (tableInterface == null)
            {
                throw new InvalidOperationException(
                    $"Loaded config table '{name}' does not implement '{typeof(IConfigTable<,>).Name}'.");
            }

            var genericArguments = tableInterface.GetGenericArguments();
            var method = typeof(IConfigRegistry)
                .GetMethod(nameof(IConfigRegistry.RegisterTable))!
                .MakeGenericMethod(genericArguments[0], genericArguments[1]);

            method.Invoke(registry, new object[] { name, table });
        }
    }

    /// <summary>
    ///     定义 YAML 配置表注册项的统一内部契约。
    /// </summary>
    private interface IYamlTableRegistration
    {
        /// <summary>
        ///     从指定根目录加载配置表。
        /// </summary>
        /// <param name="rootPath">配置根目录。</param>
        /// <param name="deserializer">YAML 反序列化器。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>已加载的配置表名称与配置表实例。</returns>
        Task<(string name, IConfigTable table)> LoadAsync(
            string rootPath,
            IDeserializer deserializer,
            CancellationToken cancellationToken);
    }

    /// <summary>
    ///     YAML 配置表注册项。
    /// </summary>
    /// <typeparam name="TKey">配置主键类型。</typeparam>
    /// <typeparam name="TValue">配置项值类型。</typeparam>
    private sealed class YamlTableRegistration<TKey, TValue> : IYamlTableRegistration
        where TKey : notnull
    {
        private readonly IEqualityComparer<TKey>? _comparer;
        private readonly Func<TValue, TKey> _keySelector;

        /// <summary>
        ///     初始化 YAML 配置表注册项。
        /// </summary>
        /// <param name="name">配置表名称。</param>
        /// <param name="relativePath">相对配置根目录的子目录。</param>
        /// <param name="keySelector">配置项主键提取器。</param>
        /// <param name="comparer">可选主键比较器。</param>
        public YamlTableRegistration(
            string name,
            string relativePath,
            Func<TValue, TKey> keySelector,
            IEqualityComparer<TKey>? comparer)
        {
            Name = name;
            RelativePath = relativePath;
            _keySelector = keySelector;
            _comparer = comparer;
        }

        /// <summary>
        ///     获取配置表名称。
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     获取相对配置根目录的子目录。
        /// </summary>
        public string RelativePath { get; }

        /// <inheritdoc />
        public async Task<(string name, IConfigTable table)> LoadAsync(
            string rootPath,
            IDeserializer deserializer,
            CancellationToken cancellationToken)
        {
            var directoryPath = Path.Combine(rootPath, RelativePath);
            if (!Directory.Exists(directoryPath))
            {
                throw new DirectoryNotFoundException(
                    $"Config directory '{directoryPath}' was not found for table '{Name}'.");
            }

            var values = new List<TValue>();
            var files = Directory
                .EnumerateFiles(directoryPath, "*.*", SearchOption.TopDirectoryOnly)
                .Where(static path =>
                    path.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith(".yml", StringComparison.OrdinalIgnoreCase))
                .OrderBy(static path => path, StringComparer.Ordinal)
                .ToArray();

            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string yaml;
                try
                {
                    yaml = await File.ReadAllTextAsync(file, cancellationToken);
                }
                catch (Exception exception)
                {
                    throw new InvalidOperationException(
                        $"Failed to read config file '{file}' for table '{Name}'.",
                        exception);
                }

                try
                {
                    var value = deserializer.Deserialize<TValue>(yaml);

                    if (value == null)
                    {
                        throw new InvalidOperationException("YAML content was deserialized to null.");
                    }

                    values.Add(value);
                }
                catch (Exception exception)
                {
                    throw new InvalidOperationException(
                        $"Failed to deserialize config file '{file}' for table '{Name}' as '{typeof(TValue).Name}'.",
                        exception);
                }
            }

            try
            {
                var table = new InMemoryConfigTable<TKey, TValue>(values, _keySelector, _comparer);
                return (Name, table);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    $"Failed to build config table '{Name}' from directory '{directoryPath}'.",
                    exception);
            }
        }
    }
}