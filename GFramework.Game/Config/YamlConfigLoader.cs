using System.Diagnostics;
using System.Globalization;
using System.Threading;
using GFramework.Core.Abstractions.Events;
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
    private const string DefaultHotReloadUnavailableMessage =
        "Hot reload is not available for the current loader configuration.";

    private const string RootPathCannotBeNullOrWhiteSpaceMessage = "Root path cannot be null or whitespace.";
    private const string TableNameCannotBeNullOrWhiteSpaceMessage = "Table name cannot be null or whitespace.";
    private const string RelativePathCannotBeNullOrWhiteSpaceMessage = "Relative path cannot be null or whitespace.";

    private const string SchemaRelativePathCannotBeNullOrWhiteSpaceMessage =
        "Schema relative path cannot be null or whitespace.";

    private static readonly TimeSpan DefaultHotReloadDebounceDelay = TimeSpan.FromMilliseconds(200);

    private readonly Func<bool> _canEnableHotReload;
    private readonly IDeserializer _deserializer;
    private readonly string _hotReloadUnavailableMessage;

    private readonly Dictionary<string, IReadOnlyCollection<string>> _lastSuccessfulDependencies =
        new(StringComparer.Ordinal);

    private readonly List<IYamlTableRegistration> _registrations = new();
    private readonly string _rootPath;

    /// <summary>
    ///     使用指定配置根目录创建 YAML 配置加载器。
    /// </summary>
    /// <param name="rootPath">配置根目录。</param>
    /// <exception cref="ArgumentException">当 <paramref name="rootPath" /> 为空时抛出。</exception>
    public YamlConfigLoader(string rootPath)
        : this(rootPath, null, null)
    {
    }

    /// <summary>
    ///     使用指定配置根目录与热重载可用性守卫创建 YAML 配置加载器。
    /// </summary>
    /// <param name="rootPath">配置根目录。</param>
    /// <param name="canEnableHotReload">
    ///     用于判断当前实例是否允许启用热重载的委托。
    ///     宿主适配层可借此把额外的文件系统前置条件下沉到底层加载器，避免公开实例被绕过时启用错误监听目标。
    /// </param>
    /// <param name="hotReloadUnavailableMessage">
    ///     当 <paramref name="canEnableHotReload" /> 返回 <see langword="false" /> 时抛出的异常消息；
    ///     为空时使用默认消息。
    /// </param>
    /// <exception cref="ArgumentException">当 <paramref name="rootPath" /> 为空时抛出。</exception>
    internal YamlConfigLoader(
        string rootPath,
        Func<bool>? canEnableHotReload,
        string? hotReloadUnavailableMessage)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new ArgumentException(RootPathCannotBeNullOrWhiteSpaceMessage, nameof(rootPath));
        }

        _rootPath = rootPath;
        _canEnableHotReload = canEnableHotReload ?? (() => true);
        _hotReloadUnavailableMessage = string.IsNullOrWhiteSpace(hotReloadUnavailableMessage)
            ? DefaultHotReloadUnavailableMessage
            : hotReloadUnavailableMessage;
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

        var loadedTables = new List<YamlTableLoadResult>(_registrations.Count);

        foreach (var registration in _registrations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            loadedTables.Add(
                await registration.LoadAsync(_rootPath, _deserializer, cancellationToken).ConfigureAwait(false));
        }

        CrossTableReferenceValidator.Validate(registry, loadedTables);

        // 仅当本轮所有配置表都成功加载后才写入注册表，避免暴露部分成功的中间状态。
        foreach (var loadedTable in loadedTables)
        {
            RegistrationDispatcher.Register(registry, loadedTable.Name, loadedTable.Table);
        }

        UpdateLastSuccessfulDependencies(loadedTables);
    }

    /// <summary>
    ///     启用开发期热重载。
    ///     该能力会监听已注册配置表对应的配置目录和 schema 文件，并在检测到文件变更后按表粒度重新加载。
    ///     重载失败时会保留注册表中的旧表，避免开发期错误配置直接破坏当前运行时状态。
    /// </summary>
    /// <param name="registry">要被热重载更新的配置注册表。</param>
    /// <param name="onTableReloaded">单个配置表重载成功后的可选回调。</param>
    /// <param name="onTableReloadFailed">
    ///     单个配置表重载失败后的可选回调。
    ///     当失败来自加载器本身时，传入异常通常为 <see cref="ConfigLoadException" />，可从其诊断对象读取稳定字段。
    /// </param>
    /// <param name="debounceDelay">防抖延迟；为空时默认使用 200 毫秒。</param>
    /// <returns>用于停止热重载监听的注销句柄。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="registry" /> 为空时抛出。</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     当显式提供的 <paramref name="debounceDelay" /> 小于 <see cref="TimeSpan.Zero" /> 时抛出。
    /// </exception>
    public IUnRegister EnableHotReload(
        IConfigRegistry registry,
        Action<string>? onTableReloaded = null,
        Action<string, Exception>? onTableReloadFailed = null,
        TimeSpan? debounceDelay = null)
    {
        return EnableHotReload(
            registry,
            new YamlConfigHotReloadOptions
            {
                OnTableReloaded = onTableReloaded,
                OnTableReloadFailed = onTableReloadFailed,
                DebounceDelay = debounceDelay ?? DefaultHotReloadDebounceDelay
            });
    }

    /// <summary>
    ///     启用开发期热重载，并通过选项对象集中配置回调和防抖行为。
    ///     该入口用于减少继续堆叠位置参数重载的需要，
    ///     也为未来扩展过滤策略或日志钩子预留稳定形态。
    /// </summary>
    /// <param name="registry">要被热重载更新的配置注册表。</param>
    /// <param name="options">热重载配置选项；为空时使用默认选项。</param>
    /// <returns>用于停止热重载监听的注销句柄。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="registry" /> 为空时抛出。</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     当 <paramref name="options" /> 的 <see cref="YamlConfigHotReloadOptions.DebounceDelay" /> 小于
    ///     <see cref="TimeSpan.Zero" /> 时抛出。
    /// </exception>
    public IUnRegister EnableHotReload(
        IConfigRegistry registry,
        YamlConfigHotReloadOptions? options)
    {
        ArgumentNullException.ThrowIfNull(registry);
        options ??= new YamlConfigHotReloadOptions();
        EnsureHotReloadCanBeEnabled();
        if (options.DebounceDelay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "DebounceDelay must be greater than or equal to zero.");
        }

        return new HotReloadSession(
            _rootPath,
            _deserializer,
            registry,
            _registrations,
            _lastSuccessfulDependencies,
            options.OnTableReloaded,
            options.OnTableReloadFailed,
            options.DebounceDelay);
    }

    private void EnsureHotReloadCanBeEnabled()
    {
        if (_canEnableHotReload())
        {
            return;
        }

        // Host adapters can attach additional filesystem constraints to the loader instance.
        // Enforcing the guard here prevents callers from bypassing the adapter by invoking
        // EnableHotReload directly on the exposed loader reference.
        throw new InvalidOperationException(_hotReloadUnavailableMessage);
    }

    private void UpdateLastSuccessfulDependencies(IEnumerable<YamlTableLoadResult> loadedTables)
    {
        _lastSuccessfulDependencies.Clear();

        foreach (var loadedTable in loadedTables)
        {
            _lastSuccessfulDependencies[loadedTable.Name] = loadedTable.ReferencedTableNames;
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
    /// <exception cref="ArgumentException">
    ///     当 <paramref name="tableName" /> 或 <paramref name="relativePath" /> 为 null、空字符串或空白字符串时抛出。
    /// </exception>
    /// <exception cref="ArgumentNullException">当 <paramref name="keySelector" /> 为 null 时抛出。</exception>
    public YamlConfigLoader RegisterTable<TKey, TValue>(
        string tableName,
        string relativePath,
        Func<TValue, TKey> keySelector,
        IEqualityComparer<TKey>? comparer = null)
        where TKey : notnull
    {
        return RegisterTable(
            new YamlConfigTableRegistrationOptions<TKey, TValue>(tableName, relativePath, keySelector)
            {
                Comparer = comparer
            });
    }

    /// <summary>
    ///     注册一个带 schema 校验的 YAML 配置表定义。
    ///     该重载会在 YAML 反序列化之前使用指定 schema 拒绝未知字段、缺失必填字段和基础类型错误，
    ///     以避免错误配置以默认值形式悄悄进入运行时。
    /// </summary>
    /// <typeparam name="TKey">配置主键类型。</typeparam>
    /// <typeparam name="TValue">配置值类型。</typeparam>
    /// <param name="tableName">配置表名称。</param>
    /// <param name="relativePath">相对配置根目录的子目录。</param>
    /// <param name="schemaRelativePath">相对配置根目录的 schema 文件路径。</param>
    /// <param name="keySelector">配置项主键提取器。</param>
    /// <param name="comparer">可选主键比较器。</param>
    /// <returns>当前加载器实例，以便链式注册。</returns>
    /// <exception cref="ArgumentException">
    ///     当 <paramref name="tableName" />、<paramref name="relativePath" /> 或 <paramref name="schemaRelativePath" />
    ///     为 null、空字符串或空白字符串时抛出。
    /// </exception>
    /// <exception cref="ArgumentNullException">当 <paramref name="keySelector" /> 为 null 时抛出。</exception>
    public YamlConfigLoader RegisterTable<TKey, TValue>(
        string tableName,
        string relativePath,
        string schemaRelativePath,
        Func<TValue, TKey> keySelector,
        IEqualityComparer<TKey>? comparer = null)
        where TKey : notnull
    {
        return RegisterTable(
            new YamlConfigTableRegistrationOptions<TKey, TValue>(tableName, relativePath, keySelector)
            {
                SchemaRelativePath = schemaRelativePath,
                Comparer = comparer
            });
    }

    /// <summary>
    ///     使用选项对象注册一个 YAML 配置表定义。
    ///     该入口集中承载配置目录、schema 路径、主键提取器和比较器，
    ///     以避免未来继续为新增开关叠加更多重载。
    /// </summary>
    /// <typeparam name="TKey">配置主键类型。</typeparam>
    /// <typeparam name="TValue">配置值类型。</typeparam>
    /// <param name="options">配置表注册选项。</param>
    /// <returns>当前加载器实例，以便链式注册。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="options" /> 为空时抛出。</exception>
    /// <exception cref="ArgumentException">
    ///     当 <paramref name="options" /> 内的 <see cref="YamlConfigTableRegistrationOptions{TKey, TValue}.TableName" />、
    ///     <see cref="YamlConfigTableRegistrationOptions{TKey, TValue}.RelativePath" /> 或
    ///     <see cref="YamlConfigTableRegistrationOptions{TKey, TValue}.SchemaRelativePath" /> 为 null、空字符串或空白字符串时抛出。
    /// </exception>
    public YamlConfigLoader RegisterTable<TKey, TValue>(YamlConfigTableRegistrationOptions<TKey, TValue> options)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(options);

        return RegisterTableCore(
            options.TableName,
            options.RelativePath,
            options.SchemaRelativePath,
            options.KeySelector,
            options.Comparer);
    }

    private YamlConfigLoader RegisterTableCore<TKey, TValue>(
        string tableName,
        string relativePath,
        string? schemaRelativePath,
        Func<TValue, TKey> keySelector,
        IEqualityComparer<TKey>? comparer)
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

        if (schemaRelativePath != null && string.IsNullOrWhiteSpace(schemaRelativePath))
        {
            throw new ArgumentException(
                SchemaRelativePathCannotBeNullOrWhiteSpaceMessage,
                nameof(schemaRelativePath));
        }

        _registrations.Add(
            new YamlTableRegistration<TKey, TValue>(
                tableName,
                relativePath,
                schemaRelativePath,
                keySelector,
                comparer));
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
        ///     获取配置表名称。
        /// </summary>
        string Name { get; }

        /// <summary>
        ///     获取相对配置根目录的子目录。
        /// </summary>
        string RelativePath { get; }

        /// <summary>
        ///     获取相对配置根目录的 schema 文件路径；未启用 schema 校验时返回空。
        /// </summary>
        string? SchemaRelativePath { get; }

        /// <summary>
        ///     从指定根目录加载配置表。
        /// </summary>
        /// <param name="rootPath">配置根目录。</param>
        /// <param name="deserializer">YAML 反序列化器。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>已加载的配置表结果。</returns>
        Task<YamlTableLoadResult> LoadAsync(
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
        /// <param name="schemaRelativePath">相对配置根目录的 schema 文件路径；未启用 schema 校验时为空。</param>
        /// <param name="keySelector">配置项主键提取器。</param>
        /// <param name="comparer">可选主键比较器。</param>
        public YamlTableRegistration(
            string name,
            string relativePath,
            string? schemaRelativePath,
            Func<TValue, TKey> keySelector,
            IEqualityComparer<TKey>? comparer)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(name), "Table registrations should always have a non-empty name.");
            Debug.Assert(!string.IsNullOrWhiteSpace(relativePath),
                "Table registrations should always have a non-empty relative path.");

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(TableNameCannotBeNullOrWhiteSpaceMessage, nameof(name));
            }

            if (string.IsNullOrWhiteSpace(relativePath))
            {
                throw new ArgumentException(RelativePathCannotBeNullOrWhiteSpaceMessage, nameof(relativePath));
            }

            Name = name;
            RelativePath = relativePath;
            SchemaRelativePath = schemaRelativePath;
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

        /// <summary>
        ///     获取相对配置根目录的 schema 文件路径；未启用 schema 校验时返回空。
        /// </summary>
        public string? SchemaRelativePath { get; }

        /// <inheritdoc />
        public async Task<YamlTableLoadResult> LoadAsync(
            string rootPath,
            IDeserializer deserializer,
            CancellationToken cancellationToken)
        {
            var directoryPath = GetValidatedDirectoryPath(rootPath);
            var schema = await LoadSchemaAsync(rootPath, cancellationToken).ConfigureAwait(false);
            var referenceUsages = new List<YamlConfigReferenceUsage>();
            var values = await LoadValuesAsync(
                    directoryPath,
                    deserializer,
                    schema,
                    referenceUsages,
                    cancellationToken)
                .ConfigureAwait(false);
            return BuildLoadResult(directoryPath, schema, values, referenceUsages, cancellationToken);
        }

        private string GetValidatedDirectoryPath(string rootPath)
        {
            var directoryPath = Path.Combine(rootPath, RelativePath);
            if (Directory.Exists(directoryPath))
            {
                return directoryPath;
            }

            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.ConfigDirectoryNotFound,
                Name,
                $"Config directory '{directoryPath}' was not found for table '{Name}'.",
                configDirectoryPath: directoryPath);
        }

        private async Task<YamlConfigSchema?> LoadSchemaAsync(string rootPath, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(SchemaRelativePath))
            {
                return null;
            }

            var schemaPath = Path.Combine(rootPath, SchemaRelativePath);
            return await YamlConfigSchemaValidator.LoadAsync(Name, schemaPath, cancellationToken).ConfigureAwait(false);
        }

        private async Task<List<TValue>> LoadValuesAsync(
            string directoryPath,
            IDeserializer deserializer,
            YamlConfigSchema? schema,
            List<YamlConfigReferenceUsage> referenceUsages,
            CancellationToken cancellationToken)
        {
            var values = new List<TValue>();
            foreach (var file in GetYamlFiles(directoryPath))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var yaml = await ReadYamlAsync(directoryPath, file, schema, cancellationToken).ConfigureAwait(false);
                CollectReferenceUsages(referenceUsages, schema, file, yaml);
                values.Add(DeserializeValue(deserializer, directoryPath, file, schema, yaml, cancellationToken));
            }

            return values;
        }

        private static string[] GetYamlFiles(string directoryPath)
        {
            return Directory
                .EnumerateFiles(directoryPath, "*.*", SearchOption.TopDirectoryOnly)
                .Where(static path =>
                    path.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith(".yml", StringComparison.OrdinalIgnoreCase))
                .OrderBy(static path => path, StringComparer.Ordinal)
                .ToArray();
        }

        private async Task<string> ReadYamlAsync(
            string directoryPath,
            string file,
            YamlConfigSchema? schema,
            CancellationToken cancellationToken)
        {
            try
            {
                return await File.ReadAllTextAsync(file, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // 保留原始取消语义，避免热重载把会话级取消误报为配置读取失败。
                throw;
            }
            catch (Exception exception)
            {
                throw ConfigLoadExceptionFactory.Create(
                    ConfigLoadFailureKind.ConfigFileReadFailed,
                    Name,
                    $"Failed to read config file '{file}' for table '{Name}'.",
                    configDirectoryPath: directoryPath,
                    yamlPath: file,
                    schemaPath: schema?.SchemaPath,
                    innerException: exception);
            }
        }

        private void CollectReferenceUsages(
            List<YamlConfigReferenceUsage> referenceUsages,
            YamlConfigSchema? schema,
            string file,
            string yaml)
        {
            if (schema == null)
            {
                return;
            }

            // 先按 schema 拒绝结构问题并提取跨表引用，避免被 IgnoreUnmatchedProperties 或默认值掩盖配置错误。
            referenceUsages.AddRange(
                YamlConfigSchemaValidator.ValidateAndCollectReferences(Name, schema, file, yaml));
        }

        private TValue DeserializeValue(
            IDeserializer deserializer,
            string directoryPath,
            string file,
            YamlConfigSchema? schema,
            string yaml,
            CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var value = deserializer.Deserialize<TValue>(yaml);
                if (value != null)
                {
                    return value;
                }

                throw new InvalidOperationException("YAML content was deserialized to null.");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // 同步反序列化阶段也要透传会话级取消，避免把停止加载误报为 YAML 解析失败。
                throw;
            }
            catch (Exception exception)
            {
                throw ConfigLoadExceptionFactory.Create(
                    ConfigLoadFailureKind.DeserializationFailed,
                    Name,
                    $"Failed to deserialize config file '{file}' for table '{Name}' as '{typeof(TValue).Name}'.",
                    configDirectoryPath: directoryPath,
                    yamlPath: file,
                    schemaPath: schema?.SchemaPath,
                    detail: $"Target CLR type: {typeof(TValue).FullName}.",
                    innerException: exception);
            }
        }

        private YamlTableLoadResult BuildLoadResult(
            string directoryPath,
            YamlConfigSchema? schema,
            List<TValue> values,
            List<YamlConfigReferenceUsage> referenceUsages,
            CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var table = new InMemoryConfigTable<TKey, TValue>(values, _keySelector, _comparer);
                return new YamlTableLoadResult(
                    Name,
                    table,
                    schema?.ReferencedTableNames ?? Array.Empty<string>(),
                    referenceUsages);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // 构建最终配置表时继续保留原始取消语义，避免热重载把提交前取消记录成构表失败。
                throw;
            }
            catch (Exception exception)
            {
                throw ConfigLoadExceptionFactory.Create(
                    ConfigLoadFailureKind.TableBuildFailed,
                    Name,
                    $"Failed to build config table '{Name}' from directory '{directoryPath}'.",
                    configDirectoryPath: directoryPath,
                    schemaPath: schema?.SchemaPath,
                    innerException: exception);
            }
        }
    }

    /// <summary>
    ///     表示单个注册项加载完成后的中间结果。
    ///     该结果同时携带配置表实例、schema 声明的依赖关系和 YAML 中提取出的实际引用，以便在批量提交前完成跨表一致性校验。
    /// </summary>
    private sealed class YamlTableLoadResult
    {
        /// <summary>
        ///     初始化一个表加载结果。
        /// </summary>
        /// <param name="name">配置表名称。</param>
        /// <param name="table">已构建好的配置表。</param>
        /// <param name="referencedTableNames">schema 声明的依赖表名称集合。</param>
        /// <param name="referenceUsages">YAML 中提取出的实际引用集合。</param>
        public YamlTableLoadResult(
            string name,
            IConfigTable table,
            IReadOnlyCollection<string> referencedTableNames,
            IReadOnlyCollection<YamlConfigReferenceUsage> referenceUsages)
        {
            ArgumentNullException.ThrowIfNull(name);
            ArgumentNullException.ThrowIfNull(table);
            ArgumentNullException.ThrowIfNull(referencedTableNames);
            ArgumentNullException.ThrowIfNull(referenceUsages);

            Name = name;
            Table = table;
            ReferencedTableNames = referencedTableNames;
            ReferenceUsages = referenceUsages;
        }

        /// <summary>
        ///     获取配置表名称。
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     获取已构建好的配置表。
        /// </summary>
        public IConfigTable Table { get; }

        /// <summary>
        ///     获取 schema 声明的依赖表名称集合。
        /// </summary>
        public IReadOnlyCollection<string> ReferencedTableNames { get; }

        /// <summary>
        ///     获取 YAML 中提取出的实际引用集合。
        /// </summary>
        public IReadOnlyCollection<YamlConfigReferenceUsage> ReferenceUsages { get; }
    }

    /// <summary>
    ///     负责在所有注册项加载完成后执行跨表引用校验。
    ///     该阶段在真正写入注册表之前运行，确保任何缺失目标表、主键类型不兼容或目标行不存在的情况都会整体回滚。
    /// </summary>
    private static class CrossTableReferenceValidator
    {
        private delegate bool IntegerTryParseDelegate<T>(
            string? value,
            NumberStyles style,
            IFormatProvider? provider,
            out T result);

        /// <summary>
        ///     使用本轮新加载结果与注册表中保留的旧表，一起验证跨表引用是否全部有效。
        /// </summary>
        /// <param name="registry">当前配置注册表。</param>
        /// <param name="loadedTables">本轮加载出的配置表集合。</param>
        public static void Validate(IConfigRegistry registry, IReadOnlyCollection<YamlTableLoadResult> loadedTables)
        {
            ArgumentNullException.ThrowIfNull(registry);
            ArgumentNullException.ThrowIfNull(loadedTables);

            var loadedTableLookup = loadedTables.ToDictionary(static table => table.Name, StringComparer.Ordinal);

            foreach (var loadedTable in loadedTables)
            {
                foreach (var referenceUsage in loadedTable.ReferenceUsages)
                {
                    if (!TryResolveTargetTable(registry, loadedTableLookup, referenceUsage.ReferencedTableName,
                            out var targetTable))
                    {
                        throw ConfigLoadExceptionFactory.Create(
                            ConfigLoadFailureKind.ReferencedTableNotFound,
                            loadedTable.Name,
                            $"Config file '{referenceUsage.YamlPath}' property '{referenceUsage.DisplayPath}' references table '{referenceUsage.ReferencedTableName}', but that table is not available in the current loader batch or registry.",
                            yamlPath: referenceUsage.YamlPath,
                            schemaPath: referenceUsage.SchemaPath,
                            displayPath: referenceUsage.DisplayPath,
                            referencedTableName: referenceUsage.ReferencedTableName,
                            rawValue: referenceUsage.RawValue);
                    }

                    if (!TryConvertReferenceKey(referenceUsage, targetTable.KeyType, out var convertedKey,
                            out var conversionError))
                    {
                        throw ConfigLoadExceptionFactory.Create(
                            ConfigLoadFailureKind.ReferenceKeyTypeMismatch,
                            loadedTable.Name,
                            $"Config file '{referenceUsage.YamlPath}' property '{referenceUsage.DisplayPath}' cannot target table '{referenceUsage.ReferencedTableName}' with key type '{targetTable.KeyType.Name}'. {conversionError}",
                            yamlPath: referenceUsage.YamlPath,
                            schemaPath: referenceUsage.SchemaPath,
                            displayPath: referenceUsage.DisplayPath,
                            referencedTableName: referenceUsage.ReferencedTableName,
                            rawValue: referenceUsage.RawValue,
                            detail: conversionError);
                    }

                    if (!ContainsKey(targetTable, convertedKey!))
                    {
                        throw ConfigLoadExceptionFactory.Create(
                            ConfigLoadFailureKind.ReferencedKeyNotFound,
                            loadedTable.Name,
                            $"Config file '{referenceUsage.YamlPath}' property '{referenceUsage.DisplayPath}' references missing key '{referenceUsage.RawValue}' in table '{referenceUsage.ReferencedTableName}'.",
                            yamlPath: referenceUsage.YamlPath,
                            schemaPath: referenceUsage.SchemaPath,
                            displayPath: referenceUsage.DisplayPath,
                            referencedTableName: referenceUsage.ReferencedTableName,
                            rawValue: referenceUsage.RawValue);
                    }
                }
            }
        }

        private static bool TryResolveTargetTable(
            IConfigRegistry registry,
            IReadOnlyDictionary<string, YamlTableLoadResult> loadedTableLookup,
            string tableName,
            out IConfigTable table)
        {
            if (loadedTableLookup.TryGetValue(tableName, out var loadedTable))
            {
                table = loadedTable.Table;
                return true;
            }

            if (registry.TryGetTable(tableName, out var registeredTable) && registeredTable != null)
            {
                table = registeredTable;
                return true;
            }

            table = null!;
            return false;
        }

        private static bool TryConvertReferenceKey(
            YamlConfigReferenceUsage referenceUsage,
            Type targetKeyType,
            out object? convertedKey,
            out string errorMessage)
        {
            convertedKey = null;
            errorMessage = string.Empty;

            if (referenceUsage.ValueType == YamlConfigSchemaPropertyType.String)
            {
                if (targetKeyType != typeof(string))
                {
                    errorMessage =
                        $"Reference values declared as schema type 'string' can currently only target string-key tables, but the target key type is '{targetKeyType.Name}'.";
                    return false;
                }

                convertedKey = referenceUsage.RawValue;
                return true;
            }

            if (referenceUsage.ValueType != YamlConfigSchemaPropertyType.Integer)
            {
                errorMessage =
                    $"Reference values currently only support schema scalar types 'string' and 'integer', but the actual type is '{referenceUsage.ValueType}'.";
                return false;
            }

            return TryConvertIntegerKey(referenceUsage.RawValue, targetKeyType, out convertedKey, out errorMessage);
        }

        private static bool TryConvertIntegerKey(
            string rawValue,
            Type targetKeyType,
            out object? convertedKey,
            out string errorMessage)
        {
            convertedKey = null;
            errorMessage = string.Empty;

            if (TryConvertIntegerKey<int>(rawValue, targetKeyType, typeof(int), int.TryParse, out convertedKey) ||
                TryConvertIntegerKey<long>(rawValue, targetKeyType, typeof(long), long.TryParse, out convertedKey) ||
                TryConvertIntegerKey<short>(rawValue, targetKeyType, typeof(short), short.TryParse, out convertedKey) ||
                TryConvertIntegerKey<byte>(rawValue, targetKeyType, typeof(byte), byte.TryParse, out convertedKey) ||
                TryConvertIntegerKey<uint>(rawValue, targetKeyType, typeof(uint), uint.TryParse, out convertedKey) ||
                TryConvertIntegerKey<ulong>(rawValue, targetKeyType, typeof(ulong), ulong.TryParse, out convertedKey) ||
                TryConvertIntegerKey<ushort>(rawValue, targetKeyType, typeof(ushort), ushort.TryParse, out convertedKey) ||
                TryConvertIntegerKey<sbyte>(rawValue, targetKeyType, typeof(sbyte), sbyte.TryParse, out convertedKey))
            {
                return true;
            }

            errorMessage =
                $"Reference value '{rawValue}' cannot be converted to supported target key type '{targetKeyType.Name}'. Integer references currently support the standard signed and unsigned integer CLR key types.";
            return false;
        }

        private static bool TryConvertIntegerKey<T>(
            string rawValue,
            Type targetKeyType,
            Type supportedType,
            IntegerTryParseDelegate<T> tryParse,
            out object? convertedKey)
            where T : struct
        {
            convertedKey = null;
            if (targetKeyType != supportedType ||
                !tryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue))
            {
                return false;
            }

            convertedKey = parsedValue;
            return true;
        }

        private static bool ContainsKey(IConfigTable table, object key)
        {
            var tableInterface = table.GetType()
                .GetInterfaces()
                .First(static type =>
                    type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IConfigTable<,>));
            var containsKeyMethod = tableInterface.GetMethod(nameof(IConfigTable<int, int>.ContainsKey))!;
            return (bool)containsKeyMethod.Invoke(table, new[] { key })!;
        }
    }

    /// <summary>
    ///     封装开发期热重载所需的文件监听与按表重载逻辑。
    ///     该会话只影响通过当前加载器注册的表，不尝试接管注册表中的其他来源数据。
    /// </summary>
    private sealed class HotReloadSession : IUnRegister, IDisposable
    {
        private readonly TimeSpan _debounceDelay;

        private readonly Dictionary<string, IReadOnlyCollection<string>> _dependenciesByTable =
            new(StringComparer.Ordinal);

        private readonly IDeserializer _deserializer;
#if NET9_0_OR_GREATER
        // net9.0 及以上目标使用专用 Lock，以满足分析器对专用同步原语的建议。
        private readonly Lock _gate = new();
#else
        // net8.0 目标仍回退到 object 锁，以保持多目标编译兼容性。
        private readonly object _gate = new();
#endif
        private readonly Action<string>? _onTableReloaded;
        private readonly Action<string, Exception>? _onTableReloadFailed;
        private readonly Dictionary<string, IYamlTableRegistration> _registrations = new(StringComparer.Ordinal);
        private readonly IConfigRegistry _registry;
        private readonly Dictionary<string, SemaphoreSlim> _reloadLocks = new(StringComparer.Ordinal);
        private readonly Dictionary<string, CancellationTokenSource> _reloadTokens = new(StringComparer.Ordinal);
        private readonly string _rootPath;
        private readonly List<FileSystemWatcher> _watchers = new();
        private bool _disposed;

        /// <summary>
        ///     初始化一个热重载会话并立即开始监听文件变更。
        /// </summary>
        /// <param name="rootPath">配置根目录。</param>
        /// <param name="deserializer">YAML 反序列化器。</param>
        /// <param name="registry">要更新的配置注册表。</param>
        /// <param name="registrations">已注册的配置表定义。</param>
        /// <param name="initialDependencies">最近一次成功加载后记录下来的跨表依赖图。</param>
        /// <param name="onTableReloaded">单表重载成功回调。</param>
        /// <param name="onTableReloadFailed">单表重载失败回调。</param>
        /// <param name="debounceDelay">监听事件防抖延迟。</param>
        public HotReloadSession(
            string rootPath,
            IDeserializer deserializer,
            IConfigRegistry registry,
            IEnumerable<IYamlTableRegistration> registrations,
            IReadOnlyDictionary<string, IReadOnlyCollection<string>> initialDependencies,
            Action<string>? onTableReloaded,
            Action<string, Exception>? onTableReloadFailed,
            TimeSpan debounceDelay)
        {
            ArgumentNullException.ThrowIfNull(rootPath);
            ArgumentNullException.ThrowIfNull(deserializer);
            ArgumentNullException.ThrowIfNull(registry);
            ArgumentNullException.ThrowIfNull(registrations);
            ArgumentNullException.ThrowIfNull(initialDependencies);

            _rootPath = rootPath;
            _deserializer = deserializer;
            _registry = registry;
            _onTableReloaded = onTableReloaded;
            _onTableReloadFailed = onTableReloadFailed;
            _debounceDelay = debounceDelay;

            foreach (var registration in registrations)
            {
                _registrations.Add(registration.Name, registration);
                _reloadLocks.Add(registration.Name, new SemaphoreSlim(1, 1));
                _dependenciesByTable[registration.Name] =
                    initialDependencies.TryGetValue(registration.Name, out var dependencies)
                        ? dependencies
                        : Array.Empty<string>();
                CreateWatchersForRegistration(registration);
            }
        }

        /// <summary>
        ///     释放热重载会话持有的文件监听器与等待资源。
        /// </summary>
        public void Dispose()
        {
            List<FileSystemWatcher> watchersToDispose;
            List<CancellationTokenSource> reloadTokensToDispose;
            List<SemaphoreSlim> reloadLocksToDispose;

            lock (_gate)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                watchersToDispose = _watchers.ToList();
                _watchers.Clear();
                reloadTokensToDispose = _reloadTokens.Values.ToList();
                _reloadTokens.Clear();
                reloadLocksToDispose = _reloadLocks.Values.ToList();
                _reloadLocks.Clear();
            }

            foreach (var reloadToken in reloadTokensToDispose)
            {
                reloadToken.Cancel();
                reloadToken.Dispose();
            }

            foreach (var watcher in watchersToDispose)
            {
                watcher.Dispose();
            }

            foreach (var reloadLock in reloadLocksToDispose)
            {
                reloadLock.Dispose();
            }
        }

        /// <summary>
        ///     停止热重载监听。
        /// </summary>
        public void UnRegister()
        {
            Dispose();
        }

        private void CreateWatchersForRegistration(IYamlTableRegistration registration)
        {
            var configDirectoryPath = Path.Combine(_rootPath, registration.RelativePath);
            AddWatcher(configDirectoryPath, "*.yaml", registration.Name);
            AddWatcher(configDirectoryPath, "*.yml", registration.Name);

            if (string.IsNullOrEmpty(registration.SchemaRelativePath))
            {
                return;
            }

            var schemaFullPath = Path.Combine(_rootPath, registration.SchemaRelativePath);
            var schemaDirectoryPath = Path.GetDirectoryName(schemaFullPath);
            if (string.IsNullOrWhiteSpace(schemaDirectoryPath))
            {
                schemaDirectoryPath = _rootPath;
            }

            AddWatcher(schemaDirectoryPath, Path.GetFileName(schemaFullPath), registration.Name);
        }

        private void AddWatcher(string directoryPath, string filter, string tableName)
        {
            if (!Directory.Exists(directoryPath))
            {
                return;
            }

            var watcher = new FileSystemWatcher(directoryPath, filter)
            {
                IncludeSubdirectories = false,
                NotifyFilter = NotifyFilters.FileName |
                               NotifyFilters.LastWrite |
                               NotifyFilters.Size |
                               NotifyFilters.CreationTime |
                               NotifyFilters.DirectoryName
            };

            watcher.Changed += (_, _) => ScheduleReload(tableName);
            watcher.Created += (_, _) => ScheduleReload(tableName);
            watcher.Deleted += (_, _) => ScheduleReload(tableName);
            watcher.Renamed += (_, _) => ScheduleReload(tableName);
            watcher.Error += (_, eventArgs) =>
            {
                var exception = eventArgs.GetException() ?? new InvalidOperationException(
                    $"Hot reload watcher for table '{tableName}' encountered an unknown error.");
                InvokeReloadFailed(tableName, exception);
            };

            watcher.EnableRaisingEvents = true;

            lock (_gate)
            {
                if (_disposed)
                {
                    watcher.Dispose();
                    return;
                }

                _watchers.Add(watcher);
            }
        }

        private void ScheduleReload(string tableName)
        {
            CancellationTokenSource reloadTokenSource;

            lock (_gate)
            {
                if (_disposed)
                {
                    return;
                }

                if (_reloadTokens.TryGetValue(tableName, out var previousTokenSource))
                {
                    previousTokenSource.Cancel();
                    previousTokenSource.Dispose();
                }

                reloadTokenSource = new CancellationTokenSource();
                _reloadTokens[tableName] = reloadTokenSource;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(_debounceDelay, reloadTokenSource.Token).ConfigureAwait(false);
                    await ReloadTableAsync(tableName, reloadTokenSource.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (reloadTokenSource.IsCancellationRequested)
                {
                    // 新事件会替换旧任务；取消属于正常防抖行为。
                }
                finally
                {
                    lock (_gate)
                    {
                        if (_reloadTokens.TryGetValue(tableName, out var currentTokenSource) &&
                            ReferenceEquals(currentTokenSource, reloadTokenSource))
                        {
                            _reloadTokens.Remove(tableName);
                        }
                    }

                    reloadTokenSource.Dispose();
                }
            });
        }

        private async Task ReloadTableAsync(string tableName, CancellationToken cancellationToken)
        {
            if (!_registrations.ContainsKey(tableName))
            {
                return;
            }

            var reloadLock = _reloadLocks[tableName];
            await reloadLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var affectedTableNames = GetAffectedTableNames(tableName);
                var loadedTables = new List<YamlTableLoadResult>(affectedTableNames.Count);

                // 目标表变更可能让依赖它的表立即失效，因此热重载需要按受影响闭包整体重验并整体提交。
                foreach (var affectedTableName in affectedTableNames)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    loadedTables.Add(
                        await _registrations[affectedTableName].LoadAsync(_rootPath, _deserializer, cancellationToken)
                            .ConfigureAwait(false));
                }

                CrossTableReferenceValidator.Validate(_registry, loadedTables);

                foreach (var loadedTable in loadedTables)
                {
                    RegistrationDispatcher.Register(_registry, loadedTable.Name, loadedTable.Table);
                    _dependenciesByTable[loadedTable.Name] = loadedTable.ReferencedTableNames;
                }

                InvokeReloaded(tableName);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // 防抖替换或会话关闭导致的取消不应视为错误。
            }
            catch (Exception exception)
            {
                InvokeReloadFailed(tableName, exception);
            }
            finally
            {
                reloadLock.Release();
            }
        }

        private IReadOnlyCollection<string> GetAffectedTableNames(string changedTableName)
        {
            var affectedTableNames = new HashSet<string>(StringComparer.Ordinal)
            {
                changedTableName
            };
            var pendingTableNames = new Queue<string>();
            pendingTableNames.Enqueue(changedTableName);

            while (pendingTableNames.Count > 0)
            {
                var currentTableName = pendingTableNames.Dequeue();

                foreach (var dependency in _dependenciesByTable)
                {
                    if (!ContainsDependency(dependency.Value, currentTableName))
                    {
                        continue;
                    }

                    if (affectedTableNames.Add(dependency.Key))
                    {
                        pendingTableNames.Enqueue(dependency.Key);
                    }
                }
            }

            return affectedTableNames
                .OrderBy(static name => name, StringComparer.Ordinal)
                .ToArray();
        }

        private static bool ContainsDependency(
            IReadOnlyCollection<string> dependencies,
            string tableName)
        {
            return dependencies.Any(
                dependency => string.Equals(dependency, tableName, StringComparison.Ordinal));
        }

        private void InvokeReloaded(string tableName)
        {
            if (_onTableReloaded == null)
            {
                return;
            }

            try
            {
                _onTableReloaded(tableName);
            }
            catch
            {
                // 诊断回调不应反向破坏热重载流程。
            }
        }

        private void InvokeReloadFailed(string tableName, Exception exception)
        {
            if (_onTableReloadFailed == null)
            {
                return;
            }

            try
            {
                _onTableReloadFailed(tableName, exception);
            }
            catch
            {
                // 诊断回调不应反向破坏热重载流程。
            }
        }
    }
}
