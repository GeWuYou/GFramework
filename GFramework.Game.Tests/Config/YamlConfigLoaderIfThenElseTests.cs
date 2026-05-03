// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using GFramework.Game.Abstractions.Config;
using GFramework.Game.Config;

namespace GFramework.Game.Tests.Config;

/// <summary>
///     验证 YAML 配置加载器对 object-focused <c>if</c> / <c>then</c> / <c>else</c> 约束的运行时行为。
/// </summary>
[TestFixture]
public sealed class YamlConfigLoaderIfThenElseTests
{
    private const string DefaultRewardPropertiesJson = """
                                                    {
                                                      "itemId": { "type": "string" },
                                                      "itemCount": { "type": "integer" },
                                                      "bonus": { "type": "integer" }
                                                    }
                                                    """;

    private const string DefaultConditionalJson = """
                                                  "if": {
                                                    "type": "object",
                                                    "properties": {
                                                      "itemId": {
                                                        "type": "string",
                                                        "const": "potion"
                                                      }
                                                    }
                                                  },
                                                  "then": {
                                                    "type": "object",
                                                    "required": ["itemCount"],
                                                    "properties": {
                                                      "itemCount": {
                                                        "type": "integer",
                                                        "minimum": 2
                                                      }
                                                    }
                                                  },
                                                  "else": {
                                                    "type": "object",
                                                    "required": ["bonus"],
                                                    "properties": {
                                                      "bonus": {
                                                        "type": "integer",
                                                        "minimum": 1
                                                      }
                                                    }
                                                  }
                                                  """;

    private string? _rootPath;

