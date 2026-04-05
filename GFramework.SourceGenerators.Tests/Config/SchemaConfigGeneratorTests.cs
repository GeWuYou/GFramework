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

    /// <summary>
    ///     验证 schema 字段名无法映射为合法 C# 标识符时会直接给出诊断，而不是生成不可编译代码。
    /// </summary>
    /// <param name="schemaKey">会映射为非法 C# 标识符的 schema key。</param>
    /// <param name="generatedIdentifier">当前命名规范化逻辑生成出的非法标识符。</param>
    [TestCase("drop$item", "Drop$item")]
    [TestCase("1-phase", "1Phase")]
    public void Run_Should_Report_Diagnostic_When_Schema_Key_Maps_To_Invalid_CSharp_Identifier(
        string schemaKey,
        string generatedIdentifier)
    {
        const string source = """
                              namespace TestApp
                              {
                                  public sealed class Dummy
                                  {
                                  }
                              }
                              """;

        var schema = $$"""
                       {
                         "type": "object",
                         "required": ["id", "{{schemaKey}}"],
                         "properties": {
                           "id": { "type": "integer" },
                           "{{schemaKey}}": { "type": "string" }
                         }
                       }
                       """;

        var result = SchemaGeneratorTestDriver.Run(
            source,
            ("monster.schema.json", schema));

        var diagnostic = result.Results.Single().Diagnostics.Single();

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Id, Is.EqualTo("GF_ConfigSchema_006"));
            Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(diagnostic.GetMessage(), Does.Contain(schemaKey));
            Assert.That(diagnostic.GetMessage(), Does.Contain(generatedIdentifier));
        });
    }

    /// <summary>
    ///     验证引用元数据成员名在不同路径规范化后发生碰撞时，生成器仍会分配全局唯一的成员名。
    /// </summary>
    [Test]
    public void Run_Should_Assign_Globally_Unique_Reference_Metadata_Member_Names()
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
                                "type": "object",
                                "required": ["id"],
                                "properties": {
                                  "id": { "type": "integer" },
                                  "drop-items": {
                                    "type": "array",
                                    "items": { "type": "string" },
                                    "x-gframework-ref-table": "item"
                                  },
                                  "drop_items": {
                                    "type": "array",
                                    "items": { "type": "string" },
                                    "x-gframework-ref-table": "item"
                                  },
                                  "dropItems1": {
                                    "type": "string",
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

        Assert.That(result.Results.Single().Diagnostics, Is.Empty);
        Assert.That(generatedSources.TryGetValue("MonsterConfigBindings.g.cs", out var bindingsSource), Is.True);
        Assert.That(bindingsSource, Does.Contain("public static readonly ReferenceMetadata DropItems ="));
        Assert.That(bindingsSource, Does.Contain("public static readonly ReferenceMetadata DropItems1 ="));
        Assert.That(bindingsSource, Does.Contain("public static readonly ReferenceMetadata DropItems11 ="));
    }
}