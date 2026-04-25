using System.IO;
using GFramework.Game.Abstractions.Config;
using GFramework.Game.Config;

namespace GFramework.Game.Tests.Config;

/// <summary>
///     验证 YAML 配置加载器对 <c>not</c> 约束的运行时行为。
/// </summary>
[TestFixture]
public sealed class YamlConfigLoaderNegationTests
{
    private string _rootPath = null!;

    /// <summary>
    ///     为每个测试创建隔离的临时目录，避免不同 <c>not</c> 用例互相污染。
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        _rootPath = Path.Combine(Path.GetTempPath(), "GFramework.ConfigTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_rootPath);
    }

    /// <summary>
    ///     清理当前测试创建的临时目录，避免本地文件残留影响后续执行。
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, true);
        }
    }

    /// <summary>
    ///     验证运行时会拒绝命中 <c>not</c> 子 schema 的标量值。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_Value_Matches_Not_Schema()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            name: Deprecated
            hp: 10
            """);
        CreateSchemaFile(
            "schemas/monster.schema.json",
            """
            {
              "type": "object",
              "required": ["id", "name", "hp"],
              "properties": {
                "id": { "type": "integer" },
                "name": {
                  "type": "string",
                  "not": {
                    "type": "string",
                    "const": "Deprecated"
                  }
                },
                "hp": { "type": "integer" }
              }
            }
            """);

        var loader = CreateMonsterLoader();
        var registry = CreateRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.ConstraintViolation));
            Assert.That(exception.Diagnostic.DisplayPath, Is.EqualTo("name"));
            Assert.That(exception.Message, Does.Contain("must not match the 'not' schema"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证值未命中 <c>not</c> 子 schema 时，加载器不会误报禁用约束。
    /// </summary>
    [Test]
    public async Task LoadAsync_Should_Accept_When_Value_Does_Not_Match_Not_Schema()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            name: Slime
            hp: 10
            """);
        CreateSchemaFile(
            "schemas/monster.schema.json",
            """
            {
              "type": "object",
              "required": ["id", "name", "hp"],
              "properties": {
                "id": { "type": "integer" },
                "name": {
                  "type": "string",
                  "not": {
                    "type": "string",
                    "const": "Deprecated"
                  }
                },
                "hp": { "type": "integer" }
              }
            }
            """);

        var loader = CreateMonsterLoader();
        var registry = CreateRegistry();

        await loader.LoadAsync(registry).ConfigureAwait(false);

        var table = registry.GetTable<int, MonsterConfigStub>("monster");

        Assert.Multiple(() =>
        {
            Assert.That(table.Count, Is.EqualTo(1));
            Assert.That(table.Get(1).Name, Is.EqualTo("Slime"));
            Assert.That(table.Get(1).Hp, Is.EqualTo(10));
        });
    }

    /// <summary>
    ///     验证对象完整命中禁用 schema 时，同样会触发 <c>not</c> 约束失败。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_Object_Fully_Matches_Not_Schema()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            reward:
              gold: 10
            """);
        CreateSchemaFile(
            "schemas/monster.schema.json",
            """
            {
              "type": "object",
              "required": ["id", "reward"],
              "properties": {
                "id": { "type": "integer" },
                "reward": {
                  "type": "object",
                  "not": {
                    "type": "object",
                    "required": ["gold"],
                    "properties": {
                      "gold": { "type": "integer" }
                    }
                  },
                  "properties": {
                    "gold": { "type": "integer" },
                    "bonus": { "type": "integer" }
                  }
                }
              }
            }
            """);

        var loader = CreateMonsterRewardLoader();
        var registry = CreateRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.ConstraintViolation));
            Assert.That(exception.Diagnostic.DisplayPath, Is.EqualTo("reward"));
            Assert.That(exception.Message, Does.Contain("must not match the 'not' schema"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证对象仅命中 <c>not</c> schema 的属性子集时，不会被误判为完整命中。
    /// </summary>
    [Test]
    public async Task LoadAsync_Should_Accept_When_Object_Does_Not_Strictly_Match_Not_Schema()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            reward:
              gold: 10
              bonus: 5
            """);
        CreateSchemaFile(
            "schemas/monster.schema.json",
            """
            {
              "type": "object",
              "required": ["id", "reward"],
              "properties": {
                "id": { "type": "integer" },
                "reward": {
                  "type": "object",
                  "not": {
                    "type": "object",
                    "required": ["gold"],
                    "properties": {
                      "gold": { "type": "integer" }
                    }
                  },
                  "properties": {
                    "gold": { "type": "integer" },
                    "bonus": { "type": "integer" }
                  }
                }
              }
            }
            """);

        var loader = CreateMonsterRewardLoader();
        var registry = CreateRegistry();

        await loader.LoadAsync(registry).ConfigureAwait(false);

        var table = registry.GetTable<int, MonsterRewardConfigStub>("monster");

        Assert.Multiple(() =>
        {
            Assert.That(table.Count, Is.EqualTo(1));
            Assert.That(table.Get(1).Reward.Gold, Is.EqualTo(10));
            Assert.That(table.Get(1).Reward.Bonus, Is.EqualTo(5));
        });
    }

    /// <summary>
    ///     验证 schema 将 <c>not</c> 声明为非对象值时，会在解析阶段被拒绝。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_Not_Is_Not_An_Object()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            name: Slime
            hp: 10
            """);
        CreateSchemaFile(
            "schemas/monster.schema.json",
            """
            {
              "type": "object",
              "required": ["id", "name", "hp"],
              "properties": {
                "id": { "type": "integer" },
                "name": {
                  "type": "string",
                  "not": "deprecated"
                },
                "hp": { "type": "integer" }
              }
            }
            """);

        var loader = CreateMonsterLoader();
        var registry = CreateRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.SchemaUnsupported));
            Assert.That(exception.Diagnostic.DisplayPath, Is.EqualTo("name"));
            Assert.That(exception.Message, Does.Contain("must declare 'not' as an object-valued schema"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     在测试目录下写入配置文件，并自动创建缺失目录。
    /// </summary>
    /// <param name="relativePath">相对根目录的配置文件路径。</param>
    /// <param name="content">要写入的 YAML 或 schema 内容。</param>
    private void CreateConfigFile(string relativePath, string content)
    {
        var filePath = Path.Combine(_rootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var directoryPath = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        File.WriteAllText(filePath, content);
    }

    /// <summary>
    ///     写入测试 schema 文件，复用通用配置文件创建逻辑。
    /// </summary>
    /// <param name="relativePath">schema 相对路径。</param>
    /// <param name="content">schema JSON 内容。</param>
    private void CreateSchemaFile(string relativePath, string content)
    {
        CreateConfigFile(relativePath, content);
    }

    /// <summary>
    ///     创建用于标量 <c>not</c> 场景的加载器，统一测试夹具中的注册方式。
    /// </summary>
    /// <returns>已注册怪物表与 schema 路径的加载器。</returns>
    private YamlConfigLoader CreateMonsterLoader()
    {
        return new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterConfigStub>("monster", "monster", "schemas/monster.schema.json",
                static config => config.Id);
    }

    /// <summary>
    ///     创建用于对象 <c>not</c> 场景的加载器，避免重复维护同一注册参数。
    /// </summary>
    /// <returns>已注册奖励对象测试表的加载器。</returns>
    private YamlConfigLoader CreateMonsterRewardLoader()
    {
        return new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterRewardConfigStub>("monster", "monster", "schemas/monster.schema.json",
                static config => config.Id);
    }

    /// <summary>
    ///     创建新的配置注册表，明确每个用例都从干净状态开始。
    /// </summary>
    /// <returns>空的配置注册表。</returns>
    private static ConfigRegistry CreateRegistry()
    {
        return new ConfigRegistry();
    }

    /// <summary>
    ///     用于标量 <c>not</c> 回归测试的最小配置类型。
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
    ///     用于对象 <c>not</c> 回归测试的最小配置类型。
    /// </summary>
    private sealed class MonsterRewardConfigStub
    {
        /// <summary>
        ///     获取或设置主键。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        ///     获取或设置奖励对象。
        /// </summary>
        public RewardConfigStub Reward { get; set; } = new();
    }

    /// <summary>
    ///     表示对象 <c>not</c> 回归测试中的奖励节点。
    /// </summary>
    private sealed class RewardConfigStub
    {
        /// <summary>
        ///     获取或设置金币数量。
        /// </summary>
        public int Gold { get; set; }

        /// <summary>
        ///     获取或设置额外奖励数量。
        /// </summary>
        public int Bonus { get; set; }
    }
}
