using System.IO;
using GFramework.Core.Abstractions.Events;
using GFramework.Game.Abstractions.Config;
using GFramework.Game.Config;
using GFramework.Godot.Extensions;

namespace GFramework.Godot.Config;

/// <summary>
///     为 Godot 运行时提供 YAML 配置加载适配层。
///     编辑器态优先直接把项目目录交给 <see cref="YamlConfigLoader" />，
///     导出态则把显式声明的 YAML 与 schema 文本同步到运行时缓存目录后再加载。
/// </summary>
public sealed class GodotYamlConfigLoader : IConfigLoader
{
    private readonly GodotYamlConfigEnvironment _environment;
    private readonly YamlConfigLoader _loader;
    private readonly GodotYamlConfigLoaderOptions _options;

    /// <summary>
    ///     使用指定选项创建一个 Godot YAML 配置加载器。
    /// </summary>
    /// <param name="options">加载器初始化选项。</param>
    public GodotYamlConfigLoader(GodotYamlConfigLoaderOptions options)
        : this(options, GodotYamlConfigEnvironment.Default)
    {
    }

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
        _loader = new YamlConfigLoader(LoaderRootPath);
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
    public YamlConfigLoader Loader => _loader;

    /// <summary>
    ///     获取一个值，指示当前实例是否可直接针对源目录启用热重载。
    /// </summary>
    public bool CanEnableHotReload => UsesSourceDirectoryDirectly(SourceRootPath);

    /// <inheritdoc />
    public async Task LoadAsync(IConfigRegistry registry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(registry);

        if (!CanEnableHotReload)
        {
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
    public IUnRegister EnableHotReload(
        IConfigRegistry registry,
        YamlConfigHotReloadOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(registry);

        if (!CanEnableHotReload)
        {
            throw new InvalidOperationException(
                "Hot reload is only available when the source root can be accessed as a normal filesystem directory.");
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
                         StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var representative = group.First();
            var sourceDirectoryPath = CombinePath(SourceRootPath, representative.ConfigRelativePath);
            var targetDirectoryPath = CombineAbsolutePath(LoaderRootPath, representative.ConfigRelativePath);

            ResetDirectory(targetDirectoryPath);
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

        Directory.CreateDirectory(targetDirectoryPath);

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

    private void ResetDirectory(string directoryPath)
    {
        if (Directory.Exists(directoryPath))
        {
            Directory.Delete(directoryPath, recursive: true);
        }

        Directory.CreateDirectory(directoryPath);
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
        return relativePath.Replace('\\', '/').TrimStart('/');
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
        Exception? innerException = null)
    {
        return new ConfigLoadException(
            new ConfigLoadDiagnostic(
                failureKind,
                tableName,
                configDirectoryPath: configDirectoryPath,
                yamlPath: yamlPath,
                schemaPath: schemaPath),
            message,
            innerException);
    }
}

internal sealed class GodotYamlConfigEnvironment
{
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

    public static GodotYamlConfigEnvironment Default { get; } = new(
        static () => OS.HasFeature("editor"),
        static path => ProjectSettings.GlobalizePath(path),
        EnumerateDirectoryCore,
        FileExistsCore,
        ReadAllBytesCore);

    public Func<bool> IsEditor { get; }

    public Func<string, string> GlobalizePath { get; }

    public Func<string, IReadOnlyList<GodotYamlConfigDirectoryEntry>?> EnumerateDirectory { get; }

    public Func<string, bool> FileExists { get; }

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
        directory.ListDirBegin();
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

internal readonly record struct GodotYamlConfigDirectoryEntry(
    string Name,
    bool IsDirectory);
