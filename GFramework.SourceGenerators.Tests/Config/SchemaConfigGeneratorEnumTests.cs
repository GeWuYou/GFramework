namespace GFramework.SourceGenerators.Tests.Config;

/// <summary>
///     验证 schema 配置生成器对对象 / 数组 <c>enum</c> 文档输出的行为。
/// </summary>
[TestFixture]
public class SchemaConfigGeneratorEnumTests
{
    /// <summary>
    ///     验证对象 <c>enum</c> 会以原始 JSON 文本写入生成代码 XML 文档。
    /// </summary>
    [Test]
    public void Run_Should_Write_Object_Enum_Into_Generated_Documentation()
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

        var generatedSources = result.Results
            .Single()
            .GeneratedSources
            .ToDictionary(
                static sourceResult => sourceResult.HintName,
                static sourceResult => sourceResult.SourceText.ToString(),
                StringComparer.Ordinal);

        Assert.That(result.Results.Single().Diagnostics, Is.Empty);
        Assert.That(generatedSources["MonsterConfig.g.cs"],
            Does.Contain("Allowed values: { \"gold\": 10, \"itemId\": \"potion\" }, { \"gold\": 50, \"itemId\": \"gem\" }."));
    }

    /// <summary>
    ///     验证数组 <c>enum</c> 会以保留顺序的 JSON 数组文本写入生成代码 XML 文档。
    /// </summary>
    [Test]
    public void Run_Should_Write_Array_Enum_Into_Generated_Documentation()
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
                                    "items": { "type": "string" },
                                    "enum": [
                                      ["fire", "ice"],
                                      ["earth"]
                                    ]
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

        Assert.That(result.Results.Single().Diagnostics, Is.Empty);
        Assert.That(generatedSources["MonsterConfig.g.cs"],
            Does.Contain("Allowed values: [\"fire\", \"ice\"], [\"earth\"]."));
    }
}
