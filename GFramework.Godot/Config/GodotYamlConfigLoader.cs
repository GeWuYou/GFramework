using System.IO;
using GFramework.Core.Abstractions.Events;
using GFramework.Game.Abstractions.Config;
using GFramework.Game.Config;
using GFramework.Godot.Extensions;
using FileAccess = Godot.FileAccess;

namespace GFramework.Godot.Config;

/// <summary>
///     为 Godot 运行时提供 YAML 配置加载适配层。
///     编辑器态优先直接把项目目录交给 <see cref="YamlConfigLoader" />，
///     导出态则把显式声明的 YAML 与 schema 文本同步到运行时缓存目录后再加载。
/// </summary>
public sealed class GodotYamlConfigLoader : IConfigLoader
{
    private const string HotReloadUnavailableMessage =
        "Hot reload is only available when the source root can be accessed as a normal filesystem directory.";

    private readonly GodotYamlConfigEnvironment _environment;
    private readonly YamlConfigLoader _loader;
    private readonly GodotYamlConfigLoaderOptions _options;

    /// <summary>
    ///     使用指定选项创建一个 Godot YAML 配置加载器。
    /// </summary>
    /// <param name="options">加载器初始化选项。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="options" /> 为 <see langword="null" /> 时抛出。</exception>
    /// <exception cref="ArgumentException">
    ///     当 <see cref="GodotYamlConfigLoaderOptions.SourceRootPath" /> 或
    ///     <see cref="GodotYamlConfigLoaderOptions.RuntimeCacheRootPath" /> 为空白字符串时抛出。
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     当 Godot 特殊路径无法被全局化为非空绝对路径时抛出。
    /// </exception>
    /// <remarks>
    ///     构造完成后，加载器会根据当前环境决定直接读取 <see cref="SourceRootPath" />，还是先同步到
    ///     <see cref="RuntimeCacheRootPath" /> 再交给底层 <see cref="YamlConfigLoader" />。
    ///     只有源根目录可直接作为普通文件系统目录访问时，<see cref="CanEnableHotReload" /> 才会返回
    ///     <see langword="true" />。
    /// </remarks>
    public GodotYamlConfigLoader(GodotYamlConfigLoaderOptions options)
        : this(options, GodotYamlConfigEnvironment.Default)
    {
    }

    /// <summary>
    ///     使用指定选项和宿主环境抽象创建一个 Godot YAML 配置加载器。
    /// </summary>
    /// <param name="options">加载器初始化选项。</param>
    /// <param name="environment">
    ///     封装编辑器探测、Godot 路径全局化、目录枚举与文件读取行为的宿主环境抽象。
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="options" /> 或 <paramref name="environment" /> 为 <see langword="null" /> 时抛出。
    /// </exception>
    /// <exception cref="ArgumentException">
    ///     <see cref="GodotYamlConfigLoaderOptions.SourceRootPath" /> 或
    ///     <see cref="GodotYamlConfigLoaderOptions.RuntimeCacheRootPath" /> 为空白字符串时抛出。
    /// </exception>
    /// <remarks>
    ///     该重载用于把与 Godot 引擎强耦合的环境行为收敛到可替换委托中。
    ///     编辑器态下，<c>res://</c> 可以被全局化后直接交给底层 <see cref="YamlConfigLoader" />；
    ///     导出态下，则需要先同步到 <c>user://</c> 缓存再切换到普通文件系统路径。
    /// </remarks>
    internal GodotYamlConfigLoader(
        GodotYamlConfigLoaderOptions options,
        GodotYamlConfigEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(environment);

        if (string.IsNullOrWhiteSpace(options.SourceRootPath))
        {
            throw new ArgumentException("SourceRootPath cannot be null or whitespace.", nameof(options));
        }

        if (string.IsNullOrWhiteSpace(options.RuntimeCacheRootPath))
        {
            throw new ArgumentException("RuntimeCacheRootPath cannot be null or whitespace.", nameof(options));
        }

        _options = options;
        _environment = environment;
        LoaderRootPath = ResolveLoaderRootPath();
        _loader = new YamlConfigLoader(
            LoaderRootPath,
            () => CanEnableHotReload,
            HotReloadUnavailableMessage);
        options.ConfigureLoader?.Invoke(_loader);
    }

