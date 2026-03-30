using System.IO;
using GFramework.Game.Config;

namespace GFramework.Game.Tests.Config;

/// <summary>
///     验证 YAML 配置加载器的目录扫描与注册行为。
/// </summary>
[TestFixture]
public class YamlConfigLoaderTests
{
    /// <summary>
    ///     为每个测试创建独立临时目录，避免文件系统状态互相污染。
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        _rootPath = Path.Combine(Path.GetTempPath(), "GFramework.ConfigTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_rootPath);
    }

    /// <summary>
    ///     清理测试期间创建的临时目录。
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, true);
        }
    }

    private string _rootPath = null!;

    /// <summary>
    ///     验证加载器能够扫描 YAML 文件并将结果写入注册表。
    /// </summary>
    [Test]
    public async Task LoadAsync_Should_Register_Table_From_Yaml_Files()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            name: Slime
            hp: 10
            """);
        CreateConfigFile(
            "monster/goblin.yml",
            """
            id: 2
            name: Goblin
            hp: 30
            """);

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterConfigStub>("monster", "monster", static config => config.Id);
        var registry = new ConfigRegistry();

        await loader.LoadAsync(registry);

        var table = registry.GetTable<int, MonsterConfigStub>("monster");

        Assert.Multiple(() =>
        {
            Assert.That(table.Count, Is.EqualTo(2));
            Assert.That(table.Get(1).Name, Is.EqualTo("Slime"));
            Assert.That(table.Get(2).Hp, Is.EqualTo(30));
        });
    }

    /// <summary>
    ///     验证注册的配置目录不存在时会抛出清晰错误。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_Config_Directory_Does_Not_Exist()
    {
        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterConfigStub>("monster", "monster", static config => config.Id);
        var registry = new ConfigRegistry();

        var exception = Assert.ThrowsAsync<DirectoryNotFoundException>(async () => await loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Message, Does.Contain("monster"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证某个配置表加载失败时，注册表不会留下部分成功的中间状态。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Not_Mutate_Registry_When_A_Later_Table_Fails()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            name: Slime
            hp: 10
            """);

        var registry = new ConfigRegistry();
        registry.RegisterTable(
            "existing",
            new InMemoryConfigTable<int, ExistingConfigStub>(
                new[]
                {
                    new ExistingConfigStub(100, "Original")
                },
                static config => config.Id));

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterConfigStub>("monster", "monster", static config => config.Id)
            .RegisterTable<int, MonsterConfigStub>("broken", "broken", static config => config.Id);

        Assert.ThrowsAsync<DirectoryNotFoundException>(async () => await loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(registry.Count, Is.EqualTo(1));
            Assert.That(registry.HasTable("monster"), Is.False);
            Assert.That(registry.GetTable<int, ExistingConfigStub>("existing").Get(100).Name, Is.EqualTo("Original"));
        });
    }

    /// <summary>
    ///     验证非法 YAML 会被包装成带文件路径的反序列化错误。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_With_File_Path_When_Yaml_Is_Invalid()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: [1
            name: Slime
            """);

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterConfigStub>("monster", "monster", static config => config.Id);
        var registry = new ConfigRegistry();

        var exception = Assert.ThrowsAsync<InvalidOperationException>(async () => await loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Message, Does.Contain("slime.yaml"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     创建测试用配置文件。
    /// </summary>
    /// <param name="relativePath">相对根目录的文件路径。</param>
    /// <param name="content">文件内容。</param>
    private void CreateConfigFile(string relativePath, string content)
    {
        var fullPath = Path.Combine(_rootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(fullPath, content);
    }

    /// <summary>
    ///     用于 YAML 加载测试的最小怪物配置类型。
    /// </summary>
    private sealed class MonsterConfigStub
    {
        /// <summary>
        ///     获取或设置主键。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        ///     获取或设置名称。
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        ///     获取或设置生命值。
        /// </summary>
        public int Hp { get; set; }
    }

    /// <summary>
    ///     用于验证注册表一致性的现有配置类型。
    /// </summary>
    /// <param name="Id">配置主键。</param>
    /// <param name="Name">配置名称。</param>
    private sealed record ExistingConfigStub(int Id, string Name);
}