    /// <summary>
    ///     为每个用例创建隔离的临时目录，避免不同条件分支场景互相污染。
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
            catch (IOException)
            {
                // Ignore cleanup failures in test teardown
            }
            catch (UnauthorizedAccessException)
            {
                // Ignore cleanup failures in test teardown
            }
        }
    }

    /// <summary>
    ///     验证 <c>if</c> 命中而 <c>then</c> 约束未满足时，运行时会拒绝加载。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_If_Matches_But_Then_Is_Not_Satisfied()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            BuildMonsterConfigYaml(
                """
                itemId: potion
                """));
        CreateSchemaFile(
            "schemas/monster.schema.json",
            BuildMonsterSchema(DefaultRewardPropertiesJson, DefaultConditionalJson));

        var loader = CreateMonsterRewardLoader();
        var registry = CreateRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.ConstraintViolation));
            Assert.That(exception.Diagnostic.DisplayPath, Is.EqualTo("reward"));
            Assert.That(exception.Message, Does.Contain("'then'"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证 <c>if</c> 命中且 <c>then</c> 约束满足时可以正常加载。
    /// </summary>
    [Test]
    public async Task LoadAsync_Should_Accept_When_If_Matches_And_Then_Is_Satisfied()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            BuildMonsterConfigYaml(
                """
                itemId: potion
                itemCount: 3
                """));
        CreateSchemaFile(
            "schemas/monster.schema.json",
            BuildMonsterSchema(DefaultRewardPropertiesJson, DefaultConditionalJson));

        var loader = CreateMonsterRewardLoader();
        var registry = CreateRegistry();

        await loader.LoadAsync(registry).ConfigureAwait(false);

        var table = registry.GetTable<int, MonsterConditionalConfigStub>("monster");
        var reward = table.Get(1).Reward;
        Assert.Multiple(() =>
        {
            Assert.That(table.Count, Is.EqualTo(1));
            Assert.That(reward.ItemId, Is.EqualTo("potion"));
            Assert.That(reward.ItemCount, Is.EqualTo(3));
            Assert.That(reward.Bonus, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证 <c>if</c> 未命中而 <c>else</c> 约束未满足时，运行时会拒绝加载。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_If_Does_Not_Match_But_Else_Is_Not_Satisfied()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            BuildMonsterConfigYaml(
                """
                itemId: sword
                itemCount: 1
                """));
        CreateSchemaFile(
            "schemas/monster.schema.json",
            BuildMonsterSchema(DefaultRewardPropertiesJson, DefaultConditionalJson));

        var loader = CreateMonsterRewardLoader();
        var registry = CreateRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.ConstraintViolation));
            Assert.That(exception.Diagnostic.DisplayPath, Is.EqualTo("reward"));
            Assert.That(exception.Message, Does.Contain("'else'"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证 <c>if</c> 未命中且 <c>else</c> 约束满足时可以正常加载。
    /// </summary>
    [Test]
    public async Task LoadAsync_Should_Accept_When_If_Does_Not_Match_And_Else_Is_Satisfied()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            BuildMonsterConfigYaml(
                """
                itemId: sword
                bonus: 2
                """));
        CreateSchemaFile(
            "schemas/monster.schema.json",
            BuildMonsterSchema(DefaultRewardPropertiesJson, DefaultConditionalJson));

        var loader = CreateMonsterRewardLoader();
        var registry = CreateRegistry();

        await loader.LoadAsync(registry).ConfigureAwait(false);

        var table = registry.GetTable<int, MonsterConditionalConfigStub>("monster");
        var reward = table.Get(1).Reward;
        Assert.Multiple(() =>
        {
            Assert.That(table.Count, Is.EqualTo(1));
            Assert.That(reward.ItemId, Is.EqualTo("sword"));
            Assert.That(reward.ItemCount, Is.EqualTo(0));
            Assert.That(reward.Bonus, Is.EqualTo(2));
        });
    }

    /// <summary>
    ///     验证非对象字段声明 <c>if</c> 时，会在 schema 解析阶段被拒绝。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_NonObject_Schema_Declares_If()
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
                  "if": {
                    "type": "object",
                    "properties": {}
                  },
                  "then": {
                    "type": "object",
                    "properties": {}
                  }
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
            Assert.That(exception.Message, Does.Contain("can only declare 'if' on object schemas"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证缺少 <c>if</c> 却声明 <c>then</c> 时，会在 schema 解析阶段被拒绝。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_Then_Is_Declared_Without_If()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            BuildMonsterConfigYaml(
                """
                itemCount: 2
                """));
        CreateSchemaFile(
            "schemas/monster.schema.json",
            BuildMonsterSchema(
                DefaultRewardPropertiesJson,
                """
                "then": {
                  "type": "object",
                  "required": ["itemCount"],
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
            Assert.That(exception.Message, Does.Contain("must declare 'if' when using 'then' or 'else'"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证缺少 <c>if</c> 却声明 <c>else</c> 时，会在 schema 解析阶段被拒绝。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_Else_Is_Declared_Without_If()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            BuildMonsterConfigYaml(
                """
                bonus: 1
                """));
        CreateSchemaFile(
            "schemas/monster.schema.json",
            BuildMonsterSchema(
                DefaultRewardPropertiesJson,
                """
                "else": {
                  "type": "object",
                  "required": ["bonus"],
                  "properties": {
                    "bonus": { "type": "integer" }
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
            Assert.That(exception.Message, Does.Contain("must declare 'if' when using 'then' or 'else'"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证条件分支不能要求父对象未声明的字段。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_Conditional_Schema_Requires_Undeclared_Parent_Property()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            BuildMonsterConfigYaml(
                """
                itemId: potion
                itemCount: 2
                """));
        CreateSchemaFile(
            "schemas/monster.schema.json",
            BuildMonsterSchema(
                DefaultRewardPropertiesJson,
                """
                "if": {
                  "type": "object",
                  "required": ["bonusCount"],
                  "properties": {
                    "itemId": { "type": "string" }
                  }
                },
                "then": {
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
            Assert.That(exception.Diagnostic.DisplayPath, Is.EqualTo("reward[if]"));
            Assert.That(exception.Message, Does.Contain("requires property 'bonusCount'"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     写入测试配置文件，复用统一的测试文件创建逻辑。
    /// </summary>
    /// <param name="relativePath">配置文件相对路径。</param>
    /// <param name="content">配置文件内容。</param>
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
    ///     构建带有指定奖励属性和条件约束的怪物 schema JSON。
    /// </summary>
    /// <param name="rewardPropertiesJson">奖励对象的 properties JSON 片段。</param>
    /// <param name="conditionalJson">条件约束的 JSON 条目片段。</param>
    /// <returns>完整的 schema JSON 文本。</returns>
    private static string BuildMonsterSchema(
        string rewardPropertiesJson,
        string conditionalJson)
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
                      {{conditionalJson}}
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
    ///     创建用于 object-focused 条件分支场景的加载器。
    /// </summary>
    /// <returns>已注册测试表与 schema 路径的加载器。</returns>
    private YamlConfigLoader CreateMonsterRewardLoader()
    {
        if (_rootPath is null)
        {
            throw new InvalidOperationException("Root path is not initialized.");
        }

        return new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterConditionalConfigStub>(
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
    ///     用于 object-focused 条件分支回归测试的最小配置类型。
    /// </summary>
    private sealed class MonsterConditionalConfigStub
    {
        /// <summary>
        ///     获取或设置主键。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        ///     获取或设置奖励对象。
        /// </summary>
        public ConditionalRewardConfigStub Reward { get; set; } = new();
    }

    /// <summary>
    ///     表示条件分支回归测试中的奖励节点。
    /// </summary>
    private sealed class ConditionalRewardConfigStub
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
    ///     用于非对象条件关键字场景回归测试的最小配置类型。
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