    /// <summary>
    ///     获取配置源根目录。
    /// </summary>
    public string SourceRootPath => _options.SourceRootPath;

    /// <summary>
    ///     获取运行时缓存根目录。
    /// </summary>
    public string RuntimeCacheRootPath => _options.RuntimeCacheRootPath;

    /// <summary>
    ///     获取底层 <see cref="YamlConfigLoader" /> 实际使用的普通文件系统根目录。
    /// </summary>
    public string LoaderRootPath { get; }

    /// <summary>
    ///     获取底层 <see cref="YamlConfigLoader" /> 实例。
    ///     调用方可继续在该实例上追加注册表定义或读取注册数量。
    /// </summary>
    /// <remarks>
    ///     该实例仅应用于补充注册表定义或检查注册状态。
    ///     不要直接调用 <see cref="YamlConfigLoader.LoadAsync(GFramework.Game.Abstractions.Config.IConfigRegistry,System.Threading.CancellationToken)" />
    ///     或 <see cref="YamlConfigLoader.EnableHotReload(GFramework.Game.Abstractions.Config.IConfigRegistry,YamlConfigHotReloadOptions?)" />；
    ///     应分别改为调用 <see cref="LoadAsync" /> 与 <see cref="EnableHotReload" />，以确保 Godot 适配层先执行缓存同步并维持
    ///     <see cref="CanEnableHotReload" /> 守卫。
    /// </remarks>
    public YamlConfigLoader Loader => _loader;

    /// <summary>
    ///     获取一个值，指示当前实例是否可直接针对源目录启用热重载。
    /// </summary>
    public bool CanEnableHotReload => UsesSourceDirectoryDirectly(SourceRootPath);

    /// <summary>
    ///     执行 Godot 场景下的配置加载。
    ///     当源目录无法直接作为普通文件系统目录访问时，加载器会先把显式声明的 YAML 与 schema 文本同步到运行时缓存，
    ///     再委托底层 <see cref="YamlConfigLoader" /> 完成解析与注册。
    /// </summary>
    /// <param name="registry">用于接收配置表的注册表。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>表示加载流程的异步任务。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="registry" /> 为 <see langword="null" /> 时抛出。</exception>
    /// <exception cref="ConfigLoadException">
    ///     当缓存同步、配置文件读取、schema 读取或底层 YAML 加载失败时抛出。
    /// </exception>
    /// <remarks>
    ///     运行时缓存同步阶段刻意保持同步执行。
    ///     原因在于默认宿主环境可能需要通过 Godot 的目录和文件访问 API 读取 <c>res://</c> 资源，
    ///     而这些访问边界目前仅以同步委托形式暴露；同时底层 <see cref="YamlConfigLoader" /> 也要求缓存文件在开始读取前已经完整落盘。
    ///     这意味着当实例无法直接访问源目录时，调用线程会在进入真正的异步 YAML 解析前承担一次文件系统同步成本。
    /// </remarks>
    public async Task LoadAsync(IConfigRegistry registry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(registry);

        if (!CanEnableHotReload)
        {
            // Runtime cache preparation must finish before the underlying loader starts enumerating files.
            // This step intentionally stays synchronous because the default Godot environment exposes
            // directory enumeration and file reads through synchronous engine/file-system APIs only.
            SynchronizeRuntimeCache(cancellationToken);
        }

        await _loader.LoadAsync(registry, cancellationToken);
    }

