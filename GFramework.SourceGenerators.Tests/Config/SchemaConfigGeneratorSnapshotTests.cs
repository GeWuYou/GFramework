using System.IO;

namespace GFramework.SourceGenerators.Tests.Config;

/// <summary>
///     验证 schema 配置生成器的生成快照。
/// </summary>
[TestFixture]
public class SchemaConfigGeneratorSnapshotTests
{
    /// <summary>
    ///     验证一个最小 monster schema 能生成配置类型和表包装。
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
                              }
                              """;

        const string schema = """
                              {
                                "title": "Monster Config",
                                "description": "Represents one monster entry generated from schema metadata.",
                                "type": "object",
                                "required": ["id", "name"],
                                "properties": {
                                  "id": {
                                    "type": "integer",
                                    "description": "Unique monster identifier."
                                  },
                                  "name": {
                                    "type": "string",
                                    "title": "Monster Name",
                                    "description": "Localized monster display name.",
                                    "default": "Slime",
                                    "enum": ["Slime", "Goblin"]
                                  },
                                  "hp": {
                                    "type": "integer",
                                    "default": 10
                                  },
                                  "dropItems": {
                                    "description": "Referenced drop ids.",
                                    "type": "array",
                                    "items": {
                                      "type": "string",
                                      "enum": ["potion", "slime_gel"]
                                    },
                                    "default": ["potion"],
                                    "x-gframework-ref-table": "item"
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