using System.IO;
using Microsoft.CodeAnalysis;

namespace GFramework.SourceGenerators.Tests.Config;

/// <summary>
///     验证 schema 配置生成器对对象 / 数组 <c>enum</c> 文档输出的快照行为。
/// </summary>
[TestFixture]
public class SchemaConfigGeneratorEnumTests
{
    /// <summary>
    ///     验证对象 <c>enum</c> 文档输出与快照保持一致。
    /// </summary>
    [Test]
    public Task Snapshot_Should_Preserve_Object_Enum_Documentation()
    {
        const string source = """
                              namespace TestApp
                              {
                                  public sealed class Dummy
                                  {
                                  }
                              }
                              """;

        const string schema = """
                              {
                                "type": "object",
                                "required": ["id", "reward"],
                                "properties": {
                                  "id": { "type": "integer" },
                                  "reward": {
                                    "type": "object",
                                    "required": ["gold", "itemId"],
                                    "properties": {
                                      "gold": { "type": "integer" },
                                      "itemId": { "type": "string" }
                                    },
                                    "enum": [
                                      { "gold": 10, "itemId": "potion" },
                                      { "gold": 50, "itemId": "gem" }
                                    ]
                                  }
                                }
                              }
                              """;

        var result = SchemaGeneratorTestDriver.Run(
            source,
            ("monster.schema.json", schema));

        Assert.That(result.Results.Single().Diagnostics, Is.Empty);
        return AssertSnapshotAsync(result, "MonsterConfig.ObjectEnum.g.txt");
    }

    /// <summary>
    ///     验证数组项 <c>enum</c> 文档回退输出与快照保持一致。
    /// </summary>
    [Test]
    public Task Snapshot_Should_Preserve_Array_Item_Enum_Documentation_Fallback()
    {
        const string source = """
                              namespace TestApp
                              {
                                  public sealed class Dummy
                                  {
                                  }
                              }
                              """;

        const string schema = """
                              {
                                "type": "object",
                                "required": ["id", "dropItemIds"],
                                "properties": {
                                  "id": { "type": "integer" },
                                  "dropItemIds": {
                                    "type": "array",
                                    "items": { "type": "string", "enum": ["fire", "ice", "earth"] }
                                  }
                                }
                              }
                              """;

        var result = SchemaGeneratorTestDriver.Run(
            source,
            ("monster.schema.json", schema));

        Assert.That(result.Results.Single().Diagnostics, Is.Empty);
        return AssertSnapshotAsync(result, "MonsterConfig.ArrayItemEnum.g.txt");
    }

    /// <summary>
    ///     验证对象数组项 <c>enum</c> 文档回退输出与快照保持一致。
    /// </summary>
    [Test]
    public Task Snapshot_Should_Preserve_Array_Object_Item_Enum_Documentation_Fallback()
    {
        const string source = """
                              namespace TestApp
                              {
                                  public sealed class Dummy
                                  {
                                  }
                              }
                              """;

        const string schema = """
                              {
                                "type": "object",
                                "required": ["id", "phases"],
                                "properties": {
                                  "id": { "type": "integer" },
                                  "phases": {
                                    "type": "array",
                                    "items": {
                                      "type": "object",
                                      "required": ["wave", "monsterId"],
                                      "properties": {
                                        "wave": { "type": "integer" },
                                        "monsterId": { "type": "string" }
                                      },
                                      "enum": [
                                        { "wave": 1, "monsterId": "slime" },
                                        { "wave": 2, "monsterId": "goblin" }
                                      ]
                                    }
                                  }
                                }
                              }
                              """;

        var result = SchemaGeneratorTestDriver.Run(
            source,
            ("monster.schema.json", schema));

        Assert.That(result.Results.Single().Diagnostics, Is.Empty);
        return AssertSnapshotAsync(result, "MonsterConfig.ArrayObjectItemEnum.g.txt");
    }

    /// <summary>
    ///     对单个生成文件执行快照断言。
    /// </summary>
    /// <param name="result">生成器运行结果。</param>
    /// <param name="snapshotFileName">快照文件名。</param>
    private static async Task AssertSnapshotAsync(
        GeneratorDriverRunResult result,
        string snapshotFileName)
    {
        var generatedSources = result.Results
            .Single()
            .GeneratedSources
            .ToDictionary(
                static sourceResult => sourceResult.HintName,
                static sourceResult => sourceResult.SourceText.ToString(),
                StringComparer.Ordinal);

        if (!generatedSources.TryGetValue("MonsterConfig.g.cs", out var actual))
        {
            Assert.Fail("Generated source 'MonsterConfig.g.cs' was not found.");
            return;
        }

        var snapshotFolder = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..",
            "..",
            "..",
            "Config",
            "snapshots",
            "SchemaConfigGeneratorEnum");
        snapshotFolder = Path.GetFullPath(snapshotFolder);

        var path = Path.Combine(snapshotFolder, snapshotFileName);
        if (!File.Exists(path))
        {
            Directory.CreateDirectory(snapshotFolder);
            await File.WriteAllTextAsync(path, actual).ConfigureAwait(false);
            Assert.Fail($"Snapshot not found. Generated new snapshot at:\n{path}");
        }

        var expected = await File.ReadAllTextAsync(path).ConfigureAwait(false);
        Assert.That(
            Normalize(expected),
            Is.EqualTo(Normalize(actual)),
            $"Snapshot mismatch: MonsterConfig.g.cs ({snapshotFileName})");
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
