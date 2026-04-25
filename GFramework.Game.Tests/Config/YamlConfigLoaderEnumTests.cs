using System.IO;
using GFramework.Game.Abstractions.Config;
using GFramework.Game.Config;

namespace GFramework.Game.Tests.Config;

/// <summary>
///     验证 YAML 配置加载器对对象 / 数组 <c>enum</c> 约束的运行时行为。
/// </summary>
[TestFixture]
public class YamlConfigLoaderEnumTests
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
    ///     验证对象 <c>enum</c> 会按字段名排序后的稳定比较键匹配，而不是依赖 schema 内的 JSON 字段顺序。
    /// </summary>
    [Test]
    public async Task LoadAsync_Should_Accept_Object_Value_Declared_In_Schema_Enum_When_Property_Order_Differs()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            reward:
              gold: 10
              itemId: potion
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
                  "required": ["gold", "itemId"],
                  "properties": {
                    "gold": { "type": "integer" },
                    "itemId": { "type": "string" }
                  },
                  "enum": [
                    { "itemId": "potion", "gold": 10 },
                    { "gold": 50, "itemId": "gem" }
                  ]
                }
              }
            }
            """);

        var loader = CreateLoader<MonsterRewardConfigStub>();
        var registry = new ConfigRegistry();

        await loader.LoadAsync(registry).ConfigureAwait(false);

        var table = registry.GetTable<int, MonsterRewardConfigStub>("monster");
        Assert.Multiple(() =>
        {
            Assert.That(table.Count, Is.EqualTo(1));
            Assert.That(table.Get(1).Reward.Gold, Is.EqualTo(10));
            Assert.That(table.Get(1).Reward.ItemId, Is.EqualTo("potion"));
        });
    }

    /// <summary>
    ///     验证对象 <c>enum</c> 不匹配时，运行时会拒绝整个对象值并输出候选 JSON 文本。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_Object_Value_Is_Not_Declared_In_Schema_Enum()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            reward:
              gold: 10
              itemId: elixir
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
                  "required": ["gold", "itemId"],
                  "properties": {
                    "gold": { "type": "integer" },
                    "itemId": { "type": "string" }
                  },
                  "enum": [
                    { "gold": 10, "itemId": "potion" },
                    { "gold": 50, "itemId": "gem" }
                  ]
                }
              }
            }
            """);

        var loader = CreateLoader<MonsterRewardConfigStub>();
        var registry = new ConfigRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Message, Does.Contain("reward"));
            Assert.That(exception.Message, Does.Contain("\"itemId\": \"potion\""));
            Assert.That(exception.Message, Does.Contain("\"itemId\": \"gem\""));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证数组 <c>enum</c> 会保留元素顺序；同一批元素但顺序不同仍视为不同候选值。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_Array_Value_Order_Does_Not_Match_Schema_Enum()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            dropItemIds:
              - ice
              - fire
            """);
        CreateSchemaFile(
            "schemas/monster.schema.json",
            """
            {
              "type": "object",
              "required": ["id", "dropItemIds"],
              "properties": {
                "id": { "type": "integer" },
                "dropItemIds": {
                  "type": "array",
                  "items": { "type": "string" },
                  "enum": [
                    ["fire", "ice"],
                    ["earth"]
                  ]
                }
              }
            }
            """);

        var loader = CreateLoader<MonsterDropItemIdsConfigStub>();
        var registry = new ConfigRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Message, Does.Contain("dropItemIds"));
            Assert.That(exception.Message, Does.Contain("[\"fire\", \"ice\"]"));
            Assert.That(exception.Message, Does.Contain("[\"earth\"]"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     创建带 schema 校验的测试加载器。
    /// </summary>
    /// <typeparam name="TConfig">配置类型。</typeparam>
    /// <returns>已注册 monster 表的加载器。</returns>
    private YamlConfigLoader CreateLoader<TConfig>()
        where TConfig : IHasMonsterId
    {
        return new YamlConfigLoader(_rootPath)
            .RegisterTable<int, TConfig>("monster", "monster", "schemas/monster.schema.json", static config => config.Id);
    }

    /// <summary>
    ///     创建测试配置文件。
    /// </summary>
    /// <param name="relativePath">相对路径。</param>
    /// <param name="content">文件内容。</param>
    private void CreateConfigFile(string relativePath, string content)
    {
        var filePath = Path.Combine(_rootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var directoryPath = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        File.WriteAllText(filePath, content);
    }

    /// <summary>
    ///     创建测试 schema 文件。
    /// </summary>
    /// <param name="relativePath">相对路径。</param>
    /// <param name="content">文件内容。</param>
    private void CreateSchemaFile(string relativePath, string content)
    {
        var filePath = Path.Combine(_rootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var directoryPath = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        File.WriteAllText(filePath, content);
    }

    /// <summary>
    ///     为通用测试加载器暴露统一主键访问约定。
    /// </summary>
    private interface IHasMonsterId
    {
        /// <summary>
        ///     获取配置主键。
        /// </summary>
        int Id { get; }
    }

    /// <summary>
    ///     供对象 <c>enum</c> 测试使用的配置桩。
    /// </summary>
    private sealed class MonsterRewardConfigStub : IHasMonsterId
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
    ///     供数组 <c>enum</c> 测试使用的配置桩。
    /// </summary>
    private sealed class MonsterDropItemIdsConfigStub : IHasMonsterId
    {
        /// <summary>
        ///     获取或设置主键。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        ///     获取或设置掉落物数组。
        /// </summary>
        public IReadOnlyList<string> DropItemIds { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    ///     供对象 <c>enum</c> 测试使用的奖励配置桩。
    /// </summary>
    private sealed class RewardConfigStub
    {
        /// <summary>
        ///     获取或设置金币数量。
        /// </summary>
        public int Gold { get; set; }

        /// <summary>
        ///     获取或设置道具标识。
        /// </summary>
        public string ItemId { get; set; } = string.Empty;
    }
}
