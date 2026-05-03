// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using GFramework.Game.Abstractions.Config;
using GFramework.Game.Config;

namespace GFramework.Game.Tests.Config;

/// <summary>
///     验证 YAML 配置加载器对对象级 <c>dependentSchemas</c> 约束的运行时行为。
/// </summary>
[TestFixture]
public sealed class YamlConfigLoaderDependentSchemasTests
{
    private const string DefaultRewardPropertiesJson = """
                                                    {
                                                      "itemId": { "type": "string" },
                                                      "itemCount": { "type": "integer" },
                                                      "bonus": { "type": "integer" }
                                                    }
                                                    """;

    private const string DefaultDependentSchemasJson = """
                                                      {
                                                        "itemId": {
                                                          "type": "object",
                                                          "required": ["itemCount"],
                                                          "properties": {
                                                            "itemCount": { "type": "integer" }
                                                          }
                                                        }
                                                      }
                                                      """;

    private string? _rootPath;

    /// <summary>
    ///     为每个用例创建隔离的临时目录，避免不同 dependentSchemas 场景互相污染。
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
            Directory.Delete(_rootPath, true);
        }
    }

    /// <summary>
    ///     验证触发字段出现但条件 schema 未满足时，运行时会拒绝当前对象。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_DependentSchema_Is_Not_Satisfied()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            BuildMonsterConfigYaml(
                """
                itemId: potion
                """));
        CreateSchemaFile(
            "schemas/monster.schema.json",
            BuildMonsterSchema(DefaultRewardPropertiesJson, DefaultDependentSchemasJson));

        var loader = CreateMonsterRewardLoader();
        var registry = CreateRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.ConstraintViolation));
            Assert.That(exception.Diagnostic.DisplayPath, Is.EqualTo("reward"));
            Assert.That(exception.Message, Does.Contain("dependentSchemas"));
            Assert.That(exception.Message, Does.Contain("reward.itemId"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证触发字段缺席时，不会误触发 dependentSchemas 检查。
    /// </summary>
    [Test]
    public async Task LoadAsync_Should_Accept_When_DependentSchemas_Trigger_Is_Absent()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            BuildMonsterConfigYaml(
                """
                bonus: 2
                """));
        CreateSchemaFile(
            "schemas/monster.schema.json",
            BuildMonsterSchema(DefaultRewardPropertiesJson, DefaultDependentSchemasJson));

        var loader = CreateMonsterRewardLoader();
        var registry = CreateRegistry();

        await loader.LoadAsync(registry).ConfigureAwait(false);

        var table = registry.GetTable<int, MonsterDependentSchemasConfigStub>("monster");
        Assert.That(table.Count, Is.EqualTo(1));
    }

    /// <summary>
    ///     验证触发字段出现且条件 schema 满足时，可以保留对象上的额外同级字段并正常通过加载。
    /// </summary>
    [Test]
    public async Task LoadAsync_Should_Accept_When_DependentSchema_Is_Satisfied()
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
            BuildMonsterSchema(DefaultRewardPropertiesJson, DefaultDependentSchemasJson));

        var loader = CreateMonsterRewardLoader();
        var registry = CreateRegistry();

        await loader.LoadAsync(registry).ConfigureAwait(false);

        var table = registry.GetTable<int, MonsterDependentSchemasConfigStub>("monster");
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
    ///     验证非对象 dependentSchemas 声明会在 schema 解析阶段被拒绝。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_DependentSchemas_Is_Not_An_Object()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            BuildMonsterConfigYaml(
                """
                itemId: potion
                """));
        CreateSchemaFile(
            "schemas/monster.schema.json",
            BuildMonsterSchema(
                """
                {
                  "itemId": { "type": "string" },
                  "itemCount": { "type": "integer" }
                }
                """,
                """
                ["itemId"]
                """));

        var loader = CreateMonsterRewardLoader();
        var registry = CreateRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.SchemaUnsupported));
            Assert.That(exception.Diagnostic.DisplayPath, Is.EqualTo("reward"));
            Assert.That(exception.Message, Does.Contain("must declare 'dependentSchemas' as an object"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证 dependentSchemas 的触发字段必须在同级 properties 中显式声明。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_DependentSchemas_Trigger_Is_Not_Declared()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            BuildMonsterConfigYaml(
                """
                itemId: potion
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
                {
                  "itemId": {
                    "type": "object",
                    "properties": {
                      "itemCount": { "type": "integer" }
                    }
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
            Assert.That(exception.Message, Does.Contain("dependentSchemas' for undeclared property 'itemId'"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证 dependentSchemas 只接受 object-typed 条件子 schema。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_DependentSchemas_Schema_Is_Not_Object_Typed()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            BuildMonsterConfigYaml(
                """
                itemId: potion
                """));
        CreateSchemaFile(
            "schemas/monster.schema.json",
            BuildMonsterSchema(
                """
                {
                  "itemId": { "type": "string" }
                }
                """,
                """
                {
                  "itemId": {
                    "type": "string",
                    "const": "potion"
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
            Assert.That(exception.Diagnostic.DisplayPath, Is.EqualTo("reward[dependentSchemas:itemId]"));
            Assert.That(exception.Message, Does.Contain("object-typed 'dependentSchemas' schema"));
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

    private static string BuildMonsterConfigYaml(string rewardYaml)
    {
        return $$"""
                id: 1
                reward:
                {{IndentLines(rewardYaml, 2)}}
                """;
    }

    private static string BuildMonsterSchema(
        string rewardPropertiesJson,
        string dependentSchemasJson)
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
                      "dependentSchemas": {{dependentSchemasJson}}
                    }
                  }
                }
                """;
    }

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
    ///     创建用于对象 dependentSchemas 场景的加载器。
    /// </summary>
    /// <returns>已注册测试表与 schema 路径的加载器。</returns>
    private YamlConfigLoader CreateMonsterRewardLoader()
    {
        if (_rootPath is null)
        {
            throw new InvalidOperationException("Root path is not initialized.");
        }

        return new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterDependentSchemasConfigStub>(
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
    ///     用于对象 dependentSchemas 回归测试的最小配置类型。
    /// </summary>
    private sealed class MonsterDependentSchemasConfigStub
    {
        /// <summary>
        ///     获取或设置主键。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        ///     获取或设置奖励对象。
        /// </summary>
        public DependentSchemasRewardConfigStub Reward { get; set; } = new();
    }

    /// <summary>
    ///     表示对象 dependentSchemas 回归测试中的奖励节点。
    /// </summary>
    private sealed class DependentSchemasRewardConfigStub
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
}
