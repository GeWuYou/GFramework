using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GFramework.Game.Config;
using GFramework.Godot.Config;
using NUnit.Framework;

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
    ///     创建一个基于临时目录映射的 Godot YAML 配置加载器。
    /// </summary>
    /// <param name="isEditor">是否模拟编辑器环境。</param>
    /// <returns>已配置好的加载器实例。</returns>
    private GodotYamlConfigLoader CreateLoader(bool isEditor)
    {
        return new GodotYamlConfigLoader(
            new GodotYamlConfigLoaderOptions
            {
                SourceRootPath = "res://",
                RuntimeCacheRootPath = "user://config_cache",
                TableSources =
                [
                    new GodotYamlConfigTableSource(
                        "monster",
                        "monster",
                        "schemas/monster.schema.json")
                ],
                ConfigureLoader = static loader =>
                    loader.RegisterTable<int, MonsterConfigStub>(
                        "monster",
                        "monster",
                        "schemas/monster.schema.json",
                        static config => config.Id)
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