    /// <summary>
    ///     在当前环境允许的情况下启用底层 YAML 热重载。
    /// </summary>
    /// <param name="registry">要被热重载更新的配置注册表。</param>
    /// <param name="options">热重载选项；为空时使用默认值。</param>
    /// <returns>用于停止监听的注销句柄。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="registry" /> 为 <see langword="null" /> 时抛出。</exception>
    /// <exception cref="InvalidOperationException">
    ///     当当前实例必须通过运行时缓存访问配置源，无法直接监听真实源目录时抛出。
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     当 <paramref name="options" /> 的防抖延迟小于 <see cref="TimeSpan.Zero" /> 时，
    ///     底层 <see cref="YamlConfigLoader" /> 会拒绝启用热重载。
    /// </exception>
    /// <remarks>
    ///     调用前应先检查 <see cref="CanEnableHotReload" />。
    ///     当 <see cref="SourceRootPath" /> 只能通过缓存同步访问时，拒绝启用热重载是为了避免监听缓存副本后误导调用方，
    ///     让其误以为源目录改动会被自动反映到运行时。
    /// </remarks>
    public IUnRegister EnableHotReload(
        IConfigRegistry registry,
        YamlConfigHotReloadOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(registry);

        if (!CanEnableHotReload)
        {
            throw new InvalidOperationException(HotReloadUnavailableMessage);
        }

        return _loader.EnableHotReload(registry, options);
    }

    private string ResolveLoaderRootPath()
    {
        if (UsesSourceDirectoryDirectly(SourceRootPath))
        {
            return EnsureAbsolutePath(SourceRootPath, nameof(GodotYamlConfigLoaderOptions.SourceRootPath));
        }

        return EnsureAbsolutePath(RuntimeCacheRootPath, nameof(GodotYamlConfigLoaderOptions.RuntimeCacheRootPath));
    }

    private bool UsesSourceDirectoryDirectly(string sourceRootPath)
    {
        if (!sourceRootPath.IsGodotPath())
        {
            return true;
        }

        if (sourceRootPath.IsUserPath())
        {
            return true;
        }

        return sourceRootPath.IsResPath() && _environment.IsEditor();
    }

