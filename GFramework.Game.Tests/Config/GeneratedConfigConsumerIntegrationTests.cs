using System.IO;
using GFramework.Game.Abstractions.Config;
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
        CreateMonsterFiles();
        CreateItemFiles();

        var registry = new ConfigRegistry();
        var loader = new YamlConfigLoader(_rootPath)
            .RegisterAllGeneratedConfigTables();

        await loader.LoadAsync(registry).ConfigureAwait(false);

        var monsterTable = registry.GetMonsterTable();
        var itemTable = registry.GetItemTable();

        AssertGeneratedBindingsLoadResults(registry, monsterTable, itemTable);
    }

    /// <summary>
    ///     验证项目级生成目录既能按配置域枚举元数据，也能直接复用聚合注册筛选规则产出启动诊断视图。
    /// </summary>
    [Test]
    public void GeneratedConfigCatalog_Should_Expose_Domain_And_Registration_Diagnostic_Views()
    {
        Assert.That(GeneratedConfigCatalog.TryGetByTableName("monster", out var monsterMetadata), Is.True);
        Assert.That(GeneratedConfigCatalog.TryGetByTableName("item", out var itemMetadata), Is.True);

        var monsterDomainTables = GeneratedConfigCatalog.GetTablesInConfigDomain(MonsterConfigBindings.ConfigDomain);
        var missingDomainTables = GeneratedConfigCatalog.GetTablesInConfigDomain("missing");
        var itemOnlyRegistrationTables = GeneratedConfigCatalog.GetTablesForRegistration(
            new GeneratedConfigRegistrationOptions
            {
                IncludedTableNames = new[] { ItemConfigBindings.TableName }
            });
        var predicateOnlyRegistrationTables = GeneratedConfigCatalog.GetTablesForRegistration(
            new GeneratedConfigRegistrationOptions
            {
                TableFilter = static metadata =>
                    string.Equals(metadata.TableName, MonsterConfigBindings.TableName, StringComparison.Ordinal)
            });
        var monsterOnlyOptions = new GeneratedConfigRegistrationOptions
        {
            IncludedConfigDomains = new[] { MonsterConfigBindings.ConfigDomain }
        };
        var predicateOnlyOptions = new GeneratedConfigRegistrationOptions
        {
            TableFilter = static metadata =>
                string.Equals(metadata.TableName, MonsterConfigBindings.TableName, StringComparison.Ordinal)
        };

        Assert.Multiple(() =>
        {
            Assert.That(monsterDomainTables.Select(static metadata => metadata.TableName),
                Is.EqualTo(new[] { MonsterConfigBindings.TableName }));
            Assert.That(missingDomainTables, Is.Empty);
            Assert.That(itemOnlyRegistrationTables.Select(static metadata => metadata.TableName),
                Is.EqualTo(new[] { ItemConfigBindings.TableName }));
            Assert.That(predicateOnlyRegistrationTables.Select(static metadata => metadata.TableName),
                Is.EqualTo(new[] { MonsterConfigBindings.TableName }));
            Assert.That(GeneratedConfigCatalog.GetTablesForRegistration().Select(static metadata => metadata.TableName),
                Is.SupersetOf(new[] { ItemConfigBindings.TableName, MonsterConfigBindings.TableName }));
            Assert.That(GeneratedConfigCatalog.MatchesRegistrationOptions(monsterMetadata, monsterOnlyOptions),
                Is.True);
            Assert.That(GeneratedConfigCatalog.MatchesRegistrationOptions(itemMetadata, monsterOnlyOptions), Is.False);
            Assert.That(GeneratedConfigCatalog.MatchesRegistrationOptions(monsterMetadata, predicateOnlyOptions),
                Is.True);
            Assert.That(GeneratedConfigCatalog.MatchesRegistrationOptions(itemMetadata, predicateOnlyOptions),
                Is.False);
            Assert.That(GeneratedConfigCatalog.MatchesRegistrationOptions(monsterMetadata, options: null), Is.True);
        });
    }

    /// <summary>
    ///     验证聚合注册入口可以通过生成配置域、表名集合和自定义谓词收敛多表项目的启动粒度。
    /// </summary>
    [Test]
    public async Task RegisterAllGeneratedConfigTables_Should_Support_Filtering_By_Domain_Table_Name_And_Predicate()
    {
        CreateMonsterFiles();
        CreateItemFiles();

        var domainRegistry = new ConfigRegistry();
        var domainLoader = new YamlConfigLoader(_rootPath)
            .RegisterAllGeneratedConfigTables(
                new GeneratedConfigRegistrationOptions
                {
                    IncludedConfigDomains = new[] { MonsterConfigBindings.ConfigDomain }
                });
        await domainLoader.LoadAsync(domainRegistry).ConfigureAwait(false);

        var tableNameRegistry = new ConfigRegistry();
        var tableNameLoader = new YamlConfigLoader(_rootPath)
            .RegisterAllGeneratedConfigTables(
                new GeneratedConfigRegistrationOptions
                {
                    IncludedTableNames = new[] { ItemConfigBindings.TableName }
                });
        await tableNameLoader.LoadAsync(tableNameRegistry).ConfigureAwait(false);

        var emptyAllowListRegistry = new ConfigRegistry();
        var emptyAllowListLoader = new YamlConfigLoader(_rootPath)
            .RegisterAllGeneratedConfigTables(
                new GeneratedConfigRegistrationOptions
                {
                    IncludedConfigDomains = Array.Empty<string>(),
                    IncludedTableNames = Array.Empty<string>()
                });
        await emptyAllowListLoader.LoadAsync(emptyAllowListRegistry).ConfigureAwait(false);

        var monsterDomain = MonsterConfigBindings.ConfigDomain;
        var predicateRegistry = new ConfigRegistry();
        var predicateLoader = new YamlConfigLoader(_rootPath)
            .RegisterAllGeneratedConfigTables(
                new GeneratedConfigRegistrationOptions
                {
                    TableFilter = metadata =>
                        string.Equals(metadata.ConfigDomain, monsterDomain, StringComparison.Ordinal)
                });
        await predicateLoader.LoadAsync(predicateRegistry).ConfigureAwait(false);

        AssertGeneratedRegistrationFilteringResults(
            domainRegistry,
            tableNameRegistry,
            emptyAllowListRegistry,
            predicateRegistry);
    }

    /// <summary>
    ///     验证生成绑定会同时暴露 YAML 序列化、schema 路径解析与文本校验入口。
    /// </summary>
    [Test]
    public async Task GeneratedBindings_Should_Expose_Serializer_And_Validator_Helpers()
    {
        CreateMonsterFiles();

        var config = new MonsterConfig
        {
            Id = 3,
            Name = "Bat",
            Hp = 12,
            Faction = "cave"
        };

        var yaml = MonsterConfigBindings.SerializeToYaml(config);
        var schemaPath = MonsterConfigBindings.GetSchemaPath(_rootPath);
        var configDirectoryPath = MonsterConfigBindings.GetConfigDirectoryPath(_rootPath);

        Assert.Multiple(() =>
        {
            Assert.That(schemaPath, Is.EqualTo(Path.Combine(_rootPath, "schemas", "monster.schema.json")));
            Assert.That(configDirectoryPath, Is.EqualTo(Path.Combine(_rootPath, "monster")));
            Assert.That(yaml, Does.Contain("id: 3"));
            Assert.That(yaml, Does.Contain("name: Bat"));
            Assert.That(yaml, Does.Contain("hp: 12"));
            Assert.That(yaml, Does.Contain("faction: cave"));
            Assert.That(yaml.EndsWith("\n", StringComparison.Ordinal), Is.True);
        });

        MonsterConfigBindings.ValidateYaml(_rootPath, "monster/generated.yaml", yaml);
        Assert.That(
            async () => await MonsterConfigBindings.ValidateYamlAsync(_rootPath, "monster/generated.yaml", yaml).ConfigureAwait(false),
            Throws.Nothing);

        var invalidYaml = """
                          id: 3
                          name: Bat
                          hp: 12
                          unknownField: true
                          """;

        var exception = Assert.Throws<ConfigLoadException>(() =>
            MonsterConfigBindings.ValidateYaml(_rootPath, "monster/generated.yaml", invalidYaml));
        var asyncException = Assert.ThrowsAsync<ConfigLoadException>(async () =>
            await MonsterConfigBindings.ValidateYamlAsync(_rootPath, "monster/generated.yaml", invalidYaml).ConfigureAwait(false));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Diagnostic.SchemaPath, Is.EqualTo(schemaPath));
            Assert.That(exception.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.UnknownProperty));
            Assert.That(asyncException, Is.Not.Null);
            Assert.That(asyncException!.Diagnostic.SchemaPath, Is.EqualTo(schemaPath));
            Assert.That(asyncException.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.UnknownProperty));
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

    /// <summary>
    ///     在临时消费者目录中创建 monster schema 与 YAML 测试数据。
    /// </summary>
    private void CreateMonsterFiles()
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
                  "description": "Monster display name.",
                  "x-gframework-index": true
                },
                "hp": {
                  "type": "integer",
                  "description": "Monster base health."
                },
                "faction": {
                  "type": "string",
                  "description": "Used by the integration test to validate generated non-unique queries.",
                  "x-gframework-index": true
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
    }

    /// <summary>
    ///     统一断言生成绑定加载后的目录元数据、查询入口与强类型表包装结果，
    ///     以便缩短端到端测试主体并降低分析器对方法长度的告警。
    /// </summary>
    private static void AssertGeneratedBindingsLoadResults(
        ConfigRegistry registry,
        MonsterTable monsterTable,
        ItemTable itemTable)
    {
        AssertGeneratedCatalogMetadata();
        AssertGeneratedMonsterTableResults(registry, monsterTable);
        AssertGeneratedItemTableResults(registry, itemTable);
    }

    /// <summary>
    ///     断言消费者项目的生成目录元数据与静态绑定常量保持一致。
    /// </summary>
    private static void AssertGeneratedCatalogMetadata()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                GeneratedConfigCatalog.Tables.Select(static metadata => metadata.TableName),
                Is.SupersetOf(new[] { "item", "monster" }));
            Assert.That(GeneratedConfigCatalog.TryGetByTableName("item", out var itemCatalogEntry), Is.True);
            Assert.That(itemCatalogEntry.ConfigDomain, Is.EqualTo("item"));
            Assert.That(itemCatalogEntry.ConfigRelativePath, Is.EqualTo("item"));
            Assert.That(itemCatalogEntry.SchemaRelativePath, Is.EqualTo("schemas/item.schema.json"));
            Assert.That(GeneratedConfigCatalog.TryGetByTableName("monster", out var catalogEntry), Is.True);
            Assert.That(catalogEntry.ConfigDomain, Is.EqualTo("monster"));
            Assert.That(catalogEntry.ConfigRelativePath, Is.EqualTo("monster"));
            Assert.That(catalogEntry.SchemaRelativePath, Is.EqualTo("schemas/monster.schema.json"));
            Assert.That(ItemConfigBindings.ConfigDomain, Is.EqualTo("item"));
            Assert.That(ItemConfigBindings.Metadata.TableName, Is.EqualTo("item"));
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
        });
    }

    /// <summary>
    ///     断言 monster 绑定在注册表中的查询辅助、索引查询与强类型访问入口都可用。
    /// </summary>
    private static void AssertGeneratedMonsterTableResults(
        ConfigRegistry registry,
        MonsterTable monsterTable)
    {
        var dungeonMonsters = monsterTable.FindByFaction("dungeon");

        Assert.Multiple(() =>
        {
            Assert.That(monsterTable.Count, Is.EqualTo(2));
            Assert.That(monsterTable.Get(1).Name, Is.EqualTo("Slime"));
            Assert.That(monsterTable.Get(2).Hp, Is.EqualTo(30));
            Assert.That(monsterTable.FindByName("Slime").Select(static config => config.Id), Is.EqualTo(new[] { 1 }));
            Assert.That(dungeonMonsters.Select(static config => config.Name),
                Is.EquivalentTo(new[] { "Slime", "Goblin" }));
            Assert.That(monsterTable.TryFindFirstByName("Goblin", out var goblin), Is.True);
            Assert.That(goblin, Is.Not.Null);
            Assert.That(goblin!.Id, Is.EqualTo(2));
            Assert.That(monsterTable.TryFindFirstByFaction("dungeon", out var firstDungeonMonster), Is.True);
            Assert.That(firstDungeonMonster, Is.Not.Null);
            Assert.That(firstDungeonMonster!.Name, Is.AnyOf("Slime", "Goblin"));
            Assert.That(monsterTable.TryFindFirstByFaction("forest", out var missingMonster), Is.False);
            Assert.That(missingMonster, Is.Null);
            Assert.That(registry.TryGetMonsterTable(out var generatedTable), Is.True);
            Assert.That(generatedTable, Is.Not.Null);
            Assert.That(generatedTable!.All().Select(static config => config.Name),
                Is.EquivalentTo(new[] { "Slime", "Goblin" }));
        });
    }

    /// <summary>
    ///     断言 item 绑定的强类型表包装与按分类查询在聚合注册路径下可正常工作。
    /// </summary>
    private static void AssertGeneratedItemTableResults(
        ConfigRegistry registry,
        ItemTable itemTable)
    {
        Assert.Multiple(() =>
        {
            Assert.That(itemTable.Count, Is.EqualTo(2));
            Assert.That(itemTable.Get("potion").Name, Is.EqualTo("Potion"));
            Assert.That(itemTable.FindByCategory("consumable").Select(static config => config.Id),
                Is.EquivalentTo(new[] { "potion", "ether" }));
            Assert.That(registry.TryGetItemTable(out var generatedItemTable), Is.True);
            Assert.That(generatedItemTable, Is.Not.Null);
            Assert.That(generatedItemTable!.Get("ether").Name, Is.EqualTo("Ether"));
        });
    }

    /// <summary>
    ///     汇总断言不同聚合注册筛选条件下的装载结果，
    ///     让测试主体聚焦于注册参数本身而不是展开大量重复断言。
    /// </summary>
    private static void AssertGeneratedRegistrationFilteringResults(
        ConfigRegistry domainRegistry,
        ConfigRegistry tableNameRegistry,
        ConfigRegistry emptyAllowListRegistry,
        ConfigRegistry predicateRegistry)
    {
        Assert.Multiple(() =>
        {
            Assert.That(emptyAllowListRegistry.TryGetMonsterTable(out var emptyAllowListMonsterTable), Is.True);
            Assert.That(emptyAllowListMonsterTable, Is.Not.Null);
            Assert.That(emptyAllowListRegistry.TryGetItemTable(out var emptyAllowListItemTable), Is.True);
            Assert.That(emptyAllowListItemTable, Is.Not.Null);

            Assert.That(domainRegistry.TryGetMonsterTable(out var domainMonsterTable), Is.True);
            Assert.That(domainMonsterTable, Is.Not.Null);
            Assert.That(domainRegistry.TryGetItemTable(out _), Is.False);

            Assert.That(tableNameRegistry.TryGetMonsterTable(out _), Is.False);
            Assert.That(tableNameRegistry.TryGetItemTable(out var tableNameItemTable), Is.True);
            Assert.That(tableNameItemTable, Is.Not.Null);
            Assert.That(tableNameItemTable!.Get("potion").Name, Is.EqualTo("Potion"));

            Assert.That(predicateRegistry.TryGetMonsterTable(out var predicateMonsterTable), Is.True);
            Assert.That(predicateMonsterTable, Is.Not.Null);
            Assert.That(predicateRegistry.TryGetItemTable(out _), Is.False);
        });
    }

    /// <summary>
    ///     在临时消费者目录中创建 item schema 与 YAML 测试数据，用于验证多表聚合注册和筛选行为。
    /// </summary>
    private void CreateItemFiles()
    {
        CreateFile(
            "schemas/item.schema.json",
            """
            {
              "title": "Item Config",
              "description": "Defines one item entry for aggregate registration filtering integration tests.",
              "type": "object",
              "required": ["id", "name", "category"],
              "properties": {
                "id": {
                  "type": "string",
                  "description": "Item identifier."
                },
                "name": {
                  "type": "string",
                  "description": "Item display name."
                },
                "category": {
                  "type": "string",
                  "description": "Used by integration tests to validate generated non-unique queries."
                }
              }
            }
            """);
        CreateFile(
            "item/potion.yaml",
            """
            id: potion
            name: Potion
            category: consumable
            """);
        CreateFile(
            "item/ether.yaml",
            """
            id: ether
            name: Ether
            category: consumable
            """);
    }
}
