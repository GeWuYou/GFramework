namespace GFramework.SourceGenerators.Tests.Config;

/// <summary>
///     验证 schema 配置生成器的错误诊断行为。
/// </summary>
[TestFixture]
public class SchemaConfigGeneratorTests
{
    /// <summary>
    ///     验证缺失必填 id 字段时会产生命名明确的诊断。
    /// </summary>
    [Test]
    public void Run_Should_Report_Diagnostic_When_Id_Property_Is_Missing()
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
                                "required": ["name"],
                                "properties": {
                                  "name": { "type": "string" }
                                }
                              }
                              """;

        var result = SchemaGeneratorTestDriver.Run(
            source,
            ("monster.schema.json", schema));

        var diagnostics = result.Results.Single().Diagnostics;
        var diagnostic = diagnostics.Single();

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Id, Is.EqualTo("GF_ConfigSchema_003"));
            Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(diagnostic.GetMessage(), Does.Contain("monster.schema.json"));
        });
    }

    /// <summary>
    ///     验证深层不支持的数组嵌套会带着完整字段路径产生命名明确的诊断。
    /// </summary>
    [Test]
    public void Run_Should_Report_Diagnostic_When_Nested_Array_Type_Is_Not_Supported()
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
                                "required": ["id"],
                                "properties": {
                                  "id": { "type": "integer" },
                                  "waves": {
                                    "type": "array",
                                    "items": {
                                      "type": "array",
                                      "items": { "type": "integer" }
                                    }
                                  }
                                }
                              }
                              """;

        var result = SchemaGeneratorTestDriver.Run(
            source,
            ("monster.schema.json", schema));

        var diagnostic = result.Results.Single().Diagnostics.Single();

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Id, Is.EqualTo("GF_ConfigSchema_004"));
            Assert.That(diagnostic.GetMessage(), Does.Contain("waves"));
            Assert.That(diagnostic.GetMessage(), Does.Contain("array<array>"));
        });
    }
}