    private void SynchronizeRuntimeCache(CancellationToken cancellationToken)
    {
        foreach (var group in _options.TableSources
                     .GroupBy(static source => NormalizeRelativePath(source.ConfigRelativePath),
                         StringComparer.Ordinal)
                     // Parent directories must be reset before children, otherwise resetting "a" later
                     // would erase files that were already synchronized into "a/b" during the same pass.
                     .OrderBy(static group => CountPathDepth(group.Key))
                     .ThenBy(static group => group.Key, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var representative = group.First();
            var sourceDirectoryPath = CombinePath(SourceRootPath, representative.ConfigRelativePath);
            var targetDirectoryPath = CombineAbsolutePath(LoaderRootPath, representative.ConfigRelativePath);

            ResetDirectory(representative.TableName, sourceDirectoryPath, targetDirectoryPath);
            CopyYamlFilesInDirectory(
                representative.TableName,
                sourceDirectoryPath,
                targetDirectoryPath,
                cancellationToken);
        }

        foreach (var group in _options.TableSources
                     .Where(static source => !string.IsNullOrEmpty(source.SchemaRelativePath))
                     .GroupBy(static source => NormalizeRelativePath(source.SchemaRelativePath!),
                         StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var representative = group.First();
            var sourceSchemaPath = CombinePath(SourceRootPath, representative.SchemaRelativePath!);
            var targetSchemaPath = CombineAbsolutePath(LoaderRootPath, representative.SchemaRelativePath!);

            CopySingleFile(
                representative.TableName,
                sourceSchemaPath,
                targetSchemaPath,
                ConfigLoadFailureKind.SchemaFileNotFound,
                ConfigLoadFailureKind.SchemaReadFailed);
        }
    }

    private void CopyYamlFilesInDirectory(
        string tableName,
        string sourceDirectoryPath,
        string targetDirectoryPath,
        CancellationToken cancellationToken)
    {
        var entries = _environment.EnumerateDirectory(sourceDirectoryPath);
        if (entries == null)
        {
            throw CreateConfigLoadException(
                ConfigLoadFailureKind.ConfigDirectoryNotFound,
                tableName,
                $"Config directory '{DescribePath(sourceDirectoryPath)}' was not found while preparing the Godot runtime cache.",
                configDirectoryPath: DescribePath(sourceDirectoryPath));
        }

        foreach (var entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entry.IsDirectory || entry.Name is "." or ".." || entry.Name.StartsWith(".", StringComparison.Ordinal))
            {
                continue;
            }

            if (!IsYamlFile(entry.Name))
            {
                continue;
            }

            var sourceFilePath = CombinePath(sourceDirectoryPath, entry.Name);
            var targetFilePath = Path.Combine(targetDirectoryPath, entry.Name);
            CopySingleFile(
                tableName,
                sourceFilePath,
                targetFilePath,
                ConfigLoadFailureKind.ConfigFileReadFailed,
                ConfigLoadFailureKind.ConfigFileReadFailed,
                configDirectoryPath: DescribePath(sourceDirectoryPath),
                yamlPath: DescribePath(sourceFilePath));
        }
    }

    private void CopySingleFile(
        string tableName,
        string sourceFilePath,
        string targetAbsolutePath,
        ConfigLoadFailureKind missingFailureKind,
        ConfigLoadFailureKind readFailureKind,
        string? configDirectoryPath = null,
        string? yamlPath = null)
    {
        if (!_environment.FileExists(sourceFilePath))
        {
            var missingMessage = missingFailureKind == ConfigLoadFailureKind.SchemaFileNotFound
                ? $"Schema file '{DescribePath(sourceFilePath)}' was not found while preparing the Godot runtime cache."
                : $"Config file '{DescribePath(sourceFilePath)}' was not found while preparing the Godot runtime cache.";

            throw CreateConfigLoadException(
                missingFailureKind,
                tableName,
                missingMessage,
                configDirectoryPath: configDirectoryPath,
                yamlPath: missingFailureKind == ConfigLoadFailureKind.SchemaFileNotFound
                    ? null
                    : yamlPath ?? DescribePath(sourceFilePath),
                schemaPath: missingFailureKind == ConfigLoadFailureKind.SchemaFileNotFound
                    ? DescribePath(sourceFilePath)
                    : null);
        }

        try
        {
            var parentDirectory = Path.GetDirectoryName(targetAbsolutePath);
            if (!string.IsNullOrWhiteSpace(parentDirectory))
            {
                Directory.CreateDirectory(parentDirectory);
            }

            File.WriteAllBytes(targetAbsolutePath, _environment.ReadAllBytes(sourceFilePath));
        }
        catch (Exception exception)
        {
            var readMessage = readFailureKind == ConfigLoadFailureKind.SchemaReadFailed
                ? $"Failed to copy schema file '{DescribePath(sourceFilePath)}' into the Godot runtime cache."
                : $"Failed to copy config file '{DescribePath(sourceFilePath)}' into the Godot runtime cache.";

            throw CreateConfigLoadException(
                readFailureKind,
                tableName,
                readMessage,
                configDirectoryPath: configDirectoryPath,
                yamlPath: readFailureKind == ConfigLoadFailureKind.SchemaReadFailed
                    ? null
                    : yamlPath ?? DescribePath(sourceFilePath),
                schemaPath: readFailureKind == ConfigLoadFailureKind.SchemaReadFailed
                    ? DescribePath(sourceFilePath)
                    : null,
                innerException: exception);
        }
    }

    private void ResetDirectory(string tableName, string sourceDirectoryPath, string targetDirectoryPath)
    {
        try
        {
            if (Directory.Exists(targetDirectoryPath))
            {
                Directory.Delete(targetDirectoryPath, recursive: true);
            }

            Directory.CreateDirectory(targetDirectoryPath);
        }
        catch (Exception exception)
        {
            var describedSourceDirectoryPath = DescribePath(sourceDirectoryPath);
            throw CreateConfigLoadException(
                ConfigLoadFailureKind.ConfigFileReadFailed,
                tableName,
                $"Failed to reset runtime cache directory '{targetDirectoryPath}' while preparing config directory '{describedSourceDirectoryPath}'.",
                configDirectoryPath: describedSourceDirectoryPath,
                detail: $"Runtime cache directory: {targetDirectoryPath}.",
                innerException: exception);
        }
    }

    private string EnsureAbsolutePath(string path, string optionName)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path cannot be null or whitespace.", optionName);
        }

