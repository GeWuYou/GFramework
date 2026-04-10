using System.IO;

namespace GFramework.SourceGenerators.Tests.Config;

/// <summary>
///     验证 schema 配置生成器的生成快照。
/// </summary>
[TestFixture]
public class SchemaConfigGeneratorSnapshotTests
{
    /// <summary>
    ///     验证一个最小 monster schema 能生成配置类型、表包装和注册辅助。
    /// </summary>
    [Test]
    public async Task Snapshot_SchemaConfigGenerator()
    {
        const string source = """
                              using System;
                              using System.Collections.Generic;

                              namespace GFramework.Game.Abstractions.Config
                              {
                                  public interface IConfigTable
                                  {
                                      Type KeyType { get; }
                                      Type ValueType { get; }
                                      int Count { get; }
                                  }

                                  public interface IConfigTable<TKey, TValue> : IConfigTable
                                      where TKey : notnull
                                  {
                                      TValue Get(TKey key);
                                      bool TryGet(TKey key, out TValue? value);
                                      bool ContainsKey(TKey key);
                                      IReadOnlyCollection<TValue> All();
                                  }

                                  public interface IConfigRegistry
                                  {
                                      IConfigTable<TKey, TValue> GetTable<TKey, TValue>(string name)
                                          where TKey : notnull;

                                      bool TryGetTable<TKey, TValue>(string name, out IConfigTable<TKey, TValue>? table)
                                          where TKey : notnull;
                                  }
                              }

                              namespace GFramework.Game.Config
                              {
                                  public sealed class YamlConfigLoader
                                  {
                                      public YamlConfigLoader RegisterTable<TKey, TValue>(
                                          string tableName,
                                          string relativePath,
                                          string schemaRelativePath,
                                          Func<TValue, TKey> keySelector,
                                          IEqualityComparer<TKey>? comparer = null)
                                          where TKey : notnull
                                      {
                                          return this;
                                      }
                                  }
                              }
                              """;

        const string schema = """
                              {
                                "title": "Monster Config",
                                "description": "Represents one monster entry generated from schema metadata.",
                                "type": "object",
                                "minProperties": 4,
                                "maxProperties": 8,
                                "required": ["id", "name", "reward", "phases"],
                                "properties": {
                                  "id": {
                                    "type": "integer",
                                    "description": "Unique monster identifier."
                                  },
                                  "name": {
                                    "type": "string",
                                    "title": "Monster Name",
                                    "description": "Localized monster display name.",
                                    "x-gframework-index": true,
                                    "minLength": 3,
                                    "maxLength": 16,
                                    "pattern": "^[A-Z][a-z]+$",
                                    "default": "Slime",
                                    "enum": ["Slime", "Goblin"]
                                  },
                                  "hp": {
                                    "type": "integer",
                                    "const": 10,
                                    "minimum": 1,
                                    "maximum": 999,
                                    "exclusiveMinimum": 0,
                                    "exclusiveMaximum": 1000,
                                    "multipleOf": 5,
                                    "default": 10
                                  },
                                  "dropItems": {
                                    "description": "Referenced drop ids.",
                                    "type": "array",
                                    "minItems": 1,
                                    "maxItems": 3,
                                    "minContains": 1,
                                    "maxContains": 2,
                                    "uniqueItems": true,
                                    "contains": {
                                      "type": "string",
                                      "const": "potion"
                                    },
                                    "items": {
                                      "type": "string",
                                      "minLength": 3,
                                      "maxLength": 12,
                                      "enum": ["potion", "slime_gel"]
                                    },
                                    "default": ["potion"],
                                    "x-gframework-ref-table": "item"
                                  },
                                  "reward": {
                                    "type": "object",
                                    "description": "Reward payload.",
                                    "minProperties": 2,
                                    "maxProperties": 2,
                                    "required": ["gold", "currency"],
                                    "properties": {
                                      "gold": {
                                        "type": "integer",
                                        "minimum": 0,
                                        "default": 10
                                      },
                                      "currency": {
                                        "type": "string",
                                        "enum": ["coin", "gem"]
                                      }
                                    }
                                  },
                                  "phases": {
                                    "type": "array",
                                    "description": "Encounter phases.",
                                    "items": {
                                      "type": "object",
                                      "required": ["wave", "monsterId"],
                                      "properties": {
                                        "wave": {
                                          "type": "integer"
                                        },
                                        "monsterId": {
                                          "type": "string",
                                          "description": "Monster reference id.",
                                          "minLength": 2,
                                          "maxLength": 32,
                                          "x-gframework-ref-table": "monster"
                                        }
                                      }
                                    }
                                  }
                                }
                              }
                              """;

        var result = SchemaGeneratorTestDriver.Run(
            source,
            ("monster.schema.json", schema));

        var generatedSources = result.Results
            .Single()
            .GeneratedSources
            .ToDictionary(
                static sourceResult => sourceResult.HintName,
                static sourceResult => sourceResult.SourceText.ToString(),
                StringComparer.Ordinal);

        var snapshotFolder = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..",
            "..",
            "..",
            "Config",
            "snapshots",
            "SchemaConfigGenerator");
        snapshotFolder = Path.GetFullPath(snapshotFolder);

        await AssertSnapshotAsync(generatedSources, snapshotFolder, "MonsterConfig.g.cs", "MonsterConfig.g.txt");
        await AssertSnapshotAsync(generatedSources, snapshotFolder, "MonsterTable.g.cs", "MonsterTable.g.txt");
        await AssertSnapshotAsync(generatedSources, snapshotFolder, "MonsterConfigBindings.g.cs",
            "MonsterConfigBindings.g.txt");
        await AssertSnapshotAsync(generatedSources, snapshotFolder, "GeneratedConfigCatalog.g.cs",
            "GeneratedConfigCatalog.g.txt");
    }

    /// <summary>
    ///     对单个生成文件执行快照断言。
    /// </summary>
    /// <param name="generatedSources">生成结果字典。</param>
    /// <param name="snapshotFolder">快照目录。</param>
    /// <param name="fileName">快照文件名。</param>
    private static async Task AssertSnapshotAsync(
        IReadOnlyDictionary<string, string> generatedSources,
        string snapshotFolder,
        string generatedFileName,
        string snapshotFileName)
    {
        if (!generatedSources.TryGetValue(generatedFileName, out var actual))
        {
            Assert.Fail($"Generated source '{generatedFileName}' was not found.");
            return;
        }

        var path = Path.Combine(snapshotFolder, snapshotFileName);
        if (!File.Exists(path))
        {
            Directory.CreateDirectory(snapshotFolder);
            await File.WriteAllTextAsync(path, actual);
            Assert.Fail($"Snapshot not found. Generated new snapshot at:\n{path}");
        }

        var expected = await File.ReadAllTextAsync(path);
        Assert.That(
            Normalize(expected),
            Is.EqualTo(Normalize(actual)),
            $"Snapshot mismatch: {generatedFileName}");
    }

    /// <summary>
    ///     标准化快照文本以避免平台换行差异。
    /// </summary>
    /// <param name="text">原始文本。</param>
    /// <returns>标准化后的文本。</returns>
    private static string Normalize(string text)
    {
        return text.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();
    }
}
