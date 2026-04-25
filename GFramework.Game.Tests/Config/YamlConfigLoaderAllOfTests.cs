using System.IO;
using GFramework.Game.Abstractions.Config;
using GFramework.Game.Config;

namespace GFramework.Game.Tests.Config;

/// <summary>
///     验证 YAML 配置加载器对对象级 <c>allOf</c> 组合约束的运行时行为。
/// </summary>
[TestFixture]
public sealed class YamlConfigLoaderAllOfTests
{
    private const string DefaultRewardPropertiesJson = """
                                                    {
                                                      "itemId": { "type": "string" },
                                                      "itemCount": { "type": "integer" },
                                                      "bonus": { "type": "integer" }
                                                    }
                                                    """;

    private const string DefaultAllOfJson = """
                                            [
                                              {
                                                "type": "object",
                                                "required": ["itemCount"],
                                                "properties": {
                                                  "itemCount": { "type": "integer" }
                                                }
                                              },
                                              {
                                                "type": "object",
                                                "properties": {
                                                  "itemCount": {
                                                    "type": "integer",
                                                    "minimum": 2
                                                  }
                                                }
                                              }
                                            ]
                                            """;

    private string? _rootPath;

    /// <summary>
    ///     为每个用例创建隔离的临时目录，避免不同 allOf 场景互相污染。
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        _rootPath = Path.Combine(Path.GetTempPath(), "GFramework.ConfigTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_rootPath);
    }