        if (path.IsGodotPath())
        {
            var absolutePath = _environment.GlobalizePath(path);
            if (string.IsNullOrWhiteSpace(absolutePath))
            {
                throw new InvalidOperationException(
                    $"Path option '{optionName}' resolved to an empty absolute path. Value='{path}'.");
            }

            return absolutePath;
        }

        return Path.GetFullPath(path);
    }

    private string DescribePath(string path)
    {
        if (path.IsGodotPath())
        {
            var absolutePath = _environment.GlobalizePath(path);
            return string.IsNullOrWhiteSpace(absolutePath) ? path : absolutePath;
        }

        return Path.GetFullPath(path);
    }

    private static string CombinePath(string rootPath, string relativePath)
    {
        var normalizedRelativePath = NormalizeRelativePath(relativePath);
        if (rootPath.IsGodotPath())
        {
            if (rootPath.EndsWith("://", StringComparison.Ordinal))
            {
                return $"{rootPath}{normalizedRelativePath}";
            }

            return $"{rootPath.TrimEnd('/')}/{normalizedRelativePath}";
        }

        return Path.Combine(rootPath, normalizedRelativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private static string CombineAbsolutePath(string rootPath, string relativePath)
    {
        return Path.Combine(rootPath, NormalizeRelativePath(relativePath).Replace('/', Path.DirectorySeparatorChar));
    }

    private static string NormalizeRelativePath(string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

        var normalizedPath = relativePath.Replace('\\', '/').Trim();
        if (normalizedPath.StartsWith("/", StringComparison.Ordinal) ||
            normalizedPath.StartsWith("res://", StringComparison.Ordinal) ||
            normalizedPath.StartsWith("user://", StringComparison.Ordinal) ||
            Path.IsPathRooted(normalizedPath) ||
            HasWindowsDrivePrefix(normalizedPath))
        {
            throw new ArgumentException("Relative path must be an unrooted path.", nameof(relativePath));
        }

        // Reject ':' in later segments as well so Windows-invalid names and ADS-like syntax never reach file APIs.
        if (normalizedPath.Contains(':', StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Relative path must not contain ':' characters.",
                nameof(relativePath));
        }

        var segments = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Any(static segment => segment is "." or ".."))
        {
            throw new ArgumentException(
                "Relative path must not contain '.' or '..' segments.",
                nameof(relativePath));
        }

        return string.Join('/', segments);
    }

    private static int CountPathDepth(string normalizedRelativePath)
    {
        return normalizedRelativePath.Count(static ch => ch == '/');
    }

    private static bool HasWindowsDrivePrefix(string path)
    {
        return path.Length >= 2 &&
               char.IsLetter(path[0]) &&
               path[1] == ':';
    }

    private static bool IsYamlFile(string fileName)
    {
        return fileName.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) ||
               fileName.EndsWith(".yml", StringComparison.OrdinalIgnoreCase);
    }

    private static ConfigLoadException CreateConfigLoadException(
        ConfigLoadFailureKind failureKind,
        string tableName,
        string message,
        string? configDirectoryPath = null,
        string? yamlPath = null,
        string? schemaPath = null,
        string? detail = null,
        Exception? innerException = null)
    {
        return new ConfigLoadException(
            new ConfigLoadDiagnostic(
                failureKind,
                tableName,
                configDirectoryPath: configDirectoryPath,
                yamlPath: yamlPath,
                schemaPath: schemaPath,
                detail: detail),
            message,
            innerException);
    }
}

