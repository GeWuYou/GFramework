using System.IO;
using GFramework.Game.Abstractions.Config;
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
    ///     验证加载器支持通过选项对象注册带 schema 校验的配置表。
    /// </summary>
    [Test]
    public async Task RegisterTable_Should_Support_Options_Object()
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
              "required": ["id", "name"],
              "properties": {
                "id": { "type": "integer" },
                "name": { "type": "string" },
                "hp": { "type": "integer" }
              }
            }
            """);

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable(
                new YamlConfigTableRegistrationOptions<int, MonsterConfigStub>(
                    "monster",
                    "monster",
                    static config => config.Id)
                {
                    SchemaRelativePath = "schemas/monster.schema.json"
                });
        var registry = new ConfigRegistry();

        await loader.LoadAsync(registry);

        var table = registry.GetTable<int, MonsterConfigStub>("monster");

        Assert.Multiple(() =>
        {
            Assert.That(table.Count, Is.EqualTo(1));
            Assert.That(table.Get(1).Name, Is.EqualTo("Slime"));
            Assert.That(table.Get(1).Hp, Is.EqualTo(10));
        });
    }

    /// <summary>
    ///     验证加载器会拒绝空的配置表注册选项对象。
    /// </summary>
    [Test]
    public void RegisterTable_Should_Throw_When_Options_Are_Null()
    {
        var loader = new YamlConfigLoader(_rootPath);

        Assert.Throws<ArgumentNullException>(() =>
            loader.RegisterTable<int, MonsterConfigStub>(null!));
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

        var exception = Assert.ThrowsAsync<ConfigLoadException>(async () => await loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Message, Does.Contain("monster"));
            Assert.That(exception.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.ConfigDirectoryNotFound));
            Assert.That(exception.Diagnostic.TableName, Is.EqualTo("monster"));
            Assert.That(exception.Diagnostic.ConfigDirectoryPath,
                Is.EqualTo(Path.Combine(_rootPath, "monster")));
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

        var exception = Assert.ThrowsAsync<ConfigLoadException>(async () => await loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Diagnostic.FailureKind,
                Is.EqualTo(ConfigLoadFailureKind.ConfigDirectoryNotFound));
            Assert.That(exception.Diagnostic.TableName, Is.EqualTo("broken"));
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

        var exception = Assert.ThrowsAsync<ConfigLoadException>(async () => await loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Message, Does.Contain("slime.yaml"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证启用 schema 校验后，缺失必填字段会在反序列化前被拒绝。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_Required_Property_Is_Missing_According_To_Schema()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            hp: 10
            """);
        CreateSchemaFile(
            "schemas/monster.schema.json",
            """
            {
              "type": "object",
              "required": ["id", "name"],
              "properties": {
                "id": { "type": "integer" },
                "name": { "type": "string" },
                "hp": { "type": "integer" }
              }
            }
            """);

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterConfigStub>("monster", "monster", "schemas/monster.schema.json",
                static config => config.Id);
        var registry = new ConfigRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(async () => await loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Message, Does.Contain("name"));
            Assert.That(exception.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.MissingRequiredProperty));
            Assert.That(exception.Diagnostic.TableName, Is.EqualTo("monster"));
            Assert.That(exception.Diagnostic.YamlPath,
                Does.EndWith("monster/slime.yaml").Or.EndWith("monster\\slime.yaml"));
            Assert.That(exception.Diagnostic.SchemaPath,
                Does.EndWith("schemas/monster.schema.json").Or.EndWith("schemas\\monster.schema.json"));
            Assert.That(exception.Diagnostic.DisplayPath, Is.EqualTo("name"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证启用 schema 校验后，类型不匹配的标量字段会被拒绝。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_Property_Type_Does_Not_Match_Schema()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            name: Slime
            hp: high
            """);
        CreateSchemaFile(
            "schemas/monster.schema.json",
            """
            {
              "type": "object",
              "required": ["id", "name"],
              "properties": {
                "id": { "type": "integer" },
                "name": { "type": "string" },
                "hp": { "type": "integer" }
              }
            }
            """);

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterConfigStub>("monster", "monster", "schemas/monster.schema.json",
                static config => config.Id);
        var registry = new ConfigRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(async () => await loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Message, Does.Contain("hp"));
            Assert.That(exception!.Message, Does.Contain("integer"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证启用 schema 校验后，标量 enum 限制会在运行时被拒绝。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_Scalar_Value_Is_Not_Declared_In_Schema_Enum()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            name: Slime
            rarity: epic
            """);
        CreateSchemaFile(
            "schemas/monster.schema.json",
            """
            {
              "type": "object",
              "required": ["id", "name", "rarity"],
              "properties": {
                "id": { "type": "integer" },
                "name": { "type": "string" },
                "rarity": {
                  "type": "string",
                  "enum": ["common", "rare"]
                }
              }
            }
            """);

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterConfigStub>("monster", "monster", "schemas/monster.schema.json",
                static config => config.Id);
        var registry = new ConfigRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(async () => await loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Message, Does.Contain("common"));
            Assert.That(exception!.Message, Does.Contain("rare"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证数值最小值与最大值约束会在运行时被统一拒绝。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_Number_Violates_Minimum_Or_Maximum()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            name: Slime
            hp: 101
            """);
        CreateSchemaFile(
            "schemas/monster.schema.json",
            """
            {
              "type": "object",
              "required": ["id", "name", "hp"],
              "properties": {
                "id": { "type": "integer" },
                "name": { "type": "string" },
                "hp": {
                  "type": "integer",
                  "minimum": 1,
                  "maximum": 100
                }
              }
            }
            """);

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterConfigStub>("monster", "monster", "schemas/monster.schema.json",
                static config => config.Id);
        var registry = new ConfigRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(async () => await loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.ConstraintViolation));
            Assert.That(exception.Diagnostic.DisplayPath, Is.EqualTo("hp"));
            Assert.That(exception.Diagnostic.RawValue, Is.EqualTo("101"));
            Assert.That(exception.Message, Does.Contain("100"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证字符串最小长度与最大长度约束会在运行时被统一拒绝。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_String_Violates_MinLength_Or_MaxLength()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            name: Sl
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
                  "minLength": 3,
                  "maxLength": 12
                },
                "hp": { "type": "integer" }
              }
            }
            """);

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterConfigStub>("monster", "monster", "schemas/monster.schema.json",
                static config => config.Id);
        var registry = new ConfigRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(async () => await loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.ConstraintViolation));
            Assert.That(exception.Diagnostic.DisplayPath, Is.EqualTo("name"));
            Assert.That(exception.Diagnostic.RawValue, Is.EqualTo("Sl"));
            Assert.That(exception.Message, Does.Contain("at least 3 characters"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证启用 schema 校验后，未知字段不会再被静默忽略。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_Unknown_Property_Is_Present_In_Schema_Bound_Mode()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            name: Slime
            attackPower: 2
            """);
        CreateSchemaFile(
            "schemas/monster.schema.json",
            """
            {
              "type": "object",
              "required": ["id", "name"],
              "properties": {
                "id": { "type": "integer" },
                "name": { "type": "string" },
                "hp": { "type": "integer" }
              }
            }
            """);

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterConfigStub>("monster", "monster", "schemas/monster.schema.json",
                static config => config.Id);
        var registry = new ConfigRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(async () => await loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Message, Does.Contain("attackPower"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证数组字段的元素类型会按 schema 校验。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_Array_Item_Type_Does_Not_Match_Schema()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            name: Slime
            dropRates:
              - 1
              - potion
            """);
        CreateSchemaFile(
            "schemas/monster.schema.json",
            """
            {
              "type": "object",
              "required": ["id", "name"],
              "properties": {
                "id": { "type": "integer" },
                "name": { "type": "string" },
                "dropRates": {
                  "type": "array",
                  "items": { "type": "integer" }
                }
              }
            }
            """);

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterConfigIntegerArrayStub>(
                "monster",
                "monster",
                "schemas/monster.schema.json",
                static config => config.Id);
        var registry = new ConfigRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(async () => await loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Message, Does.Contain("dropRates"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证数组元素上的 enum 限制会按 schema 在运行时生效。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_Array_Item_Is_Not_Declared_In_Schema_Enum()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            name: Slime
            tags:
              - fire
              - poison
            """);
        CreateSchemaFile(
            "schemas/monster.schema.json",
            """
            {
              "type": "object",
              "required": ["id", "name"],
              "properties": {
                "id": { "type": "integer" },
                "name": { "type": "string" },
                "tags": {
                  "type": "array",
                  "items": {
                    "type": "string",
                    "enum": ["fire", "ice"]
                  }
                }
              }
            }
            """);

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterDropArrayConfigStub>("monster", "monster", "schemas/monster.schema.json",
                static config => config.Id);
        var registry = new ConfigRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(async () => await loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Message, Does.Contain("fire"));
            Assert.That(exception!.Message, Does.Contain("ice"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证嵌套对象中的必填字段同样会按 schema 在运行时生效。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_Nested_Object_Is_Missing_Required_Property()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            name: Slime
            reward:
              gold: 10
            """);
        CreateSchemaFile(
            "schemas/monster.schema.json",
            """
            {
              "type": "object",
              "required": ["id", "name", "reward"],
              "properties": {
                "id": { "type": "integer" },
                "name": { "type": "string" },
                "reward": {
                  "type": "object",
                  "required": ["gold", "currency"],
                  "properties": {
                    "gold": { "type": "integer" },
                    "currency": { "type": "string" }
                  }
                }
              }
            }
            """);

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterNestedConfigStub>("monster", "monster", "schemas/monster.schema.json",
                static config => config.Id);
        var registry = new ConfigRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(async () => await loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Message, Does.Contain("reward.currency"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证对象数组中的嵌套字段也会按 schema 递归校验。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_Object_Array_Item_Contains_Unknown_Property()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            name: Slime
            phases:
              -
                wave: 1
                monsterId: slime
                hpScale: 1.5
            """);
        CreateSchemaFile(
            "schemas/monster.schema.json",
            """
            {
              "type": "object",
              "required": ["id", "name", "phases"],
              "properties": {
                "id": { "type": "integer" },
                "name": { "type": "string" },
                "phases": {
                  "type": "array",
                  "items": {
                    "type": "object",
                    "required": ["wave", "monsterId"],
                    "properties": {
                      "wave": { "type": "integer" },
                      "monsterId": { "type": "string" }
                    }
                  }
                }
              }
            }
            """);

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterPhaseArrayConfigStub>("monster", "monster", "schemas/monster.schema.json",
                static config => config.Id);
        var registry = new ConfigRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(async () => await loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Message, Does.Contain("phases[0].hpScale"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证深层对象数组中的跨表引用也会参与整批加载校验。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_Nested_Object_Array_Reference_Target_Is_Missing()
    {
        CreateConfigFile(
            "item/potion.yaml",
            """
            id: potion
            name: Potion
            """);
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            name: Slime
            phases:
              -
                wave: 1
                dropItemId: potion
              -
                wave: 2
                dropItemId: bomb
            """);
        CreateSchemaFile(
            "schemas/item.schema.json",
            """
            {
              "type": "object",
              "required": ["id", "name"],
              "properties": {
                "id": { "type": "string" },
                "name": { "type": "string" }
              }
            }
            """);
        CreateSchemaFile(
            "schemas/monster.schema.json",
            """
            {
              "type": "object",
              "required": ["id", "name", "phases"],
              "properties": {
                "id": { "type": "integer" },
                "name": { "type": "string" },
                "phases": {
                  "type": "array",
                  "items": {
                    "type": "object",
                    "required": ["wave", "dropItemId"],
                    "properties": {
                      "wave": { "type": "integer" },
                      "dropItemId": {
                        "type": "string",
                        "x-gframework-ref-table": "item"
                      }
                    }
                  }
                }
              }
            }
            """);

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<string, ItemConfigStub>("item", "item", "schemas/item.schema.json",
                static config => config.Id)
            .RegisterTable<int, MonsterPhaseDropConfigStub>("monster", "monster", "schemas/monster.schema.json",
                static config => config.Id);
        var registry = new ConfigRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(async () => await loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Message, Does.Contain("phases[1].dropItemId"));
            Assert.That(exception!.Message, Does.Contain("bomb"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证绑定跨表引用 schema 时，存在的目标行可以通过加载校验。
    /// </summary>
    [Test]
    public async Task LoadAsync_Should_Accept_Existing_Cross_Table_Reference()
    {
        CreateConfigFile(
            "item/potion.yaml",
            """
            id: potion
            name: Potion
            """);
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            name: Slime
            dropItemId: potion
            """);
        CreateSchemaFile(
            "schemas/item.schema.json",
            """
            {
              "type": "object",
              "required": ["id", "name"],
              "properties": {
                "id": { "type": "string" },
                "name": { "type": "string" }
              }
            }
            """);
        CreateSchemaFile(
            "schemas/monster.schema.json",
            """
            {
              "type": "object",
              "required": ["id", "name", "dropItemId"],
              "properties": {
                "id": { "type": "integer" },
                "name": { "type": "string" },
                "dropItemId": {
                  "type": "string",
                  "x-gframework-ref-table": "item"
                }
              }
            }
            """);

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<string, ItemConfigStub>("item", "item", "schemas/item.schema.json",
                static config => config.Id)
            .RegisterTable<int, MonsterDropConfigStub>("monster", "monster", "schemas/monster.schema.json",
                static config => config.Id);
        var registry = new ConfigRegistry();

        await loader.LoadAsync(registry);

        Assert.Multiple(() =>
        {
            Assert.That(registry.GetTable<string, ItemConfigStub>("item").ContainsKey("potion"), Is.True);
            Assert.That(registry.GetTable<int, MonsterDropConfigStub>("monster").Get(1).DropItemId,
                Is.EqualTo("potion"));
        });
    }

    /// <summary>
    ///     验证缺失的跨表引用会阻止整批配置写入注册表。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_Cross_Table_Reference_Target_Is_Missing()
    {
        CreateConfigFile(
            "item/slime-gel.yaml",
            """
            id: slime_gel
            name: Slime Gel
            """);
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            name: Slime
            dropItemId: potion
            """);
        CreateSchemaFile(
            "schemas/item.schema.json",
            """
            {
              "type": "object",
              "required": ["id", "name"],
              "properties": {
                "id": { "type": "string" },
                "name": { "type": "string" }
              }
            }
            """);
        CreateSchemaFile(
            "schemas/monster.schema.json",
            """
            {
              "type": "object",
              "required": ["id", "name", "dropItemId"],
              "properties": {
                "id": { "type": "integer" },
                "name": { "type": "string" },
                "dropItemId": {
                  "type": "string",
                  "x-gframework-ref-table": "item"
                }
              }
            }
            """);

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<string, ItemConfigStub>("item", "item", "schemas/item.schema.json",
                static config => config.Id)
            .RegisterTable<int, MonsterDropConfigStub>("monster", "monster", "schemas/monster.schema.json",
                static config => config.Id);
        var registry = new ConfigRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(async () => await loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Message, Does.Contain("dropItemId"));
            Assert.That(exception!.Message, Does.Contain("potion"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证跨表引用同样支持标量数组中的每个元素。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_Array_Reference_Item_Is_Missing()
    {
        CreateConfigFile(
            "item/potion.yaml",
            """
            id: potion
            name: Potion
            """);
        CreateConfigFile(
            "item/slime-gel.yaml",
            """
            id: slime_gel
            name: Slime Gel
            """);
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            name: Slime
            dropItemIds:
              - potion
              - missing_item
            """);
        CreateSchemaFile(
            "schemas/item.schema.json",
            """
            {
              "type": "object",
              "required": ["id", "name"],
              "properties": {
                "id": { "type": "string" },
                "name": { "type": "string" }
              }
            }
            """);
        CreateSchemaFile(
            "schemas/monster.schema.json",
            """
            {
              "type": "object",
              "required": ["id", "name", "dropItemIds"],
              "properties": {
                "id": { "type": "integer" },
                "name": { "type": "string" },
                "dropItemIds": {
                  "type": "array",
                  "items": { "type": "string" },
                  "x-gframework-ref-table": "item"
                }
              }
            }
            """);

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<string, ItemConfigStub>("item", "item", "schemas/item.schema.json",
                static config => config.Id)
            .RegisterTable<int, MonsterDropArrayConfigStub>("monster", "monster", "schemas/monster.schema.json",
                static config => config.Id);
        var registry = new ConfigRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(async () => await loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Message, Does.Contain("dropItemIds[1]"));
            Assert.That(exception!.Message, Does.Contain("missing_item"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证启用热重载后，配置文件内容变更会刷新已注册配置表。
    /// </summary>
    [Test]
    public async Task EnableHotReload_Should_Update_Registered_Table_When_Config_File_Changes()
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
              "required": ["id", "name"],
              "properties": {
                "id": { "type": "integer" },
                "name": { "type": "string" },
                "hp": { "type": "integer" }
              }
            }
            """);

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterConfigStub>("monster", "monster", "schemas/monster.schema.json",
                static config => config.Id);
        var registry = new ConfigRegistry();
        await loader.LoadAsync(registry);

        var reloadTaskSource = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var hotReload = loader.EnableHotReload(
            registry,
            onTableReloaded: tableName => reloadTaskSource.TrySetResult(tableName),
            debounceDelay: TimeSpan.FromMilliseconds(150));

        try
        {
            CreateConfigFile(
                "monster/slime.yaml",
                """
                id: 1
                name: Slime
                hp: 25
                """);

            var tableName = await WaitForTaskWithinAsync(reloadTaskSource.Task, TimeSpan.FromSeconds(5));

            Assert.Multiple(() =>
            {
                Assert.That(tableName, Is.EqualTo("monster"));
                Assert.That(registry.GetTable<int, MonsterConfigStub>("monster").Get(1).Hp, Is.EqualTo(25));
            });
        }
        finally
        {
            hotReload.UnRegister();
        }
    }

    /// <summary>
    ///     验证热重载支持通过选项对象配置回调和防抖延迟。
    /// </summary>
    [Test]
    public async Task EnableHotReload_Should_Support_Options_Object()
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
              "required": ["id", "name"],
              "properties": {
                "id": { "type": "integer" },
                "name": { "type": "string" },
                "hp": { "type": "integer" }
              }
            }
            """);

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable(
                new YamlConfigTableRegistrationOptions<int, MonsterConfigStub>(
                    "monster",
                    "monster",
                    static config => config.Id)
                {
                    SchemaRelativePath = "schemas/monster.schema.json"
                });
        var registry = new ConfigRegistry();
        await loader.LoadAsync(registry);

        var reloadTaskSource = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var hotReload = loader.EnableHotReload(
            registry,
            new YamlConfigHotReloadOptions
            {
                OnTableReloaded = tableName => reloadTaskSource.TrySetResult(tableName),
                DebounceDelay = TimeSpan.FromMilliseconds(150)
            });

        try
        {
            CreateConfigFile(
                "monster/slime.yaml",
                """
                id: 1
                name: Slime
                hp: 25
                """);

            var tableName = await WaitForTaskWithinAsync(reloadTaskSource.Task, TimeSpan.FromSeconds(5));

            Assert.Multiple(() =>
            {
                Assert.That(tableName, Is.EqualTo("monster"));
                Assert.That(registry.GetTable<int, MonsterConfigStub>("monster").Get(1).Hp, Is.EqualTo(25));
            });
        }
        finally
        {
            hotReload.UnRegister();
        }
    }

    /// <summary>
    ///     验证热重载会在启动前拒绝负的防抖延迟，避免后台延迟任务才暴露参数错误。
    /// </summary>
    [Test]
    public void EnableHotReload_Should_Throw_When_Debounce_Delay_Is_Negative()
    {
        var loader = new YamlConfigLoader(_rootPath);
        var registry = new ConfigRegistry();

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            loader.EnableHotReload(
                registry,
                new YamlConfigHotReloadOptions
                {
                    DebounceDelay = TimeSpan.FromMilliseconds(-1)
                }));

        Assert.That(exception!.ParamName, Is.EqualTo("options"));
    }

    /// <summary>
    ///     验证热重载失败时会保留旧表状态，并通过失败回调暴露诊断信息。
    /// </summary>
    [Test]
    public async Task EnableHotReload_Should_Keep_Previous_Table_When_Schema_Change_Makes_Reload_Fail()
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
              "required": ["id", "name"],
              "properties": {
                "id": { "type": "integer" },
                "name": { "type": "string" },
                "hp": { "type": "integer" }
              }
            }
            """);

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterConfigStub>("monster", "monster", "schemas/monster.schema.json",
                static config => config.Id);
        var registry = new ConfigRegistry();
        await loader.LoadAsync(registry);

        var reloadFailureTaskSource =
            new TaskCompletionSource<(string TableName, Exception Exception)>(TaskCreationOptions
                .RunContinuationsAsynchronously);
        var hotReload = loader.EnableHotReload(
            registry,
            onTableReloadFailed: (tableName, exception) =>
                reloadFailureTaskSource.TrySetResult((tableName, exception)),
            debounceDelay: TimeSpan.FromMilliseconds(150));

        try
        {
            CreateSchemaFile(
                "schemas/monster.schema.json",
                """
                {
                  "type": "object",
                  "required": ["id", "name", "rarity"],
                  "properties": {
                    "id": { "type": "integer" },
                    "name": { "type": "string" },
                    "hp": { "type": "integer" },
                    "rarity": { "type": "string" }
                  }
                }
                """);

            var failure = await WaitForTaskWithinAsync(reloadFailureTaskSource.Task, TimeSpan.FromSeconds(5));
            var diagnosticException = failure.Exception as ConfigLoadException;

            Assert.Multiple(() =>
            {
                Assert.That(failure.TableName, Is.EqualTo("monster"));
                Assert.That(failure.Exception.Message, Does.Contain("rarity"));
                Assert.That(diagnosticException, Is.Not.Null);
                Assert.That(diagnosticException!.Diagnostic.FailureKind,
                    Is.EqualTo(ConfigLoadFailureKind.MissingRequiredProperty));
                Assert.That(diagnosticException.Diagnostic.TableName, Is.EqualTo("monster"));
                Assert.That(diagnosticException.Diagnostic.DisplayPath, Is.EqualTo("rarity"));
                Assert.That(registry.GetTable<int, MonsterConfigStub>("monster").Get(1).Hp, Is.EqualTo(10));
            });
        }
        finally
        {
            hotReload.UnRegister();
        }
    }

    /// <summary>
    ///     验证当被引用表变更导致依赖表引用失效时，热重载会整体回滚受影响表。
    /// </summary>
    [Test]
    public async Task EnableHotReload_Should_Keep_Previous_State_When_Dependency_Table_Breaks_Cross_Table_Reference()
    {
        CreateConfigFile(
            "item/potion.yaml",
            """
            id: potion
            name: Potion
            """);
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            name: Slime
            dropItemId: potion
            """);
        CreateSchemaFile(
            "schemas/item.schema.json",
            """
            {
              "type": "object",
              "required": ["id", "name"],
              "properties": {
                "id": { "type": "string" },
                "name": { "type": "string" }
              }
            }
            """);
        CreateSchemaFile(
            "schemas/monster.schema.json",
            """
            {
              "type": "object",
              "required": ["id", "name", "dropItemId"],
              "properties": {
                "id": { "type": "integer" },
                "name": { "type": "string" },
                "dropItemId": {
                  "type": "string",
                  "x-gframework-ref-table": "item"
                }
              }
            }
            """);

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<string, ItemConfigStub>("item", "item", "schemas/item.schema.json",
                static config => config.Id)
            .RegisterTable<int, MonsterDropConfigStub>("monster", "monster", "schemas/monster.schema.json",
                static config => config.Id);
        var registry = new ConfigRegistry();
        await loader.LoadAsync(registry);

        var reloadFailureTaskSource =
            new TaskCompletionSource<(string TableName, Exception Exception)>(TaskCreationOptions
                .RunContinuationsAsynchronously);
        var hotReload = loader.EnableHotReload(
            registry,
            onTableReloadFailed: (tableName, exception) =>
                reloadFailureTaskSource.TrySetResult((tableName, exception)),
            debounceDelay: TimeSpan.FromMilliseconds(150));

        try
        {
            CreateConfigFile(
                "item/potion.yaml",
                """
                id: elixir
                name: Elixir
                """);

            var failure = await WaitForTaskWithinAsync(reloadFailureTaskSource.Task, TimeSpan.FromSeconds(5));
            var diagnosticException = failure.Exception as ConfigLoadException;

            Assert.Multiple(() =>
            {
                Assert.That(failure.TableName, Is.EqualTo("item"));
                Assert.That(failure.Exception.Message, Does.Contain("dropItemId"));
                Assert.That(diagnosticException, Is.Not.Null);
                Assert.That(diagnosticException!.Diagnostic.FailureKind,
                    Is.EqualTo(ConfigLoadFailureKind.ReferencedKeyNotFound));
                Assert.That(diagnosticException.Diagnostic.TableName, Is.EqualTo("monster"));
                Assert.That(diagnosticException.Diagnostic.ReferencedTableName, Is.EqualTo("item"));
                Assert.That(diagnosticException.Diagnostic.DisplayPath, Is.EqualTo("dropItemId"));
                Assert.That(diagnosticException.Diagnostic.RawValue, Is.EqualTo("potion"));
                Assert.That(registry.GetTable<string, ItemConfigStub>("item").ContainsKey("potion"), Is.True);
                Assert.That(registry.GetTable<int, MonsterDropConfigStub>("monster").Get(1).DropItemId,
                    Is.EqualTo("potion"));
            });
        }
        finally
        {
            hotReload.UnRegister();
        }
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
    ///     创建测试用 schema 文件。
    /// </summary>
    /// <param name="relativePath">相对根目录的文件路径。</param>
    /// <param name="content">文件内容。</param>
    private void CreateSchemaFile(string relativePath, string content)
    {
        CreateConfigFile(relativePath, content);
    }

    /// <summary>
    ///     在限定时间内等待异步任务完成，避免文件监听测试无限挂起。
    /// </summary>
    /// <typeparam name="T">任务结果类型。</typeparam>
    /// <param name="task">要等待的任务。</param>
    /// <param name="timeout">超时时间。</param>
    /// <returns>任务结果。</returns>
    private static async Task<T> WaitForTaskWithinAsync<T>(Task<T> task, TimeSpan timeout)
    {
        var completedTask = await Task.WhenAny(task, Task.Delay(timeout));
        if (!ReferenceEquals(completedTask, task))
        {
            Assert.Fail($"Timed out after {timeout} while waiting for file watcher notification.");
        }

        return await task;
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
    ///     用于数组 schema 校验测试的最小怪物配置类型。
    /// </summary>
    private sealed class MonsterConfigIntegerArrayStub
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
        ///     获取或设置掉落率列表。
        /// </summary>
        public IReadOnlyList<int> DropRates { get; set; } = Array.Empty<int>();
    }

    /// <summary>
    ///     用于嵌套对象 schema 校验测试的最小怪物配置类型。
    /// </summary>
    private sealed class MonsterNestedConfigStub
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
        ///     获取或设置奖励对象。
        /// </summary>
        public RewardConfigStub Reward { get; set; } = new();
    }

    /// <summary>
    ///     表示嵌套奖励对象的测试桩类型。
    /// </summary>
    private sealed class RewardConfigStub
    {
        /// <summary>
        ///     获取或设置金币数量。
        /// </summary>
        public int Gold { get; set; }

        /// <summary>
        ///     获取或设置货币类型。
        /// </summary>
        public string Currency { get; set; } = string.Empty;
    }

    /// <summary>
    ///     用于对象数组 schema 校验测试的怪物配置类型。
    /// </summary>
    private sealed class MonsterPhaseArrayConfigStub
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
        ///     获取或设置阶段数组。
        /// </summary>
        public IReadOnlyList<PhaseConfigStub> Phases { get; set; } = Array.Empty<PhaseConfigStub>();
    }

    /// <summary>
    ///     表示对象数组中的阶段元素。
    /// </summary>
    private sealed class PhaseConfigStub
    {
        /// <summary>
        ///     获取或设置波次编号。
        /// </summary>
        public int Wave { get; set; }

        /// <summary>
        ///     获取或设置怪物主键。
        /// </summary>
        public string MonsterId { get; set; } = string.Empty;
    }

    /// <summary>
    ///     用于深层跨表引用测试的怪物配置类型。
    /// </summary>
    private sealed class MonsterPhaseDropConfigStub
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
        ///     获取或设置阶段数组。
        /// </summary>
        public List<PhaseDropConfigStub> Phases { get; set; } = new();
    }

    /// <summary>
    ///     表示带有掉落引用的阶段元素。
    /// </summary>
    private sealed class PhaseDropConfigStub
    {
        /// <summary>
        ///     获取或设置波次编号。
        /// </summary>
        public int Wave { get; set; }

        /// <summary>
        ///     获取或设置掉落物品主键。
        /// </summary>
        public string DropItemId { get; set; } = string.Empty;
    }

    /// <summary>
    ///     用于跨表引用测试的最小物品配置类型。
    /// </summary>
    private sealed class ItemConfigStub
    {
        /// <summary>
        ///     获取或设置主键。
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        ///     获取或设置名称。
        /// </summary>
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    ///     用于单值跨表引用测试的怪物配置类型。
    /// </summary>
    private sealed class MonsterDropConfigStub
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
        ///     获取或设置掉落物品主键。
        /// </summary>
        public string DropItemId { get; set; } = string.Empty;
    }

    /// <summary>
    ///     用于数组跨表引用测试的怪物配置类型。
    /// </summary>
    private sealed class MonsterDropArrayConfigStub
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
        ///     获取或设置掉落物品主键列表。
        /// </summary>
        public List<string> DropItemIds { get; set; } = new();
    }

    /// <summary>
    ///     用于验证注册表一致性的现有配置类型。
    /// </summary>
    /// <param name="Id">配置主键。</param>
    /// <param name="Name">配置名称。</param>
    private sealed record ExistingConfigStub(int Id, string Name);
}