    /// <summary>
    ///     清理当前测试创建的目录，避免本地临时文件堆积。
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        if (!string.IsNullOrEmpty(_rootPath) &&
            Directory.Exists(_rootPath))
        {
            try
            {
                Directory.Delete(_rootPath, true);
            }
            catch (Exception)
            {
                // Ignore cleanup failures in test teardown
            }
        }
    }

    /// <summary>
    ///     验证当前对象未满足任一 allOf 条目时，运行时会拒绝加载。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_AllOf_Entry_Is_Not_Satisfied()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            BuildMonsterConfigYaml(
                """
                bonus: 1
                """));
        CreateSchemaFile(
            "schemas/monster.schema.json",
            BuildMonsterSchema(DefaultRewardPropertiesJson, DefaultAllOfJson));

        var loader = CreateMonsterRewardLoader();
        var registry = CreateRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.ConstraintViolation));
            Assert.That(exception.Diagnostic.DisplayPath, Is.EqualTo("reward"));
            Assert.That(exception.Message, Does.Contain("allOf"));
            Assert.That(exception.Message, Does.Contain("entry #1"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证对象满足全部 allOf 条目时，可以保留未在 focused block 中重复声明的同级字段。
    /// </summary>
    [Test]
    public async Task LoadAsync_Should_Accept_When_All_AllOf_Entries_Are_Satisfied()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            BuildMonsterConfigYaml(
                """
                itemId: potion
                itemCount: 3
                bonus: 1
                """));
        CreateSchemaFile(
            "schemas/monster.schema.json",
            BuildMonsterSchema(DefaultRewardPropertiesJson, DefaultAllOfJson));

        var loader = CreateMonsterRewardLoader();
        var registry = CreateRegistry();

        await loader.LoadAsync(registry).ConfigureAwait(false);

        var table = registry.GetTable<int, MonsterAllOfConfigStub>("monster");
        var reward = table.Get(1).Reward;
        Assert.Multiple(() =>
        {
            Assert.That(table.Count, Is.EqualTo(1));
            Assert.That(reward.ItemId, Is.EqualTo("potion"));
            Assert.That(reward.ItemCount, Is.EqualTo(3));
            Assert.That(reward.Bonus, Is.EqualTo(1));
        });
    }

    /// <summary>
    ///     验证非数组 allOf 声明会在 schema 解析阶段被拒绝。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_AllOf_Is_Not_An_Array()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            BuildMonsterConfigYaml(
                """
                itemCount: 3
                """));
        CreateSchemaFile(
            "schemas/monster.schema.json",
            BuildMonsterSchema(
                DefaultRewardPropertiesJson,
                """
                {
                  "type": "object",
                  "properties": {
                    "itemCount": { "type": "integer" }
                  }
                }
                """));

        var loader = CreateMonsterRewardLoader();
        var registry = CreateRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.SchemaUnsupported));
            Assert.That(exception.Diagnostic.DisplayPath, Is.EqualTo("reward"));
            Assert.That(exception.Message, Does.Contain("must declare 'allOf' as an array"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证只有对象字段允许声明 allOf。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_NonObject_Schema_Declares_AllOf()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            tag: elite
            """);
        CreateSchemaFile(
            "schemas/monster.schema.json",
            """
            {
              "type": "object",
              "required": ["id", "tag"],
              "properties": {
                "id": { "type": "integer" },
                "tag": {
                  "type": "string",
                  "allOf": [
                    {
                      "type": "object",
                      "properties": {}
                    }
                  ]
                }
              }
            }
            """);

        if (_rootPath is null)
        {
            throw new InvalidOperationException("Root path is not initialized.");
        }

        var loader = new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterTagConfigStub>(
                "monster",
                "monster",
                "schemas/monster.schema.json",
                static config => config.Id);
        var registry = CreateRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.SchemaUnsupported));
            Assert.That(exception.Diagnostic.DisplayPath, Is.EqualTo("tag"));
            Assert.That(exception.Message, Does.Contain("can only declare 'allOf' on object schemas"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证 allOf 条目必须是对象值 schema。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_AllOf_Entry_Is_Not_Object_Valued()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            BuildMonsterConfigYaml(
                """
                itemCount: 3
                """));
        CreateSchemaFile(
            "schemas/monster.schema.json",
            BuildMonsterSchema(
                DefaultRewardPropertiesJson,
                """
                [123]
                """));

        var loader = CreateMonsterRewardLoader();
        var registry = CreateRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.SchemaUnsupported));
            Assert.That(exception.Diagnostic.DisplayPath, Is.EqualTo("reward"));
            Assert.That(exception.Message, Does.Contain("allOf' entries as object-valued schemas"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证 allOf 条目只接受 object-typed schema。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_AllOf_Entry_Is_Not_Object_Typed()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            BuildMonsterConfigYaml(
                """
                itemCount: 3
                """));
        CreateSchemaFile(
            "schemas/monster.schema.json",
            BuildMonsterSchema(
                DefaultRewardPropertiesJson,
                """
                [
                  {
                    "type": "string",
                    "const": "potion"
                  }
                ]
                """));

        var loader = CreateMonsterRewardLoader();
        var registry = CreateRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.SchemaUnsupported));
            Assert.That(exception.Diagnostic.DisplayPath, Is.EqualTo("reward[allOf[0]]"));
            Assert.That(exception.Message, Does.Contain("object-typed schema"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证 allOf 条目的 <c>properties</c> 必须声明为对象映射。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_AllOf_Entry_Properties_Is_Not_Object_Valued()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            BuildMonsterConfigYaml(
                """
                itemCount: 3
                """));
        CreateSchemaFile(
            "schemas/monster.schema.json",
            BuildMonsterSchema(
                DefaultRewardPropertiesJson,
                """
                [
                  {
                    "type": "object",
                    "properties": 1
                  }
                ]
                """));

        var loader = CreateMonsterRewardLoader();
        var registry = CreateRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.SchemaUnsupported));
            Assert.That(exception.Diagnostic.DisplayPath, Is.EqualTo("reward[allOf[0]]"));
            Assert.That(exception.Message, Does.Contain("must declare 'properties' as an object-valued map"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证 allOf 条目的 <c>required</c> 必须声明为字段名数组。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_AllOf_Entry_Required_Is_Not_An_Array()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            BuildMonsterConfigYaml(
                """
                itemCount: 3
                """));
        CreateSchemaFile(
            "schemas/monster.schema.json",
            BuildMonsterSchema(
                DefaultRewardPropertiesJson,
                """
                [
                  {
                    "type": "object",
                    "required": {}
                  }
                ]
                """));

        var loader = CreateMonsterRewardLoader();
        var registry = CreateRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.SchemaUnsupported));
            Assert.That(exception.Diagnostic.DisplayPath, Is.EqualTo("reward[allOf[0]]"));
            Assert.That(exception.Message, Does.Contain("must declare 'required' as an array of property names"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证 allOf 条目的 <c>required</c> 项必须是字符串字段名。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_AllOf_Entry_Required_Item_Is_Not_A_String()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            BuildMonsterConfigYaml(
                """
                itemCount: 3
                """));
        CreateSchemaFile(
            "schemas/monster.schema.json",
            BuildMonsterSchema(
                DefaultRewardPropertiesJson,
                """
                [
                  {
                    "type": "object",
                    "required": [1]
                  }
                ]
                """));

        var loader = CreateMonsterRewardLoader();
        var registry = CreateRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.SchemaUnsupported));
            Assert.That(exception.Diagnostic.DisplayPath, Is.EqualTo("reward[allOf[0]]"));
            Assert.That(exception.Message, Does.Contain("must declare 'required' entries as property-name strings"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证 allOf 条目的 <c>required</c> 不允许空白字段名。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_AllOf_Entry_Required_Item_Is_Blank()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            BuildMonsterConfigYaml(
                """
                itemCount: 3
                """));
        CreateSchemaFile(
            "schemas/monster.schema.json",
            BuildMonsterSchema(
                DefaultRewardPropertiesJson,
                """
                [
                  {
                    "type": "object",
                    "required": [""]
                  }
                ]
                """));

        var loader = CreateMonsterRewardLoader();
        var registry = CreateRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.SchemaUnsupported));
            Assert.That(exception.Diagnostic.DisplayPath, Is.EqualTo("reward[allOf[0]]"));
            Assert.That(exception.Message, Does.Contain("cannot declare blank property names in 'required'"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证 allOf 条目不能要求父对象未声明的字段。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_AllOf_Entry_Requires_Undeclared_Parent_Property()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            BuildMonsterConfigYaml(
                """
                itemCount: 3
                """));
        CreateSchemaFile(
            "schemas/monster.schema.json",
            BuildMonsterSchema(
                """
                {
                  "itemCount": { "type": "integer" }
                }
                """,
                """
                [
                  {
                    "type": "object",
                    "required": ["bonus"]
                  }
                ]
                """));

        var loader = CreateMonsterRewardLoader();
        var registry = CreateRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.SchemaUnsupported));
            Assert.That(exception.Diagnostic.DisplayPath, Is.EqualTo("reward[allOf[0]]"));
            Assert.That(exception.Message, Does.Contain("requires property 'bonus'"));
            Assert.That(exception.Message, Does.Contain("parent object schema"));
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
        if (_rootPath is null)
        {
            throw new InvalidOperationException("Root path is not initialized.");
        }

        var filePath = Path.Combine(_rootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var directoryPath = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        File.WriteAllText(filePath, content);
    }

    /// <summary>
    ///     写入测试 schema 文件，复用统一的测试文件创建逻辑。
    /// </summary>
    /// <param name="relativePath">schema 相对路径。</param>
    /// <param name="content">schema JSON 内容。</param>
    private void CreateSchemaFile(string relativePath, string content)
    {
        CreateConfigFile(relativePath, content);
    }

    /// <summary>
    ///     构建带有指定奖励内容的怪物配置 YAML 文本。
    /// </summary>
    /// <param name="rewardYaml">奖励对象的 YAML 片段。</param>
    /// <returns>完整的怪物配置 YAML 文本。</returns>
    private static string BuildMonsterConfigYaml(string rewardYaml)
    {
        return $$"""
                id: 1
                reward:
                {{IndentLines(rewardYaml, 2)}}
                """;
    }

    /// <summary>
    ///     构建带有指定奖励属性和 allOf 约束的怪物 schema JSON。
    /// </summary>
    /// <param name="rewardPropertiesJson">奖励对象的 properties JSON 片段。</param>
    /// <param name="allOfJson">allOf 约束的 JSON 数组片段。</param>
    /// <returns>完整的 schema JSON 文本。</returns>
    private static string BuildMonsterSchema(
        string rewardPropertiesJson,
        string allOfJson)
    {
        return $$"""
                {
                  "type": "object",
                  "required": ["id", "reward"],
                  "properties": {
                    "id": { "type": "integer" },
                    "reward": {
                      "type": "object",
                      "properties": {{rewardPropertiesJson}},
                      "allOf": {{allOfJson}}
                    }
                  }
                }
                """;
    }

    /// <summary>
    ///     为多行文本的每一行添加指定数量的空格缩进。
    /// </summary>
    /// <param name="text">原始文本。</param>
    /// <param name="indentLevel">缩进空格数。</param>
    /// <returns>添加缩进后的文本。</returns>
    private static string IndentLines(string text, int indentLevel)
    {
        var indentation = new string(' ', indentLevel);
        var lines = text
            .Trim()
            .Split('\n', StringSplitOptions.None)
            .Select(static line => line.TrimEnd('\r'));

        return string.Join(
            Environment.NewLine,
            lines.Select(line => $"{indentation}{line}"));
    }

    /// <summary>
    ///     创建用于对象 allOf 场景的加载器。
    /// </summary>
    /// <returns>已注册测试表与 schema 路径的加载器。</returns>
    private YamlConfigLoader CreateMonsterRewardLoader()
    {
        if (_rootPath is null)
        {
            throw new InvalidOperationException("Root path is not initialized.");
        }

        return new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterAllOfConfigStub>(
                "monster",
                "monster",
                "schemas/monster.schema.json",
                static config => config.Id);
    }

    /// <summary>
    ///     创建新的配置注册表，确保每个用例从干净状态开始。
    /// </summary>
    /// <returns>空的配置注册表。</returns>
    private static ConfigRegistry CreateRegistry()
    {
        return new ConfigRegistry();
    }

    /// <summary>
    ///     用于对象 allOf 回归测试的最小配置类型。
    /// </summary>
    private sealed class MonsterAllOfConfigStub
    {
        /// <summary>
        ///     获取或设置主键。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        ///     获取或设置奖励对象。
        /// </summary>
        public AllOfRewardConfigStub Reward { get; set; } = new();
    }

    /// <summary>
    ///     表示对象 allOf 回归测试中的奖励节点。
    /// </summary>
    private sealed class AllOfRewardConfigStub
    {
        /// <summary>
        ///     获取或设置掉落物 ID。
        /// </summary>
        public string ItemId { get; set; } = string.Empty;

        /// <summary>
        ///     获取或设置掉落物数量。
        /// </summary>
        public int ItemCount { get; set; }

        /// <summary>
        ///     获取或设置额外奖励值。
        /// </summary>
        public int Bonus { get; set; }
    }

    /// <summary>
    ///     用于非对象 allOf 场景回归测试的最小配置类型。
    /// </summary>
    private sealed class MonsterTagConfigStub
    {
        /// <summary>
        ///     获取或设置主键。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        ///     获取或设置标签。
        /// </summary>
        public string Tag { get; set; } = string.Empty;
    }
}
