// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using System.Reflection;
using System.Threading;
using GFramework.Core.Abstractions.Events;
using GFramework.Game.Abstractions.Config;
using GFramework.Game.Config;
using YamlDotNet.Serialization;

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

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

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

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

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

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

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

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

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

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

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

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Message, Does.Contain("common"));
            Assert.That(exception!.Message, Does.Contain("rare"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证标量 <c>const</c> 限制会在运行时被拒绝。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_Scalar_Value_Does_Not_Match_Schema_Const()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            name: Slime
            rarity: rare
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
                  "const": "common"
                }
              }
            }
            """);

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterConfigStub>("monster", "monster", "schemas/monster.schema.json",
                static config => config.Id);
        var registry = new ConfigRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Message, Does.Contain("constant value"));
            Assert.That(exception.Message, Does.Contain("\"common\""));
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

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

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
    ///     验证数值命中开区间下界时会按 schema 在运行时被拒绝。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_Number_Violates_Exclusive_Minimum()
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
                "name": { "type": "string" },
                "hp": {
                  "type": "integer",
                  "exclusiveMinimum": 10,
                  "exclusiveMaximum": 100
                }
              }
            }
            """);

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterConfigStub>("monster", "monster", "schemas/monster.schema.json",
                static config => config.Id);
        var registry = new ConfigRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.ConstraintViolation));
            Assert.That(exception.Diagnostic.DisplayPath, Is.EqualTo("hp"));
            Assert.That(exception.Diagnostic.RawValue, Is.EqualTo("10"));
            Assert.That(exception.Message, Does.Contain("greater than 10"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证数值命中开区间上界时会按 schema 在运行时被拒绝。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_Number_Violates_Exclusive_Maximum()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            name: Slime
            hp: 100
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
                  "exclusiveMinimum": 10,
                  "exclusiveMaximum": 100
                }
              }
            }
            """);

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterConfigStub>("monster", "monster", "schemas/monster.schema.json",
                static config => config.Id);
        var registry = new ConfigRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.ConstraintViolation));
            Assert.That(exception.Diagnostic.DisplayPath, Is.EqualTo("hp"));
            Assert.That(exception.Diagnostic.RawValue, Is.EqualTo("100"));
            Assert.That(exception.Message, Does.Contain("less than 100"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证数值不满足 <c>multipleOf</c> 时会在运行时被拒绝。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_Number_Violates_MultipleOf()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            name: Slime
            hp: 12
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
                  "multipleOf": 5
                }
              }
            }
            """);

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterConfigStub>("monster", "monster", "schemas/monster.schema.json",
                static config => config.Id);
        var registry = new ConfigRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.ConstraintViolation));
            Assert.That(exception.Diagnostic.DisplayPath, Is.EqualTo("hp"));
            Assert.That(exception.Diagnostic.RawValue, Is.EqualTo("12"));
            Assert.That(exception.Message, Does.Contain("multiple of 5"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证大数值配合十进制步进时，会按十进制精确整倍数规则被运行时接受。
    /// </summary>
    [Test]
    public async Task LoadAsync_Should_Accept_Large_Decimal_Number_When_MultipleOf_Matches_Exact_Decimal_Step()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            dropRate: 10000000.2
            """);
        CreateSchemaFile(
            "schemas/monster.schema.json",
            """
            {
              "type": "object",
              "required": ["id", "dropRate"],
              "properties": {
                "id": { "type": "integer" },
                "dropRate": {
                  "type": "number",
                  "multipleOf": 0.1
                }
              }
            }
            """);

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterNumberConfigStub>("monster", "monster", "schemas/monster.schema.json",
                static config => config.Id);
        var registry = new ConfigRegistry();

        await loader.LoadAsync(registry);

        var table = registry.GetTable<int, MonsterNumberConfigStub>("monster");

        Assert.Multiple(() =>
        {
            Assert.That(table.Count, Is.EqualTo(1));
            Assert.That(table.Get(1).DropRate, Is.EqualTo(10000000.2d));
        });
    }

    /// <summary>
    ///     验证大数量级但实际不满足 <c>multipleOf</c> 的数值会被运行时拒绝。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_Large_Number_Is_Not_Actually_MultipleOf()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            dropRate: 1000000000000.4
            """);
        CreateSchemaFile(
            "schemas/monster.schema.json",
            """
            {
              "type": "object",
              "required": ["id", "dropRate"],
              "properties": {
                "id": { "type": "integer" },
                "dropRate": {
                  "type": "number",
                  "multipleOf": 1
                }
              }
            }
            """);

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterNumberConfigStub>("monster", "monster", "schemas/monster.schema.json",
                static config => config.Id);
        var registry = new ConfigRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.ConstraintViolation));
            Assert.That(exception.Diagnostic.DisplayPath, Is.EqualTo("dropRate"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证科学计数法数值会按 <c>number</c> 类型被运行时接受。
    /// </summary>
    [Test]
    public async Task LoadAsync_Should_Accept_Scientific_Notation_Number()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            dropRate: 1.5e10
            """);
        CreateSchemaFile(
            "schemas/monster.schema.json",
            """
            {
              "type": "object",
              "required": ["id", "dropRate"],
              "properties": {
                "id": { "type": "integer" },
                "dropRate": { "type": "number" }
              }
            }
            """);

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterNumberConfigStub>("monster", "monster", "schemas/monster.schema.json",
                static config => config.Id);
        var registry = new ConfigRegistry();

        await loader.LoadAsync(registry);

        var table = registry.GetTable<int, MonsterNumberConfigStub>("monster");

        Assert.Multiple(() =>
        {
            Assert.That(table.Count, Is.EqualTo(1));
            Assert.That(table.Get(1).DropRate, Is.EqualTo(1.5e10));
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

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

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
    ///     验证字符串正则模式约束会在运行时被统一拒绝。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_String_Does_Not_Match_Pattern()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            name: slime
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
                  "pattern": "^[A-Z][a-z]+$"
                },
                "hp": { "type": "integer" }
              }
            }
            """);

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterConfigStub>("monster", "monster", "schemas/monster.schema.json",
                static config => config.Id);
        var registry = new ConfigRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.ConstraintViolation));
            Assert.That(exception.Diagnostic.DisplayPath, Is.EqualTo("name"));
            Assert.That(exception.Diagnostic.RawValue, Is.EqualTo("slime"));
            Assert.That(exception.Message, Does.Contain("regular expression"));
            Assert.That(exception.Message, Does.Contain("^[A-Z][a-z]+$"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证运行时会接受当前共享支持的字符串 <c>format</c> 子集。
    /// </summary>
    /// <param name="formatName">schema 中声明的 format 名称。</param>
    /// <param name="value">满足该 format 的 YAML 标量值。</param>
    [TestCase("date", "2026-04-11")]
    [TestCase("date-time", "2026-04-11T08:30:00Z")]
    [TestCase("duration", "P2DT3H4M5.5S")]
    [TestCase("email", "boss@example.com")]
    [TestCase("time", "08:30:00Z")]
    [TestCase("uri", "https://example.com/loot-table")]
    [TestCase("uuid", "123e4567-e89b-12d3-a456-426614174000")]
    public async Task LoadAsync_Should_Accept_Supported_String_Format(
        string formatName,
        string value)
    {
        CreateConfigFile(
            "monster/slime.yaml",
            $$"""
              id: 1
              name: {{value}}
              hp: 10
              """);
        CreateSchemaFile(
            "schemas/monster.schema.json",
            $$"""
              {
                "type": "object",
                "required": ["id", "name", "hp"],
                "properties": {
                  "id": { "type": "integer" },
                  "name": {
                    "type": "string",
                    "format": "{{formatName}}"
                  },
                  "hp": { "type": "integer" }
                }
              }
              """);

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterConfigStub>("monster", "monster", "schemas/monster.schema.json",
                static config => config.Id);
        var registry = new ConfigRegistry();

        await loader.LoadAsync(registry);

        var table = registry.GetTable<int, MonsterConfigStub>("monster");
        Assert.Multiple(() =>
        {
            Assert.That(table.Count, Is.EqualTo(1));
            Assert.That(table.Get(1).Name, Is.EqualTo(value));
        });
    }

    /// <summary>
    ///     验证运行时会拒绝不满足共享字符串 <c>format</c> 子集的值。
    /// </summary>
    /// <param name="formatName">schema 中声明的 format 名称。</param>
    /// <param name="value">不满足该 format 的 YAML 标量值。</param>
    [TestCase("date", "2026-02-30")]
    [TestCase("date-time", "2026-04-11T08:30:00")]
    [TestCase("duration", "P1Y")]
    [TestCase("email", "boss.example.com")]
    [TestCase("time", "08:30:00")]
    [TestCase("uri", "/loot-table")]
    [TestCase("uuid", "123e4567e89b12d3a456426614174000")]
    public void LoadAsync_Should_Throw_When_String_Does_Not_Match_Supported_Format(
        string formatName,
        string value)
    {
        CreateConfigFile(
            "monster/slime.yaml",
            $$"""
              id: 1
              name: {{value}}
              hp: 10
              """);
        CreateSchemaFile(
            "schemas/monster.schema.json",
            $$"""
              {
                "type": "object",
                "required": ["id", "name", "hp"],
                "properties": {
                  "id": { "type": "integer" },
                  "name": {
                    "type": "string",
                    "format": "{{formatName}}"
                  },
                  "hp": { "type": "integer" }
                }
              }
              """);

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterConfigStub>("monster", "monster", "schemas/monster.schema.json",
                static config => config.Id);
        var registry = new ConfigRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.ConstraintViolation));
            Assert.That(exception.Diagnostic.DisplayPath, Is.EqualTo("name"));
            Assert.That(exception.Diagnostic.RawValue, Is.EqualTo(value));
            Assert.That(exception.Message, Does.Contain("string format"));
            Assert.That(exception.Message, Does.Contain(formatName));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证 schema 使用当前未支持的字符串 <c>format</c> 时会在解析阶段被拒绝。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_String_Format_Is_Not_Supported()
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
                  "format": "ipv4"
                },
                "hp": { "type": "integer" }
              }
            }
            """);

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterConfigStub>("monster", "monster", "schemas/monster.schema.json",
                static config => config.Id);
        var registry = new ConfigRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.SchemaUnsupported));
            Assert.That(exception.Diagnostic.DisplayPath, Is.EqualTo("name"));
            Assert.That(exception.Diagnostic.RawValue, Is.EqualTo("ipv4"));
            Assert.That(exception.Message, Does.Contain("unsupported string format"));
            Assert.That(exception.Message, Does.Contain("date-time"));
            Assert.That(exception.Message, Does.Contain("duration"));
            Assert.That(exception.Message, Does.Contain("time"));
        });
    }

    /// <summary>
    ///     验证 schema 在非字符串节点上声明 <c>format</c> 时，会在 schema 解析阶段被拒绝。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_Format_Is_Used_On_Non_String_Property()
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
              "required": ["id", "hp"],
              "properties": {
                "id": { "type": "integer" },
                "hp": {
                  "type": "integer",
                  "format": "uuid"
                }
              }
            }
            """);

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterConfigStub>("monster", "monster", "schemas/monster.schema.json",
                static config => config.Id);
        var registry = new ConfigRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.SchemaUnsupported));
            Assert.That(exception.Diagnostic.DisplayPath, Is.EqualTo("hp"));
            Assert.That(exception.Message, Does.Contain("only 'string' scalar types support string formats"));
        });
    }

    /// <summary>
    ///     验证 schema 将 <c>format</c> 声明为非字符串值时，会在 schema 解析阶段被拒绝。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_Format_Is_Not_A_String()
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
                  "format": 123
                },
                "hp": { "type": "integer" }
              }
            }
            """);

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterConfigStub>("monster", "monster", "schemas/monster.schema.json",
                static config => config.Id);
        var registry = new ConfigRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.SchemaUnsupported));
            Assert.That(exception.Diagnostic.DisplayPath, Is.EqualTo("name"));
            Assert.That(exception.Message, Does.Contain("must declare 'format' as a string"));
        });
    }

    /// <summary>
    ///     验证运行时 schema 校验与 JS 工具对反向引用模式保持一致。
    /// </summary>
    [Test]
    public async Task LoadAsync_Should_Accept_Backreference_Pattern_When_Value_Matches()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            name: aa
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
                  "pattern": "^(a)\\1$"
                },
                "hp": { "type": "integer" }
              }
            }
            """);

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterConfigStub>("monster", "monster", "schemas/monster.schema.json",
                static config => config.Id);
        var registry = new ConfigRegistry();

        await loader.LoadAsync(registry);

        var table = registry.GetTable<int, MonsterConfigStub>("monster");

        Assert.Multiple(() =>
        {
            Assert.That(table.Count, Is.EqualTo(1));
            Assert.That(table.Get(1).Name, Is.EqualTo("aa"));
        });
    }

    /// <summary>
    ///     验证数组元素数量命中上界时会在运行时被统一拒绝。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_Array_Violates_MaxItems()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            name: Slime
            dropRates:
              - 1
              - 2
              - 3
              - 4
            """);
        CreateSchemaFile(
            "schemas/monster.schema.json",
            """
            {
              "type": "object",
              "required": ["id", "name", "dropRates"],
              "properties": {
                "id": { "type": "integer" },
                "name": { "type": "string" },
                "dropRates": {
                  "type": "array",
                  "minItems": 1,
                  "maxItems": 3,
                  "items": {
                    "type": "integer"
                  }
                }
              }
            }
            """);

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterConfigIntegerArrayStub>("monster", "monster", "schemas/monster.schema.json",
                static config => config.Id);
        var registry = new ConfigRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.ConstraintViolation));
            Assert.That(exception.Diagnostic.DisplayPath, Is.EqualTo("dropRates"));
            Assert.That(exception.Diagnostic.RawValue, Is.EqualTo("4"));
            Assert.That(exception.Message, Does.Contain("at most 3 items"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证数组元素数量命中下界时会在运行时被统一拒绝。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_Array_Violates_MinItems()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            name: Slime
            dropRates: []
            """);
        CreateSchemaFile(
            "schemas/monster.schema.json",
            """
            {
              "type": "object",
              "required": ["id", "name", "dropRates"],
              "properties": {
                "id": { "type": "integer" },
                "name": { "type": "string" },
                "dropRates": {
                  "type": "array",
                  "minItems": 1,
                  "maxItems": 3,
                  "items": {
                    "type": "integer"
                  }
                }
              }
            }
            """);

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterConfigIntegerArrayStub>("monster", "monster", "schemas/monster.schema.json",
                static config => config.Id);
        var registry = new ConfigRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.ConstraintViolation));
            Assert.That(exception.Diagnostic.DisplayPath, Is.EqualTo("dropRates"));
            Assert.That(exception.Diagnostic.RawValue, Is.EqualTo("0"));
            Assert.That(exception.Message, Does.Contain("at least 1 items"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证数组声明 <c>uniqueItems</c> 后，重复元素会在运行时被拒绝。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_Array_Violates_UniqueItems()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            name: Slime
            dropRates:
              - 5
              - 10
              - 5
            """);
        CreateSchemaFile(
            "schemas/monster.schema.json",
            """
            {
              "type": "object",
              "required": ["id", "name", "dropRates"],
              "properties": {
                "id": { "type": "integer" },
                "name": { "type": "string" },
                "dropRates": {
                  "type": "array",
                  "uniqueItems": true,
                  "items": {
                    "type": "integer"
                  }
                }
              }
            }
            """);

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterConfigIntegerArrayStub>("monster", "monster", "schemas/monster.schema.json",
                static config => config.Id);
        var registry = new ConfigRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.ConstraintViolation));
            Assert.That(exception.Diagnostic.DisplayPath, Is.EqualTo("dropRates[2]"));
            Assert.That(exception.Diagnostic.RawValue, Is.EqualTo("5"));
            Assert.That(exception.Message, Does.Contain("unique array items"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证数组声明 <c>contains</c> 后，默认至少要有一个匹配元素。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_Array_Violates_Default_Contains_Match_Count()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            name: Slime
            dropRates:
              - 1
              - 2
            """);
        CreateSchemaFile(
            "schemas/monster.schema.json",
            """
            {
              "type": "object",
              "required": ["id", "name", "dropRates"],
              "properties": {
                "id": { "type": "integer" },
                "name": { "type": "string" },
                "dropRates": {
                  "type": "array",
                  "contains": {
                    "type": "integer",
                    "const": 5
                  },
                  "items": {
                    "type": "integer"
                  }
                }
              }
            }
            """);

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterConfigIntegerArrayStub>("monster", "monster", "schemas/monster.schema.json",
                static config => config.Id);
        var registry = new ConfigRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.ConstraintViolation));
            Assert.That(exception.Diagnostic.DisplayPath, Is.EqualTo("dropRates"));
            Assert.That(exception.Diagnostic.RawValue, Is.EqualTo("0"));
            Assert.That(exception.Message, Does.Contain("at least 1 items matching the 'contains' schema"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证数组声明 <c>minContains</c> 后，会按匹配数量而不是总元素数做约束判断。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_Array_Violates_MinContains()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            name: Slime
            dropRates:
              - 5
              - 7
              - 9
            """);
        CreateSchemaFile(
            "schemas/monster.schema.json",
            """
            {
              "type": "object",
              "required": ["id", "name", "dropRates"],
              "properties": {
                "id": { "type": "integer" },
                "name": { "type": "string" },
                "dropRates": {
                  "type": "array",
                  "minContains": 2,
                  "contains": {
                    "type": "integer",
                    "const": 5
                  },
                  "items": {
                    "type": "integer"
                  }
                }
              }
            }
            """);

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterConfigIntegerArrayStub>("monster", "monster", "schemas/monster.schema.json",
                static config => config.Id);
        var registry = new ConfigRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.ConstraintViolation));
            Assert.That(exception.Diagnostic.DisplayPath, Is.EqualTo("dropRates"));
            Assert.That(exception.Diagnostic.RawValue, Is.EqualTo("1"));
            Assert.That(exception.Message, Does.Contain("at least 2 items matching the 'contains' schema"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证数组声明 <c>maxContains</c> 后，会拒绝匹配元素过多的序列。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_Array_Violates_MaxContains()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            name: Slime
            dropRates:
              - 5
              - 5
              - 7
            """);
        CreateSchemaFile(
            "schemas/monster.schema.json",
            """
            {
              "type": "object",
              "required": ["id", "name", "dropRates"],
              "properties": {
                "id": { "type": "integer" },
                "name": { "type": "string" },
                "dropRates": {
                  "type": "array",
                  "maxContains": 1,
                  "contains": {
                    "type": "integer",
                    "const": 5
                  },
                  "items": {
                    "type": "integer"
                  }
                }
              }
            }
            """);

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterConfigIntegerArrayStub>("monster", "monster", "schemas/monster.schema.json",
                static config => config.Id);
        var registry = new ConfigRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.ConstraintViolation));
            Assert.That(exception.Diagnostic.DisplayPath, Is.EqualTo("dropRates"));
            Assert.That(exception.Diagnostic.RawValue, Is.EqualTo("2"));
            Assert.That(exception.Message, Does.Contain("at most 1 items matching the 'contains' schema"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证匹配数量刚好等于 <c>minContains</c> / <c>maxContains</c> 时会被视为合法边界。
    /// </summary>
    [Test]
    public async Task LoadAsync_Should_Accept_Array_When_Contains_Match_Count_Equals_Min_And_Max_Bounds()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            name: Slime
            dropRates:
              - 5
              - 7
              - 5
            """);
        CreateSchemaFile(
            "schemas/monster.schema.json",
            """
            {
              "type": "object",
              "required": ["id", "name", "dropRates"],
              "properties": {
                "id": { "type": "integer" },
                "name": { "type": "string" },
                "dropRates": {
                  "type": "array",
                  "minContains": 2,
                  "maxContains": 2,
                  "contains": {
                    "type": "integer",
                    "const": 5
                  },
                  "items": {
                    "type": "integer"
                  }
                }
              }
            }
            """);

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterConfigIntegerArrayStub>("monster", "monster", "schemas/monster.schema.json",
                static config => config.Id);
        var registry = new ConfigRegistry();

        await loader.LoadAsync(registry);

        var table = registry.GetTable<int, MonsterConfigIntegerArrayStub>("monster");

        Assert.Multiple(() =>
        {
            Assert.That(table.Count, Is.EqualTo(1));
            Assert.That(table.Get(1).DropRates, Is.EqualTo(new[] { 5, 7, 5 }));
        });
    }

    /// <summary>
    ///     验证数组字段将 <c>contains</c> 声明为非对象 schema 时，会在 schema 解析阶段被拒绝。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_Contains_Is_Not_Object_Schema()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            name: Slime
            dropRates:
              - 5
            """);
        CreateSchemaFile(
            "schemas/monster.schema.json",
            """
            {
              "type": "object",
              "required": ["id", "name", "dropRates"],
              "properties": {
                "id": { "type": "integer" },
                "name": { "type": "string" },
                "dropRates": {
                  "type": "array",
                  "contains": 5,
                  "items": {
                    "type": "integer"
                  }
                }
              }
            }
            """);

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterConfigIntegerArrayStub>("monster", "monster", "schemas/monster.schema.json",
                static config => config.Id);
        var registry = new ConfigRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.SchemaUnsupported));
            Assert.That(exception.Diagnostic.DisplayPath, Is.EqualTo("dropRates"));
            Assert.That(exception.Message, Does.Contain("'contains' as an object-valued schema"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证数组字段将 <c>contains</c> 声明为嵌套数组 schema 时，会在 schema 解析阶段被拒绝。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_Contains_Uses_Nested_Array_Schema()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            name: Slime
            dropRates:
              - 5
            """);
        CreateSchemaFile(
            "schemas/monster.schema.json",
            """
            {
              "type": "object",
              "required": ["id", "name", "dropRates"],
              "properties": {
                "id": { "type": "integer" },
                "name": { "type": "string" },
                "dropRates": {
                  "type": "array",
                  "contains": {
                    "type": "array",
                    "items": {
                      "type": "integer"
                    }
                  },
                  "items": {
                    "type": "integer"
                  }
                }
              }
            }
            """);

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterConfigIntegerArrayStub>("monster", "monster", "schemas/monster.schema.json",
                static config => config.Id);
        var registry = new ConfigRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.SchemaUnsupported));
            Assert.That(exception.Diagnostic.DisplayPath, Is.EqualTo("dropRates"));
            Assert.That(exception.Message, Does.Contain("unsupported nested array 'contains' schemas"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证对象数组的 <c>contains</c> 试匹配会按声明属性子集工作，而不会因额外字段误判为不匹配。
    /// </summary>
    [Test]
    public async Task LoadAsync_Should_Accept_Object_Array_When_Contains_Matches_Declared_Subset_Properties()
    {
        var loader = CreateLoaderForContainsSubsetObjectArrayScenario();
        var registry = new ConfigRegistry();

        await loader.LoadAsync(registry);

        var table = registry.GetTable<int, MonsterWeightedEntryArrayConfigStub>("monster");

        Assert.Multiple(() =>
        {
            Assert.That(table.Count, Is.EqualTo(1));
            Assert.That(table.Get(1).Entries.Count, Is.EqualTo(2));
            Assert.That(table.Get(1).Entries[0].Id, Is.EqualTo(1));
            Assert.That(table.Get(1).Entries[0].Weight, Is.EqualTo(2));
        });
    }

    /// <summary>
    ///     验证数组在未声明 <c>contains</c> 时不能单独使用 <c>minContains</c>。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_MinContains_Is_Declared_Without_Contains()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            name: Slime
            dropRates:
              - 5
            """);
        CreateSchemaFile(
            "schemas/monster.schema.json",
            """
            {
              "type": "object",
              "required": ["id", "name", "dropRates"],
              "properties": {
                "id": { "type": "integer" },
                "name": { "type": "string" },
                "dropRates": {
                  "type": "array",
                  "minContains": 1,
                  "items": {
                    "type": "integer"
                  }
                }
              }
            }
            """);

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterConfigIntegerArrayStub>("monster", "monster", "schemas/monster.schema.json",
                static config => config.Id);
        var registry = new ConfigRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.SchemaUnsupported));
            Assert.That(exception.Diagnostic.DisplayPath, Is.EqualTo("dropRates"));
            Assert.That(exception.Message, Does.Contain("minContains"));
            Assert.That(exception.Message, Does.Contain("without a companion 'contains' schema"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证数组字段将 <c>minContains</c> 声明为大于 <c>maxContains</c> 时，会在 schema 解析阶段被拒绝。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_Array_Contains_Count_Constraints_Are_Inverted()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            name: Slime
            dropRates:
              - 5
            """);
        CreateSchemaFile(
            "schemas/monster.schema.json",
            """
            {
              "type": "object",
              "required": ["id", "name", "dropRates"],
              "properties": {
                "id": { "type": "integer" },
                "name": { "type": "string" },
                "dropRates": {
                  "type": "array",
                  "minContains": 2,
                  "maxContains": 1,
                  "contains": {
                    "type": "integer",
                    "const": 5
                  },
                  "items": {
                    "type": "integer"
                  }
                }
              }
            }
            """);

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterConfigIntegerArrayStub>("monster", "monster", "schemas/monster.schema.json",
                static config => config.Id);
        var registry = new ConfigRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.SchemaUnsupported));
            Assert.That(exception.Diagnostic.DisplayPath, Is.EqualTo("dropRates"));
            Assert.That(exception.Message, Does.Contain("minContains"));
            Assert.That(exception.Message, Does.Contain("greater than 'maxContains'"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证 <c>uniqueItems</c> 的归一化键不会把带分隔符的不同对象值误判为重复项。
    /// </summary>
    [Test]
    public async Task LoadAsync_Should_Accept_Distinct_Object_Items_When_Comparable_Values_Contain_Separators()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            entries:
              -
                a: "x|1:b=string:yz"
              -
                a: x
                b: yz
            """);
        CreateSchemaFile(
            "schemas/monster.schema.json",
            """
            {
              "type": "object",
              "required": ["id", "entries"],
              "properties": {
                "id": { "type": "integer" },
                "entries": {
                  "type": "array",
                  "uniqueItems": true,
                  "items": {
                    "type": "object",
                    "properties": {
                      "a": { "type": "string" },
                      "b": { "type": "string" }
                    }
                  }
                }
              }
            }
            """);

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterComparableEntryArrayConfigStub>(
                "monster",
                "monster",
                "schemas/monster.schema.json",
                static config => config.Id);
        var registry = new ConfigRegistry();

        await loader.LoadAsync(registry);

        var table = registry.GetTable<int, MonsterComparableEntryArrayConfigStub>("monster");

        Assert.Multiple(() =>
        {
            Assert.That(table.Count, Is.EqualTo(1));
            Assert.That(table.Get(1).Entries.Count, Is.EqualTo(2));
            Assert.That(table.Get(1).Entries[0].A, Is.EqualTo("x|1:b=string:yz"));
            Assert.That(table.Get(1).Entries[1].A, Is.EqualTo("x"));
            Assert.That(table.Get(1).Entries[1].B, Is.EqualTo("yz"));
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

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

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

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

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

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Message, Does.Contain("fire"));
            Assert.That(exception!.Message, Does.Contain("ice"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证数组 <c>const</c> 限制会保留元素顺序并按完整序列比较。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_Array_Value_Does_Not_Match_Schema_Const()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            name: Slime
            dropItemIds:
              - gem
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
                "dropItemIds": {
                  "type": "array",
                  "const": ["potion", "gem"],
                  "items": {
                    "type": "string"
                  }
                }
              }
            }
            """);

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterDropArrayConfigStub>("monster", "monster", "schemas/monster.schema.json",
                static config => config.Id);
        var registry = new ConfigRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Message, Does.Contain("dropItemIds"));
            Assert.That(exception.Message, Does.Contain("potion"));
            Assert.That(exception.Message, Does.Contain("gem"));
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

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Message, Does.Contain("reward.currency"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证嵌套对象 <c>const</c> 限制会按完整对象内容比较。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_Nested_Object_Does_Not_Match_Schema_Const()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            name: Slime
            reward:
              gold: 10
              currency: gem
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
                  "properties": {
                    "gold": { "type": "integer" },
                    "currency": { "type": "string" }
                  },
                  "const": {
                    "gold": 10,
                    "currency": "coin"
                  }
                }
              }
            }
            """);

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterNestedConfigStub>("monster", "monster", "schemas/monster.schema.json",
                static config => config.Id);
        var registry = new ConfigRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Message, Does.Contain("reward"));
            Assert.That(exception.Message, Does.Contain("\"gold\""));
            Assert.That(exception.Message, Does.Contain("\"currency\""));
            Assert.That(exception.Message, Does.Contain("\"coin\""));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证空对象 <c>const</c> 约束会被视为合法 schema，并与空 YAML 映射正确匹配。
    /// </summary>
    [Test]
    public async Task LoadAsync_Should_Accept_Empty_Object_Schema_Const()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            name: Slime
            reward: {}
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
                  "properties": {},
                  "const": {}
                }
              }
            }
            """);

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterNestedConfigStub>("monster", "monster", "schemas/monster.schema.json",
                static config => config.Id);
        var registry = new ConfigRegistry();

        await loader.LoadAsync(registry);

        var table = registry.GetTable<int, MonsterNestedConfigStub>("monster");

        Assert.Multiple(() =>
        {
            Assert.That(table.Count, Is.EqualTo(1));
            Assert.That(table.Get(1).Name, Is.EqualTo("Slime"));
        });
    }

    /// <summary>
    ///     验证对象字段不满足 <c>minProperties</c> 时会在运行时被拒绝。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_Object_Violates_MinProperties()
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
                  "minProperties": 2,
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

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.ConstraintViolation));
            Assert.That(exception.Diagnostic.DisplayPath, Is.EqualTo("reward"));
            Assert.That(exception.Diagnostic.RawValue, Is.EqualTo("1"));
            Assert.That(exception.Message, Does.Contain("at least 2 properties"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证对象字段不满足 <c>maxProperties</c> 时会在运行时被拒绝。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_Object_Violates_MaxProperties()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            name: Slime
            reward:
              gold: 10
              currency: coin
              tier: epic
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
                  "maxProperties": 2,
                  "properties": {
                    "gold": { "type": "integer" },
                    "currency": { "type": "string" },
                    "tier": { "type": "string" }
                  }
                }
              }
            }
            """);

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterNestedConfigStub>("monster", "monster", "schemas/monster.schema.json",
                static config => config.Id);
        var registry = new ConfigRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.ConstraintViolation));
            Assert.That(exception.Diagnostic.DisplayPath, Is.EqualTo("reward"));
            Assert.That(exception.Diagnostic.RawValue, Is.EqualTo("3"));
            Assert.That(exception.Message, Does.Contain("at most 2 properties"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证对象字段将 <c>minProperties</c> 声明为非法值时，会在 schema 解析阶段被拒绝。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_Object_Property_Count_Constraint_Is_Not_NonNegative_Integer()
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
                  "minProperties": -1,
                  "properties": {
                    "gold": { "type": "integer" }
                  }
                }
              }
            }
            """);

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterNestedConfigStub>("monster", "monster", "schemas/monster.schema.json",
                static config => config.Id);
        var registry = new ConfigRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.SchemaUnsupported));
            Assert.That(exception.Diagnostic.DisplayPath, Is.EqualTo("reward"));
            Assert.That(exception.Message, Does.Contain("minProperties"));
            Assert.That(exception.Message, Does.Contain("non-negative integer"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证对象字段将 <c>maxProperties</c> 声明为非整数数值时，会在 schema 解析阶段被拒绝。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_Object_MaxProperties_Constraint_Is_Not_Integer()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            name: Slime
            reward:
              gold: 10
              currency: coin
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
                  "maxProperties": 1.5,
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

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.SchemaUnsupported));
            Assert.That(exception.Diagnostic.DisplayPath, Is.EqualTo("reward"));
            Assert.That(exception.Message, Does.Contain("maxProperties"));
            Assert.That(exception.Message, Does.Contain("non-negative integer"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证对象字段将 <c>minProperties</c> 声明为大于 <c>maxProperties</c> 时，会在 schema 解析阶段被拒绝。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_Object_Property_Count_Constraints_Are_Inverted()
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
                  "minProperties": 3,
                  "maxProperties": 2,
                  "properties": {
                    "gold": { "type": "integer" },
                    "currency": { "type": "string" },
                    "tier": { "type": "string" }
                  }
                }
              }
            }
            """);

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterNestedConfigStub>("monster", "monster", "schemas/monster.schema.json",
                static config => config.Id);
        var registry = new ConfigRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.SchemaUnsupported));
            Assert.That(exception.Diagnostic.DisplayPath, Is.EqualTo("reward"));
            Assert.That(exception.Message, Does.Contain("minProperties"));
            Assert.That(exception.Message, Does.Contain("greater than 'maxProperties'"));
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

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

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
        var loader = CreateItemBackedMonsterLoader<MonsterPhaseDropConfigStub>(
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
            """,
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
            """,
            static config => config.Id,
            ("item/potion.yaml", "potion", "Potion"));
        var registry = new ConfigRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

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

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

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
        var loader = CreateItemBackedMonsterLoader<MonsterDropArrayConfigStub>(
            """
            id: 1
            name: Slime
            dropItemIds:
              - potion
              - missing_item
            """,
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
            """,
            static config => config.Id,
            ("item/potion.yaml", "potion", "Potion"),
            ("item/slime-gel.yaml", "slime_gel", "Slime Gel"));
        var registry = new ConfigRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Message, Does.Contain("dropItemIds[1]"));
            Assert.That(exception!.Message, Does.Contain("missing_item"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证仅声明在 <c>contains</c> 子 schema 里的跨表引用也会参与整批加载校验。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_Contains_Matched_Reference_Target_Is_Missing()
    {
        var loader = CreateItemBackedMonsterLoader<MonsterDropArrayConfigStub>(
            """
            id: 1
            name: Slime
            dropItemIds:
              - potion
              - missing_item
            """,
            """
            {
              "type": "object",
              "required": ["id", "name", "dropItemIds"],
              "properties": {
                "id": { "type": "integer" },
                "name": { "type": "string" },
                "dropItemIds": {
                  "type": "array",
                  "minContains": 1,
                  "contains": {
                    "type": "string",
                    "x-gframework-ref-table": "item"
                  },
                  "items": {
                    "type": "string"
                  }
                }
              }
            }
            """,
            static config => config.Id,
            ("item/potion.yaml", "potion", "Potion"));
        var registry = new ConfigRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.ReferencedKeyNotFound));
            Assert.That(exception.Diagnostic.TableName, Is.EqualTo("monster"));
            Assert.That(exception.Diagnostic.ReferencedTableName, Is.EqualTo("item"));
            Assert.That(exception.Diagnostic.DisplayPath, Is.EqualTo("dropItemIds[1]"));
            Assert.That(exception.Diagnostic.RawValue, Is.EqualTo("missing_item"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证底层文件读取在取消时会保留 <see cref="OperationCanceledException" />，
    ///     避免热重载把会话级取消误报为配置读取失败。
    /// </summary>
    [Test]
    public void ReadYamlAsync_Should_Preserve_OperationCanceledException_When_Cancellation_Is_Requested()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            name: Slime
            hp: 10
            """);

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterConfigStub>("monster", "monster", static config => config.Id);
        var registration = GetSingleYamlTableRegistration(loader);
        var readYamlAsyncMethod = registration.GetType()
            .GetMethod("ReadYamlAsync", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(readYamlAsyncMethod, Is.Not.Null);

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // 通过反射直接命中注册项的文件读取路径，稳定回归本次取消语义修复。
        var readTask = (Task<string>)readYamlAsyncMethod!.Invoke(
            registration,
            new object?[]
            {
                Path.Combine(_rootPath, "monster"),
                Path.Combine(_rootPath, "monster", "slime.yaml"),
                null,
                cancellationTokenSource.Token
            })!;

        Assert.That(
            async () => await readTask.ConfigureAwait(false),
            Throws.InstanceOf<OperationCanceledException>());
    }

    /// <summary>
    ///     验证同步反序列化阶段遇到已取消 token 时会直接透传 <see cref="OperationCanceledException" />，
    ///     避免把停止加载误报为 YAML 解析失败。
    /// </summary>
    [Test]
    public void DeserializeValue_Should_Preserve_OperationCanceledException_When_Cancellation_Is_Requested()
    {
        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterConfigStub>("monster", "monster", static config => config.Id);
        var registration = GetSingleYamlTableRegistration(loader);
        var deserializeValueMethod = registration.GetType()
            .GetMethod("DeserializeValue", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(deserializeValueMethod, Is.Not.Null);

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        var deserializer = new DeserializerBuilder().Build();
        var exception = Assert.Throws<TargetInvocationException>(() =>
            deserializeValueMethod!.Invoke(
                registration,
                new object?[]
                {
                    deserializer,
                    Path.Combine(_rootPath, "monster"),
                    Path.Combine(_rootPath, "monster", "slime.yaml"),
                    null,
                    """
                    id: 1
                    name: Slime
                    hp: 10
                    """,
                    cancellationTokenSource.Token
                }));

        // 反射调用同步私有方法时会把原始异常包装为 TargetInvocationException。
        Assert.That(exception!.InnerException, Is.InstanceOf<OperationCanceledException>());
    }

    /// <summary>
    ///     验证构建最终配置表阶段遇到已取消 token 时会继续透传 <see cref="OperationCanceledException" />，
    ///     避免热重载把提交前取消记录成构表失败。
    /// </summary>
    [Test]
    public void BuildLoadResult_Should_Preserve_OperationCanceledException_When_Cancellation_Is_Requested()
    {
        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterConfigStub>("monster", "monster", static config => config.Id);
        var registration = GetSingleYamlTableRegistration(loader);
        var buildLoadResultMethod = registration.GetType()
            .GetMethod("BuildLoadResult", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(buildLoadResultMethod, Is.Not.Null);

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        var exception = Assert.Throws<TargetInvocationException>(() =>
            buildLoadResultMethod!.Invoke(
                registration,
                new object?[]
                {
                    Path.Combine(_rootPath, "monster"),
                    null,
                    new List<MonsterConfigStub>
                    {
                        new()
                        {
                            Id = 1,
                            Name = "Slime",
                            Hp = 10
                        }
                    },
                    new List<YamlConfigReferenceUsage>(),
                    cancellationTokenSource.Token
                }));

        // 反射调用同步私有方法时会把原始异常包装为 TargetInvocationException。
        Assert.That(exception!.InnerException, Is.InstanceOf<OperationCanceledException>());
    }

    /// <summary>
    ///     验证依赖关系仅来自 <c>contains</c> 子 schema 时，热重载仍会追踪该依赖并在目标表破坏引用后回滚。
    /// </summary>
    [Test]
    public async Task EnableHotReload_Should_Keep_Previous_State_When_Contains_Reference_Dependency_Breaks()
    {
        var (loader, registry) = await CreateLoadedContainsReferenceHotReloadScenarioAsync().ConfigureAwait(false);
        var (reloadFailureTaskSource, hotReload) = EnableHotReloadWithFailureCapture(loader, registry);

        try
        {
            CreateConfigFile("item/potion.yaml", UpdatedItemConfigContent);

            var failure = await WaitForTaskWithinAsync(reloadFailureTaskSource.Task, TimeSpan.FromSeconds(5))
                .ConfigureAwait(false);
            var diagnosticException = failure.Exception as ConfigLoadException;

            Assert.Multiple(() =>
            {
                Assert.That(failure.TableName, Is.EqualTo("item"));
                Assert.That(diagnosticException, Is.Not.Null);
                Assert.That(diagnosticException!.Diagnostic.FailureKind,
                    Is.EqualTo(ConfigLoadFailureKind.ReferencedKeyNotFound));
                Assert.That(diagnosticException.Diagnostic.TableName, Is.EqualTo("monster"));
                Assert.That(diagnosticException.Diagnostic.ReferencedTableName, Is.EqualTo("item"));
                Assert.That(diagnosticException.Diagnostic.DisplayPath, Is.EqualTo("dropItemIds[0]"));
                Assert.That(diagnosticException.Diagnostic.RawValue, Is.EqualTo("potion"));
                Assert.That(registry.GetTable<string, ItemConfigStub>("item").ContainsKey("potion"), Is.True);
                Assert.That(registry.GetTable<int, MonsterDropArrayConfigStub>("monster").Get(1).DropItemIds,
                    Is.EqualTo(new[] { "potion" }));
            });
        }
        finally
        {
            hotReload.UnRegister();
        }
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
        var (loader, registry) = await CreateLoadedMonsterHotReloadScenarioAsync(useOptionsObject: true)
            .ConfigureAwait(false);
        var (reloadTaskSource, hotReload) = EnableHotReloadWithReloadCapture(loader, registry, useOptionsObject: true);

        try
        {
            CreateConfigFile("monster/slime.yaml", UpdatedMonsterConfigContent);

            var tableName = await WaitForTaskWithinAsync(reloadTaskSource.Task, TimeSpan.FromSeconds(5))
                .ConfigureAwait(false);
            AssertMonsterHotReloadUpdated(tableName, registry);
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
        var (loader, registry) = await CreateLoadedMonsterHotReloadScenarioAsync().ConfigureAwait(false);
        var (reloadFailureTaskSource, hotReload) = EnableHotReloadWithFailureCapture(loader, registry);

        try
        {
            CreateSchemaFile("schemas/monster.schema.json", MonsterSchemaWithRarityContent);

            var failure = await WaitForTaskWithinAsync(reloadFailureTaskSource.Task, TimeSpan.FromSeconds(5))
                .ConfigureAwait(false);
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
        var (loader, registry) = await CreateLoadedCrossTableReferenceHotReloadScenarioAsync().ConfigureAwait(false);
        var (reloadFailureTaskSource, hotReload) = EnableHotReloadWithFailureCapture(loader, registry);

        try
        {
            CreateConfigFile("item/potion.yaml", UpdatedItemConfigContent);

            var failure = await WaitForTaskWithinAsync(reloadFailureTaskSource.Task, TimeSpan.FromSeconds(5))
                .ConfigureAwait(false);
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

    private const string ItemSchemaContent =
        """
        {
          "type": "object",
          "required": ["id", "name"],
          "properties": {
            "id": { "type": "string" },
            "name": { "type": "string" }
          }
        }
        """;

    private const string InitialMonsterConfigContent =
        """
        id: 1
        name: Slime
        hp: 10
        """;

    private const string UpdatedMonsterConfigContent =
        """
        id: 1
        name: Slime
        hp: 25
        """;

    private const string MonsterSchemaContent =
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
        """;

    private const string MonsterSchemaWithRarityContent =
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
        """;

    private const string UpdatedItemConfigContent =
        """
        id: elixir
        name: Elixir
        """;

    private const string MonsterDropArrayConfigContent =
        """
        id: 1
        name: Slime
        dropItemIds:
          - potion
        """;

    private const string MonsterDropArraySchemaContent =
        """
        {
          "type": "object",
          "required": ["id", "name", "dropItemIds"],
          "properties": {
            "id": { "type": "integer" },
            "name": { "type": "string" },
            "dropItemIds": {
              "type": "array",
              "minContains": 1,
              "contains": {
                "type": "string",
                "x-gframework-ref-table": "item"
              },
              "items": {
                "type": "string"
              }
            }
          }
        }
        """;

    private const string MonsterDropConfigContent =
        """
        id: 1
        name: Slime
        dropItemId: potion
        """;

    private const string MonsterDropSchemaContent =
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
        """;

    /// <summary>
    ///     创建并加载标准 monster 热重载夹具，供重载成功与 schema 失败场景复用。
    /// </summary>
    /// <param name="useOptionsObject">是否通过选项对象注册表。</param>
    /// <returns>已完成首次加载的加载器与注册表。</returns>
    private async Task<(YamlConfigLoader Loader, ConfigRegistry Registry)> CreateLoadedMonsterHotReloadScenarioAsync(
        bool useOptionsObject = false)
    {
        CreateConfigFile("monster/slime.yaml", InitialMonsterConfigContent);
        CreateSchemaFile("schemas/monster.schema.json", MonsterSchemaContent);

        var loader = useOptionsObject
            ? new YamlConfigLoader(_rootPath).RegisterTable(
                new YamlConfigTableRegistrationOptions<int, MonsterConfigStub>(
                    "monster",
                    "monster",
                    static config => config.Id)
                {
                    SchemaRelativePath = "schemas/monster.schema.json"
                })
            : new YamlConfigLoader(_rootPath)
                .RegisterTable<int, MonsterConfigStub>("monster", "monster", "schemas/monster.schema.json",
                    static config => config.Id);
        var registry = new ConfigRegistry();
        await loader.LoadAsync(registry).ConfigureAwait(false);
        return (loader, registry);
    }

    /// <summary>
    ///     创建并加载 contains 子 schema 引用场景，供热重载依赖回滚测试复用。
    /// </summary>
    /// <returns>已完成首次加载的加载器与注册表。</returns>
    private async Task<(YamlConfigLoader Loader, ConfigRegistry Registry)>
        CreateLoadedContainsReferenceHotReloadScenarioAsync()
    {
        var loader = CreateItemBackedMonsterLoader<MonsterDropArrayConfigStub>(
            MonsterDropArrayConfigContent,
            MonsterDropArraySchemaContent,
            static config => config.Id,
            ("item/potion.yaml", "potion", "Potion"));
        var registry = new ConfigRegistry();
        await loader.LoadAsync(registry).ConfigureAwait(false);
        return (loader, registry);
    }

    /// <summary>
    ///     创建并加载跨表单值引用场景，供热重载依赖回滚测试复用。
    /// </summary>
    /// <returns>已完成首次加载的加载器与注册表。</returns>
    private async Task<(YamlConfigLoader Loader, ConfigRegistry Registry)>
        CreateLoadedCrossTableReferenceHotReloadScenarioAsync()
    {
        var loader = CreateItemBackedMonsterLoader<MonsterDropConfigStub>(
            MonsterDropConfigContent,
            MonsterDropSchemaContent,
            static config => config.Id,
            ("item/potion.yaml", "potion", "Potion"));
        var registry = new ConfigRegistry();
        await loader.LoadAsync(registry).ConfigureAwait(false);
        return (loader, registry);
    }

    /// <summary>
    ///     以统一的失败回调配置启用热重载，避免每个测试重复接线相同的通知逻辑。
    /// </summary>
    /// <param name="loader">已完成首次加载的加载器。</param>
    /// <param name="registry">要复用的配置注册表。</param>
    /// <returns>失败通知任务源与取消注册句柄。</returns>
    private static (TaskCompletionSource<(string TableName, Exception Exception)> TaskSource, IUnRegister Registration)
        EnableHotReloadWithFailureCapture(YamlConfigLoader loader, ConfigRegistry registry)
    {
        var reloadFailureTaskSource =
            new TaskCompletionSource<(string TableName, Exception Exception)>(TaskCreationOptions
                .RunContinuationsAsynchronously);
        var hotReload = loader.EnableHotReload(
            registry,
            onTableReloadFailed: (tableName, exception) =>
                reloadFailureTaskSource.TrySetResult((tableName, exception)),
            debounceDelay: TimeSpan.FromMilliseconds(150));
        return (reloadFailureTaskSource, hotReload);
    }

    /// <summary>
    ///     以统一的成功回调配置启用热重载，避免相同的防抖与回调装配在测试中重复出现。
    /// </summary>
    /// <param name="loader">已完成首次加载的加载器。</param>
    /// <param name="registry">要复用的配置注册表。</param>
    /// <param name="useOptionsObject">是否通过选项对象启用热重载。</param>
    /// <returns>成功通知任务源与取消注册句柄。</returns>
    private static (TaskCompletionSource<string> TaskSource, IUnRegister Registration) EnableHotReloadWithReloadCapture(
        YamlConfigLoader loader,
        ConfigRegistry registry,
        bool useOptionsObject = false)
    {
        var reloadTaskSource = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var hotReload = useOptionsObject
            ? loader.EnableHotReload(
                registry,
                new YamlConfigHotReloadOptions
                {
                    OnTableReloaded = tableName => reloadTaskSource.TrySetResult(tableName),
                    DebounceDelay = TimeSpan.FromMilliseconds(150)
                })
            : loader.EnableHotReload(
                registry,
                onTableReloaded: tableName => reloadTaskSource.TrySetResult(tableName),
                debounceDelay: TimeSpan.FromMilliseconds(150));
        return (reloadTaskSource, hotReload);
    }

    /// <summary>
    ///     断言标准 monster 热重载成功后，通知表名与刷新后的生命值都符合预期。
    /// </summary>
    /// <param name="tableName">热重载回调返回的表名。</param>
    /// <param name="registry">承载刷新结果的注册表。</param>
    private static void AssertMonsterHotReloadUpdated(string tableName, ConfigRegistry registry)
    {
        Assert.Multiple(() =>
        {
            Assert.That(tableName, Is.EqualTo("monster"));
            Assert.That(registry.GetTable<int, MonsterConfigStub>("monster").Get(1).Hp, Is.EqualTo(25));
        });
    }

    /// <summary>
    ///     为对象数组 <c>contains</c> 子集匹配场景创建加载器，避免测试方法体被大段固定 schema 稀释。
    /// </summary>
    /// <returns>已注册目标表的加载器。</returns>
    private YamlConfigLoader CreateLoaderForContainsSubsetObjectArrayScenario()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            name: Slime
            entries:
              -
                id: 1
                weight: 2
              -
                id: 2
                weight: 3
            """);
        CreateSchemaFile(
            "schemas/monster.schema.json",
            """
            {
              "type": "object",
              "required": ["id", "name", "entries"],
              "properties": {
                "id": { "type": "integer" },
                "name": { "type": "string" },
                "entries": {
                  "type": "array",
                  "minContains": 1,
                  "contains": {
                    "type": "object",
                    "required": ["id"],
                    "properties": {
                      "id": {
                        "type": "integer",
                        "const": 1
                      }
                    }
                  },
                  "items": {
                    "type": "object",
                    "required": ["id", "weight"],
                    "properties": {
                      "id": { "type": "integer" },
                      "weight": { "type": "integer" }
                    }
                  }
                }
              }
            }
            """);

        return new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterWeightedEntryArrayConfigStub>("monster", "monster", "schemas/monster.schema.json",
                static config => config.Id);
    }

    /// <summary>
    ///     为跨表引用加载测试创建标准 item 表夹具，并按既有顺序注册 <c>item</c> 与 <c>monster</c>。
    /// </summary>
    /// <typeparam name="TMonsterConfig">monster 表的配置类型。</typeparam>
    /// <param name="monsterConfigContent">monster 配置文件内容。</param>
    /// <param name="monsterSchemaContent">monster schema 内容。</param>
    /// <param name="keySelector">monster 表主键选择器。</param>
    /// <param name="items">要写入的 item 配置文件集合。</param>
    /// <returns>已完成 schema 与表注册的加载器。</returns>
    private YamlConfigLoader CreateItemBackedMonsterLoader<TMonsterConfig>(
        string monsterConfigContent,
        string monsterSchemaContent,
        Func<TMonsterConfig, int> keySelector,
        params (string RelativePath, string ItemId, string Name)[] items)
    {
        foreach (var (relativePath, itemId, name) in items)
        {
            CreateConfigFile(
                relativePath,
                $"""
                id: {itemId}
                name: {name}
                """);
        }

        CreateConfigFile("monster/slime.yaml", monsterConfigContent);
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
        CreateSchemaFile("schemas/monster.schema.json", monsterSchemaContent);

        return new YamlConfigLoader(_rootPath)
            .RegisterTable<string, ItemConfigStub>("item", "item", "schemas/item.schema.json",
                static config => config.Id)
            .RegisterTable<int, TMonsterConfig>("monster", "monster", "schemas/monster.schema.json", keySelector);
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

    private static object GetSingleYamlTableRegistration(YamlConfigLoader loader)
    {
        var registrationsField = typeof(YamlConfigLoader).GetField(
            "_registrations",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(registrationsField, Is.Not.Null);

        var registrations = registrationsField!.GetValue(loader) as System.Collections.IList;

        Assert.That(registrations, Is.Not.Null);
        Assert.That(registrations!.Count, Is.EqualTo(1));

        return registrations[0]!;
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
        var completedTask = await Task.WhenAny(task, Task.Delay(timeout)).ConfigureAwait(false);
        if (!ReferenceEquals(completedTask, task))
        {
            Assert.Fail($"Timed out after {timeout} while waiting for file watcher notification.");
        }

        return await task.ConfigureAwait(false);
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
    ///     用于浮点数 schema 校验测试的最小怪物配置类型。
    /// </summary>
    private sealed class MonsterNumberConfigStub
    {
        /// <summary>
        ///     获取或设置主键。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        ///     获取或设置浮点掉落率。
        /// </summary>
        public double DropRate { get; set; }
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
        public List<int> DropRates { get; set; } = new();
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
    ///     用于 <c>uniqueItems</c> 比较键碰撞回归测试的最小配置类型。
    /// </summary>
    private sealed class MonsterComparableEntryArrayConfigStub
    {
        /// <summary>
        ///     获取或设置主键。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        ///     获取或设置待比较对象数组。
        /// </summary>
        public List<ComparableEntryConfigStub> Entries { get; set; } = new();
    }

    /// <summary>
    ///     用于对象数组 <c>contains</c> 子集匹配回归测试的最小配置类型。
    /// </summary>
    private sealed class MonsterWeightedEntryArrayConfigStub
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
        ///     获取或设置对象数组条目。
        /// </summary>
        public List<WeightedEntryConfigStub> Entries { get; set; } = new();
    }

    /// <summary>
    ///     表示对象数组 <c>contains</c> 子集匹配回归测试中的条目元素。
    /// </summary>
    private sealed class WeightedEntryConfigStub
    {
        /// <summary>
        ///     获取或设置条目标识。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        ///     获取或设置权重。
        /// </summary>
        public int Weight { get; set; }
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
    ///     表示用于比较键碰撞回归测试的对象数组元素。
    /// </summary>
    private sealed class ComparableEntryConfigStub
    {
        /// <summary>
        ///     获取或设置字段 A。
        /// </summary>
        public string A { get; set; } = string.Empty;

        /// <summary>
        ///     获取或设置字段 B。
        /// </summary>
        public string B { get; set; } = string.Empty;
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