/// <summary>
///     抽象 <see cref="GodotYamlConfigLoader" /> 与具体宿主环境之间的 Godot 路径和文件访问边界。
/// </summary>
/// <remarks>
///     该抽象存在的原因，是编辑器态与导出态对 <c>res://</c>、<c>user://</c> 的访问方式不同：
///     编辑器态通常可以把 Godot 特殊路径全局化后直接落到普通文件系统，而导出态往往只能通过 Godot API 读取原始文本资源，
///     再把它们复制到运行时缓存目录。<see cref="EnumerateDirectory" /> 在目录不存在或当前环境无法枚举时必须返回
///     <see langword="null" />，用来表达“不可访问”而不是抛出未找到异常；<see cref="ReadAllBytes" /> 则应保留底层读取失败异常，
///     交由加载器包装成配置诊断。对于普通文件系统路径，应遵循 <see cref="Directory" /> / <see cref="File" /> 语义；
///     对于 Godot 特殊路径，则应使用引擎提供的路径解析和读取能力。
/// </remarks>
internal sealed class GodotYamlConfigEnvironment
{
    /// <summary>
    ///     初始化一个可替换的 Godot YAML 配置宿主环境抽象。
    /// </summary>
    /// <param name="isEditor">返回当前进程是否处于 Godot 编辑器态的委托。</param>
    /// <param name="globalizePath">
    ///     把 Godot 特殊路径转换为普通绝对路径的委托。
    ///     当前加载器仅会在输入为 <c>res://</c> 或 <c>user://</c> 时调用它，返回值必须为非空绝对路径。
    /// </param>
    /// <param name="enumerateDirectory">
    ///     枚举指定目录直接子项的委托。
    ///     当目录不存在、无法访问或当前环境无法枚举该路径时，必须返回 <see langword="null" />。
    /// </param>
    /// <param name="fileExists">
    ///     检查指定路径上的文件是否存在的委托。
    ///     输入既可能是 Godot 特殊路径，也可能是普通绝对路径。
    /// </param>
    /// <param name="readAllBytes">
    ///     读取指定文件完整字节内容的委托。
    ///     当文件缺失或读取失败时，应抛出底层异常，由加载器统一包装为配置加载诊断。
    /// </param>
    /// <exception cref="ArgumentNullException">任一委托参数为 <see langword="null" /> 时抛出。</exception>
    public GodotYamlConfigEnvironment(
        Func<bool> isEditor,
        Func<string, string> globalizePath,
        Func<string, IReadOnlyList<GodotYamlConfigDirectoryEntry>?> enumerateDirectory,
        Func<string, bool> fileExists,
        Func<string, byte[]> readAllBytes)
    {
        IsEditor = isEditor ?? throw new ArgumentNullException(nameof(isEditor));
        GlobalizePath = globalizePath ?? throw new ArgumentNullException(nameof(globalizePath));
        EnumerateDirectory = enumerateDirectory ?? throw new ArgumentNullException(nameof(enumerateDirectory));
        FileExists = fileExists ?? throw new ArgumentNullException(nameof(fileExists));
        ReadAllBytes = readAllBytes ?? throw new ArgumentNullException(nameof(readAllBytes));
    }

    /// <summary>
    ///     获取默认的 Godot 运行时环境实现。
    /// </summary>
    /// <remarks>
    ///     默认实现使用 <see cref="OS.HasFeature(string)" /> 检测编辑器态，
    ///     使用 <see cref="ProjectSettings.GlobalizePath(string)" /> 处理 Godot 特殊路径，
    ///     并在 Godot 路径与普通路径之间切换对应的枚举和读取 API。
    /// </remarks>
    public static GodotYamlConfigEnvironment Default { get; } = new(
        static () => OS.HasFeature("editor"),
        static path => ProjectSettings.GlobalizePath(path),
        EnumerateDirectoryCore,
        FileExistsCore,
        ReadAllBytesCore);

    /// <summary>
    ///     获取用于判断当前进程是否处于编辑器态的委托。
    /// </summary>
    public Func<bool> IsEditor { get; }

