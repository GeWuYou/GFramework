// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using GFramework.Game.Abstractions.Config;
using GFramework.Game.Config;

namespace GFramework.Game.Tests.Config;

/// <summary>
///     验证 YAML 配置加载器对对象级 <c>dependentRequired</c> 约束的运行时行为。
/// </summary>
[TestFixture]
public sealed class YamlConfigLoaderDependentRequiredTests
{
    private string _rootPath = null!;

    /// <summary>
    ///     为每个用例创建隔离的临时目录，避免不同 dependentRequired 场景互相污染。
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
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, true);
        }
    }

    /// <summary>
    ///     验证触发字段出现但依赖字段缺失时，运行时会拒绝当前对象。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_DependentRequired_Property_Is_Missing()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            reward:
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
                  "properties": {
                    "itemId": { "type": "string" },
                    "itemCount": { "type": "integer" },
                    "bonusId": { "type": "string" },
                    "bonusCount": { "type": "integer" }
                  },
                  "dependentRequired": {
                    "itemId": ["itemCount"],
                    "bonusId": ["bonusCount"]
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
            Assert.That(exception!.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.MissingRequiredProperty));
            Assert.That(exception.Diagnostic.DisplayPath, Is.EqualTo("reward.itemCount"));
            Assert.That(exception.Message, Does.Contain("required when sibling property 'reward.itemId' is present"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证触发字段未出现时，不会误报 dependentRequired 缺失。
    /// </summary>
    [Test]
    public async Task LoadAsync_Should_Accept_When_Trigger_Property_Is_Absent()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            reward: {}
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
                  "properties": {
                    "itemId": { "type": "string" },
                    "itemCount": { "type": "integer" }
                  },
                  "dependentRequired": {
                    "itemId": ["itemCount"]
                  }
                }
              }
            }
            """);

        var loader = CreateMonsterRewardLoader();
        var registry = CreateRegistry();

        await loader.LoadAsync(registry).ConfigureAwait(false);

        var table = registry.GetTable<int, MonsterRewardConfigStub>("monster");
        Assert.That(table.Count, Is.EqualTo(1));
    }

    /// <summary>
    ///     验证依赖字段同时存在时，当前对象可以正常通过加载。
    /// </summary>
    [Test]
    public async Task LoadAsync_Should_Accept_When_DependentRequired_Properties_Are_Present()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            reward:
              itemId: potion
              itemCount: 3
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
                  "properties": {
                    "itemId": { "type": "string" },
                    "itemCount": { "type": "integer" }
                  },
                  "dependentRequired": {
                    "itemId": ["itemCount"]
                  }
                }
              }
            }
            """);

        var loader = CreateMonsterRewardLoader();
        var registry = CreateRegistry();

        await loader.LoadAsync(registry).ConfigureAwait(false);

        var table = registry.GetTable<int, MonsterRewardConfigStub>("monster");
        var reward = table.Get(1).Reward;
        Assert.Multiple(() =>
        {
            Assert.That(table.Count, Is.EqualTo(1));
            Assert.That(reward.ItemId, Is.EqualTo("potion"));
            Assert.That(reward.ItemCount, Is.EqualTo(3));
        });
    }

    /// <summary>
    ///     验证非对象 dependentRequired 声明会在 schema 解析阶段被拒绝。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_DependentRequired_Is_Not_An_Object()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            reward:
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
                  "properties": {
                    "itemId": { "type": "string" },
                    "itemCount": { "type": "integer" }
                  },
                  "dependentRequired": ["itemId"]
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
            Assert.That(exception!.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.SchemaUnsupported));
            Assert.That(exception.Diagnostic.DisplayPath, Is.EqualTo("reward"));
            Assert.That(exception.Message, Does.Contain("must declare 'dependentRequired' as an object"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证 dependentRequired 的 schema 诊断会保留对象路径原始大小写，避免作者难以定位大小写敏感的坏元数据。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Preserve_Object_Path_Casing_In_DependentRequired_Diagnostics()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            Reward:
              ItemId: potion
            """);
        CreateSchemaFile(
            "schemas/monster.schema.json",
            """
            {
              "type": "object",
              "required": ["id", "Reward"],
              "properties": {
                "id": { "type": "integer" },
                "Reward": {
                  "type": "object",
                  "properties": {
                    "ItemId": { "type": "string" },
                    "ItemCount": { "type": "integer" }
                  },
                  "dependentRequired": {
                    "ItemId": [42]
                  }
                }
              }
            }
            """);

        var loader = CreateCaseSensitiveRewardLoader();
        var registry = CreateRegistry();

        var exception = Assert.ThrowsAsync<ConfigLoadException>(() => loader.LoadAsync(registry));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.SchemaUnsupported));
            Assert.That(exception.Diagnostic.DisplayPath, Is.EqualTo("Reward"));
            Assert.That(exception.Message, Does.Contain("Property 'ItemId' in property 'Reward'"));
            Assert.That(exception.Message, Does.Not.Contain("property 'reward'"));
            Assert.That(registry.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    ///     验证 dependentRequired 只能引用同一对象内已声明的字段。
    /// </summary>
    [Test]
    public void LoadAsync_Should_Throw_When_DependentRequired_Target_Is_Not_Declared()
    {
        CreateConfigFile(
            "monster/slime.yaml",
            """
            id: 1
            reward:
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
                  "properties": {
                    "itemId": { "type": "string" }
                  },
                  "dependentRequired": {
                    "itemId": ["itemCount"]
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
            Assert.That(exception!.Diagnostic.FailureKind, Is.EqualTo(ConfigLoadFailureKind.SchemaUnsupported));
            Assert.That(exception.Diagnostic.DisplayPath, Is.EqualTo("reward"));
            Assert.That(exception.Message, Does.Contain("dependentRequired"));
            Assert.That(exception.Message, Does.Contain("itemCount"));
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
    ///     写入测试 schema 文件，复用统一的测试文件创建逻辑。
    /// </summary>
    /// <param name="relativePath">schema 相对路径。</param>
    /// <param name="content">schema JSON 内容。</param>
    private void CreateSchemaFile(string relativePath, string content)
    {
        CreateConfigFile(relativePath, content);
    }

    /// <summary>
    ///     创建用于对象 dependentRequired 场景的加载器。
    /// </summary>
    /// <returns>已注册测试表与 schema 路径的加载器。</returns>
    private YamlConfigLoader CreateMonsterRewardLoader()
    {
        return new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterRewardConfigStub>("monster", "monster", "schemas/monster.schema.json",
                static config => config.Id);
    }

    /// <summary>
    ///     创建使用大小写敏感对象路径的加载器，验证 schema 诊断不会篡改原始字段名。
    /// </summary>
    /// <returns>已注册 PascalCase 奖励节点的加载器。</returns>
    private YamlConfigLoader CreateCaseSensitiveRewardLoader()
    {
        return new YamlConfigLoader(_rootPath)
            .RegisterTable<int, MonsterPascalCaseRewardConfigStub>("monster", "monster", "schemas/monster.schema.json",
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
    ///     用于对象 dependentRequired 回归测试的最小配置类型。
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
    ///     表示对象 dependentRequired 回归测试中的奖励节点。
    /// </summary>
    private sealed class RewardConfigStub
    {
        /// <summary>
        ///     获取或设置掉落物 ID。
        /// </summary>
        public string ItemId { get; set; } = string.Empty;

        /// <summary>
        ///     获取或设置掉落物数量。
        /// </summary>
        public int ItemCount { get; set; }
    }

    /// <summary>
    ///     用于验证大小写敏感字段路径诊断的配置类型。
    /// </summary>
    private sealed class MonsterPascalCaseRewardConfigStub
    {
        /// <summary>
        ///     获取或设置主键。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        ///     获取或设置使用 PascalCase 字段名的奖励对象。
        /// </summary>
        public PascalCaseRewardConfigStub Reward { get; set; } = new();
    }

    /// <summary>
    ///     表示使用 PascalCase 字段路径的奖励节点。
    /// </summary>
    private sealed class PascalCaseRewardConfigStub
    {
        /// <summary>
        ///     获取或设置掉落物 ID。
        /// </summary>
        public string ItemId { get; set; } = string.Empty;

        /// <summary>
        ///     获取或设置掉落物数量。
        /// </summary>
        public int ItemCount { get; set; }
    }
}
