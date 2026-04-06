using System;
using System.IO;
using System.Linq;
using GFramework.Game.Config;
using GFramework.Game.Config.Generated;

namespace GFramework.Game.Tests.Config;

/// <summary>
///     验证消费者项目通过 `schemas/**/*.schema.json` 自动拾取 schema 后，
///     可以直接编译并使用生成的聚合注册辅助、强类型访问入口、查询辅助与运行时加载链路。
/// </summary>
[TestFixture]
public class GeneratedConfigConsumerIntegrationTests
{
    /// <summary>
    ///     为每个端到端测试准备独立的配置根目录，避免编译期 schema 资产与运行时写入互相污染。
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        _rootPath = Path.Combine(Path.GetTempPath(), "GFramework.GeneratedConfigTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_rootPath);
    }

    /// <summary>
    ///     清理测试过程中创建的临时消费者目录。
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
    ///     验证生成器自动拾取消费者项目的 schema 后，
    ///     可以用生成的聚合注册辅助完成加载，并通过强类型表包装访问运行时数据与查询辅助。
    /// </summary>
    [Test]
    public async Task LoadAsync_Should_Support_Generated_Bindings_In_Consumer_Project()
    {
        CreateFile(
            "schemas/monster.schema.json",
            """
            {
              "title": "Monster Config",
              "description": "Defines one monster entry for the end-to-end consumer integration test.",
              "type": "object",
              "required": ["id", "name", "hp", "faction"],
              "properties": {
                "id": {
                  "type": "integer",
                  "description": "Monster identifier."
                },
                "name": {
                  "type": "string",
                  "description": "Monster display name."
                },
                "hp": {
                  "type": "integer",
                  "description": "Monster base health."
                },
                "faction": {
                  "type": "string",
                  "description": "Used by the integration test to validate generated non-unique queries."
                }
              }
            }
            """);
        CreateFile(
            "monster/slime.yaml",
            """
            id: 1
            name: Slime
            hp: 10
            faction: dungeon
            """);
        CreateFile(
            "monster/goblin.yaml",
            """
            id: 2
            name: Goblin
            hp: 30
            faction: dungeon
            """);

        var registry = new ConfigRegistry();
        var loader = new YamlConfigLoader(_rootPath)
            .RegisterAllGeneratedConfigTables();

        await loader.LoadAsync(registry);

        var table = registry.GetMonsterTable();
        var dungeonMonsters = table.FindByFaction("dungeon");

        Assert.Multiple(() =>
        {
            Assert.That(
                GeneratedConfigCatalog.Tables.Select(static metadata => metadata.TableName),
                Does.Contain("monster"));
            Assert.That(GeneratedConfigCatalog.TryGetByTableName("monster", out var catalogEntry), Is.True);
            Assert.That(catalogEntry.ConfigDomain, Is.EqualTo("monster"));
            Assert.That(catalogEntry.ConfigRelativePath, Is.EqualTo("monster"));
            Assert.That(catalogEntry.SchemaRelativePath, Is.EqualTo("schemas/monster.schema.json"));
            Assert.That(MonsterConfigBindings.ConfigDomain, Is.EqualTo("monster"));
            Assert.That(MonsterConfigBindings.TableName, Is.EqualTo("monster"));
            Assert.That(MonsterConfigBindings.ConfigRelativePath, Is.EqualTo("monster"));
            Assert.That(MonsterConfigBindings.SchemaRelativePath, Is.EqualTo("schemas/monster.schema.json"));
            Assert.That(MonsterConfigBindings.Metadata.ConfigDomain, Is.EqualTo(MonsterConfigBindings.ConfigDomain));
            Assert.That(MonsterConfigBindings.Metadata.TableName, Is.EqualTo(MonsterConfigBindings.TableName));
            Assert.That(MonsterConfigBindings.Metadata.ConfigRelativePath,
                Is.EqualTo(MonsterConfigBindings.ConfigRelativePath));
            Assert.That(MonsterConfigBindings.Metadata.SchemaRelativePath,
                Is.EqualTo(MonsterConfigBindings.SchemaRelativePath));
            Assert.That(MonsterConfigBindings.References.All, Is.Empty);
            Assert.That(MonsterConfigBindings.References.TryGetByDisplayPath("dropItems", out _), Is.False);
            Assert.That(table.Count, Is.EqualTo(2));
            Assert.That(table.Get(1).Name, Is.EqualTo("Slime"));
            Assert.That(table.Get(2).Hp, Is.EqualTo(30));
            Assert.That(table.FindByName("Slime").Select(static config => config.Id), Is.EqualTo(new[] { 1 }));
            Assert.That(dungeonMonsters.Select(static config => config.Name), Is.EquivalentTo(new[] { "Slime", "Goblin" }));
            Assert.That(table.TryFindFirstByName("Goblin", out var goblin), Is.True);
            Assert.That(goblin, Is.Not.Null);
            Assert.That(goblin!.Id, Is.EqualTo(2));
            Assert.That(table.TryFindFirstByFaction("dungeon", out var firstDungeonMonster), Is.True);
            Assert.That(firstDungeonMonster, Is.Not.Null);
            Assert.That(firstDungeonMonster!.Name, Is.AnyOf("Slime", "Goblin"));
            Assert.That(table.TryFindFirstByFaction("forest", out var missingMonster), Is.False);
            Assert.That(missingMonster, Is.Null);
            Assert.That(registry.TryGetMonsterTable(out var generatedTable), Is.True);
            Assert.That(generatedTable, Is.Not.Null);
            Assert.That(generatedTable!.All().Select(static config => config.Name),
                Is.EquivalentTo(new[] { "Slime", "Goblin" }));
        });
    }

    /// <summary>
    ///     在临时消费者根目录中创建测试文件。
    /// </summary>
    /// <param name="relativePath">相对根目录的文件路径。</param>
    /// <param name="content">要写入的文件内容。</param>
    private void CreateFile(
        string relativePath,
        string content)
    {
        var path = Path.Combine(_rootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var directoryPath = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        File.WriteAllText(path, content.Replace("\n", Environment.NewLine, StringComparison.Ordinal));
    }
}
