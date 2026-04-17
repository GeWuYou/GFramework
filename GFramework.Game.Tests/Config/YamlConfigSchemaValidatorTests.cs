using System.IO;
using GFramework.Game.Config;

namespace GFramework.Game.Tests.Config;

/// <summary>
///     验证内部 schema 解析器会输出稳定且可预期的运行时依赖元数据。
/// </summary>
[TestFixture]
public sealed class YamlConfigSchemaValidatorTests
{
    private string? _rootPath;

    /// <summary>
    ///     为每个测试准备独立临时目录。
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        _rootPath = Path.Combine(Path.GetTempPath(), "GFramework.SchemaValidatorTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_rootPath);
    }

    /// <summary>
    ///     清理测试临时目录。
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
    ///     验证 schema 中声明的跨表引用名称会以序数排序形式输出，
    ///     避免热重载依赖推导与测试快照受哈希集合枚举顺序影响。
    /// </summary>
    [Test]
    public void Load_Should_Return_Referenced_Table_Names_In_Ordinal_Sorted_Order()
    {
        var schemaPath = CreateSchemaFile(
            "schemas/monster.schema.json",
            """
            {
              "type": "object",
              "properties": {
                "weaponId": {
                  "type": "string",
                  "x-gframework-ref-table": "weapon"
                },
                "allies": {
                  "type": "array",
                  "items": {
                    "type": "integer",
                    "x-gframework-ref-table": "ally"
                  }
                },
                "itemId": {
                  "type": "string",
                  "x-gframework-ref-table": "item"
                }
              }
            }
            """);

        var schema = YamlConfigSchemaValidator.Load("monster", schemaPath);

        Assert.That(schema.ReferencedTableNames, Is.EqualTo(new[] { "ally", "item", "weapon" }));
    }

    /// <summary>
    ///     验证条件子 schema 复用同一条 ref-table 字段时，不会把同一引用重复写入结果。
    /// </summary>
    [Test]
    public void ValidateAndCollectReferences_Should_Not_Duplicate_Reference_Usages_From_DependentSchemas()
    {
        var schemaPath = CreateSchemaFile(
            "schemas/monster.schema.json",
            """
            {
              "type": "object",
              "properties": {
                "reward": {
                  "type": "object",
                  "properties": {
                    "itemId": {
                      "type": "string",
                      "x-gframework-ref-table": "item"
                    }
                  },
                  "dependentSchemas": {
                    "itemId": {
                      "type": "object",
                      "properties": {
                        "itemId": {
                          "type": "string",
                          "x-gframework-ref-table": "item"
                        }
                      }
                    }
                  }
                }
              }
            }
            """);
        var schema = YamlConfigSchemaValidator.Load("monster", schemaPath);

        var references = YamlConfigSchemaValidator.ValidateAndCollectReferences(
            "monster",
            schema,
            "monster/slime.yaml",
            """
            reward:
              itemId: potion
            """);

        Assert.That(references, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(references[0].DisplayPath, Is.EqualTo("reward.itemId"));
            Assert.That(references[0].ReferencedTableName, Is.EqualTo("item"));
            Assert.That(references[0].RawValue, Is.EqualTo("potion"));
        });
    }

    /// <summary>
    ///     验证 <c>allOf</c> focused block 复用同一条 ref-table 字段时，不会把同一引用重复写入结果。
    /// </summary>
    [Test]
    public void ValidateAndCollectReferences_Should_Not_Duplicate_Reference_Usages_From_AllOf()
    {
        var schemaPath = CreateSchemaFile(
            "schemas/monster.schema.json",
            """
            {
              "type": "object",
              "properties": {
                "reward": {
                  "type": "object",
                  "properties": {
                    "itemId": {
                      "type": "string",
                      "x-gframework-ref-table": "item"
                    }
                  },
                  "allOf": [
                    {
                      "type": "object",
                      "properties": {
                        "itemId": {
                          "type": "string",
                          "x-gframework-ref-table": "item"
                        }
                      }
                    }
                  ]
                }
              }
            }
            """);
        var schema = YamlConfigSchemaValidator.Load("monster", schemaPath);

        var references = YamlConfigSchemaValidator.ValidateAndCollectReferences(
            "monster",
            schema,
            "monster/slime.yaml",
            """
            reward:
              itemId: potion
            """);

        Assert.That(references, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(references[0].DisplayPath, Is.EqualTo("reward.itemId"));
            Assert.That(references[0].ReferencedTableName, Is.EqualTo("item"));
            Assert.That(references[0].RawValue, Is.EqualTo("potion"));
        });
    }

    /// <summary>
    ///     在临时目录中创建 schema 文件。
    /// </summary>
    /// <param name="relativePath">相对根目录的路径。</param>
    /// <param name="content">文件内容。</param>
    /// <returns>写入后的绝对路径。</returns>
    private string CreateSchemaFile(
        string relativePath,
        string content)
    {
        ArgumentNullException.ThrowIfNull(_rootPath);

        var fullPath = Path.Combine(_rootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var directoryPath = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        File.WriteAllText(fullPath, content.Replace("\n", Environment.NewLine, StringComparison.Ordinal));
        return fullPath;
    }
}
