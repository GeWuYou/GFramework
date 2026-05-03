// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using System.Runtime.CompilerServices;
using GFramework.Game.Abstractions.Config;
using GFramework.Game.Config;
using GFramework.Godot.Config;

namespace GFramework.Godot.Tests.Config;

/// <summary>
///     验证 Godot YAML 配置加载器能够在编辑器态直读项目目录，并在导出态同步运行时缓存。
/// </summary>
[TestFixture]
public sealed class GodotYamlConfigLoaderTests
{
    /// <summary>
    ///     为每个测试准备独立的资源根目录与用户目录。
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        _testRoot = Path.Combine(
            Path.GetTempPath(),
            "GFramework.GodotYamlConfigLoaderTests",
            Guid.NewGuid().ToString("N"));
        _resourceRoot = Path.Combine(_testRoot, "res-root");
        _userRoot = Path.Combine(_testRoot, "user-root");
        Directory.CreateDirectory(_resourceRoot);
        Directory.CreateDirectory(_userRoot);
    }

    /// <summary>
    ///     清理测试期间创建的临时目录。
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, true);
        }
    }

    private string _resourceRoot = null!;
    private string _testRoot = null!;
    private string _userRoot = null!;

    /// <summary>
    ///     验证导出态会把注册过的 YAML 与 schema 文本同步到运行时缓存，再交给底层加载器。
    /// </summary>
    [Test]
    public async Task LoadAsync_Should_Copy_Registered_Text_Assets_Into_Runtime_Cache_When_Source_Is_Res_Path()
    {
        CreateMonsterFiles(_resourceRoot);

        var loader = CreateLoader(isEditor: false);
        var registry = new ConfigRegistry();

        await loader.LoadAsync(registry);

        var table = registry.GetTable<int, MonsterConfigStub>("monster");
        var cacheRoot = Path.Combine(_userRoot, "config_cache");

        Assert.Multiple(() =>
        {
            Assert.That(loader.CanEnableHotReload, Is.False);
            Assert.That(loader.LoaderRootPath, Is.EqualTo(cacheRoot));
            Assert.That(table.Count, Is.EqualTo(2));
            Assert.That(table.Get(1).Name, Is.EqualTo("Slime"));
            Assert.That(File.Exists(Path.Combine(cacheRoot, "monster", "slime.yaml")), Is.True);
            Assert.That(File.Exists(Path.Combine(cacheRoot, "monster", "goblin.yml")), Is.True);
            Assert.That(File.Exists(Path.Combine(cacheRoot, "schemas", "monster.schema.json")), Is.True);
            Assert.That(File.Exists(Path.Combine(cacheRoot, "monster", "notes.txt")), Is.False);
            Assert.That(Directory.Exists(Path.Combine(cacheRoot, "monster", "nested")), Is.False);
        });
    }

    /// <summary>
    ///     验证编辑器态会直接使用全局化后的项目目录，而不会额外创建运行时缓存副本。
    /// </summary>
    [Test]
    public async Task LoadAsync_Should_Use_Globalized_Res_Directory_Directly_When_Running_In_Editor()
    {
        CreateMonsterFiles(_resourceRoot);

        var loader = CreateLoader(isEditor: true);
        var registry = new ConfigRegistry();

        await loader.LoadAsync(registry);

        var table = registry.GetTable<int, MonsterConfigStub>("monster");

        Assert.Multiple(() =>
        {
            Assert.That(loader.CanEnableHotReload, Is.True);
            Assert.That(loader.LoaderRootPath, Is.EqualTo(_resourceRoot));
            Assert.That(table.Count, Is.EqualTo(2));
            Assert.That(table.Get(2).Hp, Is.EqualTo(30));
            Assert.That(Directory.Exists(Path.Combine(_userRoot, "config_cache")), Is.False);
        });
    }

    /// <summary>
    ///     验证当实例必须依赖运行时缓存时，不允许再直接启用底层文件热重载。
    /// </summary>
    [Test]
    public void EnableHotReload_Should_Throw_When_Source_Root_Cannot_Be_Used_Directly()
    {
        var loader = CreateLoader(isEditor: false);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            loader.EnableHotReload(new ConfigRegistry()));

        Assert.That(exception!.Message, Does.Contain("Hot reload"));
    }

    /// <summary>
    ///     验证即使调用方拿到底层加载器实例，也不能绕过 Godot 适配层施加的热重载守卫。
    /// </summary>
    [Test]
    public void Loader_EnableHotReload_Should_Still_Respect_Godot_HotReload_Guard()
    {
        var loader = CreateLoader(isEditor: false);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            loader.Loader.EnableHotReload(new ConfigRegistry()));

        Assert.That(exception!.Message, Does.Contain("Hot reload"));
    }

    /// <summary>
    ///     验证导出态会按父目录优先同步缓存，避免父目录重置删掉先前复制到子目录的内容。
    /// </summary>
    [Test]
    public async Task LoadAsync_Should_Synchronize_Parent_Directories_Before_Children()
    {
        WriteFile(
            _resourceRoot,
            "monster/slime.yaml",
            """
            id: 1
            name: Slime
            hp: 10
            """);
        WriteFile(
            _resourceRoot,
            "monster/boss/dragon.yaml",
            """
            id: 99
            name: Dragon
            hp: 500
            """);

        var loader = CreateLoader(
            isEditor: false,
            tableSources:
            [
                new GodotYamlConfigTableSource("boss", "monster/boss"),
                new GodotYamlConfigTableSource("monster", "monster")
            ],
            configureLoader: loader =>
            {
                loader.RegisterTable<int, MonsterConfigStub>(
                    "boss",
                    "monster/boss",
                    keySelector: static config => config.Id);
                loader.RegisterTable<int, MonsterConfigStub>(
                    "monster",
                    "monster",
                    keySelector: static config => config.Id);
            });
        var registry = new ConfigRegistry();

        await loader.LoadAsync(registry);

        var cacheRoot = Path.Combine(_userRoot, "config_cache");
        var bossTable = registry.GetTable<int, MonsterConfigStub>("boss");
        var monsterTable = registry.GetTable<int, MonsterConfigStub>("monster");

        Assert.Multiple(() =>
        {
            Assert.That(monsterTable.Count, Is.EqualTo(1));
            Assert.That(bossTable.Count, Is.EqualTo(1));
            Assert.That(bossTable.Get(99).Name, Is.EqualTo("Dragon"));
            Assert.That(File.Exists(Path.Combine(cacheRoot, "monster", "boss", "dragon.yaml")), Is.True);
        });
    }

    /// <summary>
    ///     验证运行时缓存目录无法重置时，Godot 适配层仍会返回结构化的配置加载诊断。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Wrap_Runtime_Cache_Directory_Reset_Failure_As_ConfigLoadException()
    {
        CreateMonsterFiles(_resourceRoot);
        WriteFile(_userRoot, "config_cache", "occupied");

        var loader = CreateLoader(isEditor: false);

        var exception = Assert.ThrowsAsync<ConfigLoadException>(async () =>
            await loader.LoadAsync(new ConfigRegistry()).ConfigureAwait(false));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.ConfigFileReadFailed));
            Assert.That(exception.Diagnostic.TableName, Is.EqualTo("monster"));
            Assert.That(exception.Diagnostic.ConfigDirectoryPath, Is.EqualTo(Path.Combine(_resourceRoot, "monster")));
            Assert.That(exception.Diagnostic.Detail, Does.Contain(Path.Combine(_userRoot, "config_cache", "monster")));
            Assert.That(exception.InnerException, Is.InstanceOf<IOException>());
        });
    }

    /// <summary>
    ///     验证加载器自身会拒绝可能逃逸缓存根目录的非法配置目录路径，即使调用方绕过了公开构造约束。
    /// </summary>
    [TestCase("../outside")]
    [TestCase("schemas:bad/monster")]
    public void LoadAsync_Should_Reject_Invalid_Config_Relative_Path_When_Metadata_Is_Corrupted(
        string configRelativePath)
    {
        var corruptedSource = CreateUnsafeTableSource("monster", configRelativePath);
        var loader = CreateLoader(
            isEditor: false,
            tableSources: [corruptedSource],
            configureLoader: static _ => { });

        var exception = Assert.ThrowsAsync<ArgumentException>(async () =>
            await loader.LoadAsync(new ConfigRegistry()).ConfigureAwait(false));

        Assert.That(exception!.ParamName, Is.EqualTo("relativePath"));
    }

    /// <summary>
    ///     验证加载器自身会拒绝可能逃逸缓存根目录的非法 schema 路径，即使调用方绕过了公开构造约束。
    /// </summary>
    [TestCase("../schemas/monster.schema.json")]
    [TestCase("schemas:bad/monster.schema.json")]
    public void LoadAsync_Should_Reject_Invalid_Schema_Relative_Path_When_Metadata_Is_Corrupted(
        string schemaRelativePath)
    {
        WriteFile(
            _resourceRoot,
            "monster/slime.yaml",
            """
            id: 1
            name: Slime
            hp: 10
            """);

        var corruptedSource = CreateUnsafeTableSource("monster", "monster", schemaRelativePath);
        var loader = CreateLoader(
            isEditor: false,
            tableSources: [corruptedSource],
            configureLoader: static _ => { });

        var exception = Assert.ThrowsAsync<ArgumentException>(async () =>
            await loader.LoadAsync(new ConfigRegistry()).ConfigureAwait(false));

        Assert.That(exception!.ParamName, Is.EqualTo("relativePath"));
    }

    /// <summary>
    ///     创建一个基于临时目录映射的 Godot YAML 配置加载器。
    /// </summary>
    /// <param name="isEditor">是否模拟编辑器环境。</param>
    /// <param name="tableSources">要同步的配置表来源集合；为空时使用默认 monster 表。</param>
    /// <param name="configureLoader">底层 YAML 加载器注册逻辑；为空时使用默认 monster 表注册。</param>
    /// <returns>已配置好的加载器实例。</returns>
    private GodotYamlConfigLoader CreateLoader(
        bool isEditor,
        IReadOnlyCollection<GodotYamlConfigTableSource>? tableSources = null,
        Action<YamlConfigLoader>? configureLoader = null)
    {
        return new GodotYamlConfigLoader(
            new GodotYamlConfigLoaderOptions
            {
                SourceRootPath = "res://",
                RuntimeCacheRootPath = "user://config_cache",
                TableSources = tableSources ??
                [
                    new GodotYamlConfigTableSource(
                        "monster",
                        "monster",
                        "schemas/monster.schema.json")
                ],
                ConfigureLoader = configureLoader ??
                                  (static loader =>
                                      loader.RegisterTable<int, MonsterConfigStub>(
                                          "monster",
                                          "monster",
                                          "schemas/monster.schema.json",
                                          static config => config.Id))
            },
            CreateEnvironment(isEditor));
    }

    /// <summary>
    ///     创建一个把 <c>res://</c> 与 <c>user://</c> 映射到临时目录的测试环境。
    /// </summary>
    /// <param name="isEditor">是否模拟编辑器环境。</param>
    /// <returns>测试专用环境对象。</returns>
    private GodotYamlConfigEnvironment CreateEnvironment(bool isEditor)
    {
        return new GodotYamlConfigEnvironment(
            () => isEditor,
            path => MapGodotPath(path),
            path =>
            {
                var absolutePath = MapGodotPath(path);
                if (!Directory.Exists(absolutePath))
                {
                    return null;
                }

                return Directory
                    .EnumerateFileSystemEntries(absolutePath, "*", SearchOption.TopDirectoryOnly)
                    .Select(static entryPath => new GodotYamlConfigDirectoryEntry(
                        Path.GetFileName(entryPath),
                        Directory.Exists(entryPath)))
                    .ToArray();
            },
            path => File.Exists(MapGodotPath(path)),
            path => File.ReadAllBytes(MapGodotPath(path)));
    }

    /// <summary>
    ///     创建一组最小可运行的 monster YAML 与 schema 文件。
    /// </summary>
    /// <param name="rootPath">目标根目录。</param>
    private static void CreateMonsterFiles(string rootPath)
    {
        WriteFile(
            rootPath,
            "schemas/monster.schema.json",
            """
            {
              "type": "object",
              "required": ["id", "name", "hp"],
              "properties": {
                "id": { "type": "integer" },
                "name": { "type": "string" },
                "hp": { "type": "integer" }
              }
            }
            """);
        WriteFile(
            rootPath,
            "monster/slime.yaml",
            """
            id: 1
            name: Slime
            hp: 10
            """);
        WriteFile(
            rootPath,
            "monster/goblin.yml",
            """
            id: 2
            name: Goblin
            hp: 30
            """);
        WriteFile(
            rootPath,
            "monster/notes.txt",
            "ignored");
        WriteFile(
            rootPath,
            "monster/nested/ghost.yaml",
            """
            id: 3
            name: Ghost
            hp: 99
            """);
    }

    /// <summary>
    ///     把逻辑相对路径写入指定根目录。
    /// </summary>
    /// <param name="rootPath">目标根目录。</param>
    /// <param name="relativePath">相对文件路径。</param>
    /// <param name="content">文件内容。</param>
    private static void WriteFile(string rootPath, string relativePath, string content)
    {
        var fullPath = Path.Combine(rootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var directoryPath = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        File.WriteAllText(fullPath, content);
    }

    /// <summary>
    ///     构造一个绕过公开构造校验的配置来源对象，用于验证加载器的防御式路径校验。
    /// </summary>
    /// <param name="tableName">伪造的表名称。</param>
    /// <param name="configRelativePath">伪造的配置目录路径。</param>
    /// <param name="schemaRelativePath">伪造的 schema 路径。</param>
    /// <returns>已写入指定字段值的未初始化对象。</returns>
    private static GodotYamlConfigTableSource CreateUnsafeTableSource(
        string tableName,
        string configRelativePath,
        string? schemaRelativePath = null)
    {
        var source =
            (GodotYamlConfigTableSource)RuntimeHelpers.GetUninitializedObject(typeof(GodotYamlConfigTableSource));
        SetAutoPropertyBackingField(source, nameof(GodotYamlConfigTableSource.TableName), tableName);
        SetAutoPropertyBackingField(source, nameof(GodotYamlConfigTableSource.ConfigRelativePath), configRelativePath);
        SetAutoPropertyBackingField(source, nameof(GodotYamlConfigTableSource.SchemaRelativePath), schemaRelativePath);
        return source;
    }

    /// <summary>
    ///     直接写入自动属性的编译器生成字段，用于构造损坏的测试对象。
    /// </summary>
    /// <typeparam name="TValue">字段值类型。</typeparam>
    /// <param name="instance">要写入字段的目标对象。</param>
    /// <param name="propertyName">对应的属性名称。</param>
    /// <param name="value">要写入的字段值。</param>
    private static void SetAutoPropertyBackingField<TValue>(
        object instance,
        string propertyName,
        TValue value)
    {
        var field = instance.GetType().GetField(
            $"<{propertyName}>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
        {
            throw new InvalidOperationException(
                $"Backing field for property '{propertyName}' was not found on type '{instance.GetType().FullName}'.");
        }

        field.SetValue(instance, value);
    }

    /// <summary>
    ///     将测试中的 Godot 路径映射到本地临时目录。
    /// </summary>
    /// <param name="path">Godot 路径或普通路径。</param>
    /// <returns>映射后的绝对路径。</returns>
    private string MapGodotPath(string path)
    {
        if (path.StartsWith("res://", StringComparison.Ordinal))
        {
            return Path.Combine(
                _resourceRoot,
                path["res://".Length..].Replace('/', Path.DirectorySeparatorChar));
        }

        if (path.StartsWith("user://", StringComparison.Ordinal))
        {
            return Path.Combine(
                _userRoot,
                path["user://".Length..].Replace('/', Path.DirectorySeparatorChar));
        }

        return path;
    }

    /// <summary>
    ///     最小 monster 配置桩类型。
    /// </summary>
    private sealed class MonsterConfigStub
    {
        /// <summary>
        ///     主键。
        /// </summary>
        public int Id { get; init; }

        /// <summary>
        ///     名称。
        /// </summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>
        ///     生命值。
        /// </summary>
        public int Hp { get; init; }
    }
}