    /// <summary>
    ///     获取把 Godot 特殊路径转换为普通绝对路径的委托。
    /// </summary>
    /// <remarks>
    ///     当前加载器只会对 <c>res://</c> 和 <c>user://</c> 路径调用该委托。
    ///     返回空字符串会被视为无效环境实现，并在后续路径解析阶段触发异常。
    /// </remarks>
    public Func<string, string> GlobalizePath { get; }

    /// <summary>
    ///     获取用于枚举目录直接子项的委托。
    /// </summary>
    /// <remarks>
    ///     当目录不存在、无法访问，或当前环境无法枚举给定路径时，该委托必须返回 <see langword="null" />。
    ///     返回的集合只应包含当前目录下的直接子项，调用方会自行过滤隐藏项、子目录与非 YAML 文件。
    /// </remarks>
    public Func<string, IReadOnlyList<GodotYamlConfigDirectoryEntry>?> EnumerateDirectory { get; }

    /// <summary>
    ///     获取用于检查文件是否存在的委托。
    /// </summary>
    public Func<string, bool> FileExists { get; }

    /// <summary>
    ///     获取用于读取文件完整字节内容的委托。
    /// </summary>
    /// <remarks>
    ///     该委托在路径不存在、权限不足或 I/O 失败时应抛出底层异常，以便加载器保留失败原因并生成诊断信息。
    /// </remarks>
    public Func<string, byte[]> ReadAllBytes { get; }

    private static IReadOnlyList<GodotYamlConfigDirectoryEntry>? EnumerateDirectoryCore(string path)
    {
        if (!path.IsGodotPath())
        {
            if (!Directory.Exists(path))
            {
                return null;
            }

            return Directory
                .EnumerateFileSystemEntries(path, "*", SearchOption.TopDirectoryOnly)
                .Select(static entryPath => new GodotYamlConfigDirectoryEntry(
                    Path.GetFileName(entryPath),
                    Directory.Exists(entryPath)))
                .ToArray();
        }

        using var directory = DirAccess.Open(path);
        if (directory == null)
        {
            return null;
        }

        var entries = new List<GodotYamlConfigDirectoryEntry>();
        var listDirectoryError = directory.ListDirBegin();
        if (listDirectoryError != Error.Ok)
        {
            return null;
        }

        while (true)
        {
            var name = directory.GetNext();
            if (string.IsNullOrEmpty(name))
            {
                break;
            }

            entries.Add(new GodotYamlConfigDirectoryEntry(name, directory.CurrentIsDir()));
        }

        directory.ListDirEnd();
        return entries;
    }

    private static bool FileExistsCore(string path)
    {
        return path.IsGodotPath()
            ? FileAccess.FileExists(path)
            : File.Exists(path);
    }

    private static byte[] ReadAllBytesCore(string path)
    {
        return path.IsGodotPath()
            ? FileAccess.GetFileAsBytes(path)
            : File.ReadAllBytes(path);
    }
}

/// <summary>
///     描述一次目录枚举返回的单个子项。
/// </summary>
/// <remarks>
///     该结构只承载目录扫描阶段需要的最小信息。
///     <see cref="Name" /> 必须是单个目录项名称，而不是包含父目录的完整路径；
///     对于 Godot 路径和普通路径都遵循相同约定，便于加载器统一做后续拼接与过滤。
/// </remarks>
internal readonly record struct GodotYamlConfigDirectoryEntry
{
    /// <summary>
    ///     初始化一个目录枚举结果项。
    /// </summary>
    /// <param name="name">当前目录项的名称，不包含父目录路径。</param>
    /// <param name="isDirectory">指示该目录项是否为子目录。</param>
    public GodotYamlConfigDirectoryEntry(string name, bool isDirectory)
    {
        Name = name;
        IsDirectory = isDirectory;
    }

    /// <summary>
    ///     获取当前目录项的名称，不包含父目录路径。
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     获取一个值，指示当前目录项是否为子目录。
    /// </summary>
    public bool IsDirectory { get; }
}
